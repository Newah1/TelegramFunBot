using FluentAssertions;
using Standard.AI.OpenAI.Clients.OpenAIs;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TFB.Models;
using MessageType = TFB.Models.MessageType;

namespace TFB.Services;

public class BotHandlerService
{
    private List<AnalysisService> _analyzers;
    private TelegramService _telegramService;
    private OpenAIClient _openAiClient;
    private ITelegramBotClient? _telegramBotClient;
    private PersonalitySheetsService _personalitySheetsService;
    private IEnumerable<Personality> _personalities;
    private OpenRouterService _openRouterService;
    private ChatSettings _chatSettings;
    private CompressorService _compressorService;
    private ChoicesService _choicesService;

    private DateTime _lastRanSpreadsheet = DateTime.Now;
    public BotHandlerService(List<AnalysisService> analyzers, TelegramService telegramService, PersonalitySheetsService personalitySheetsService, ITelegramBotClient? telegramBotClient, IEnumerable<Personality> personalities, OpenAIClient openAiClient, OpenRouterService openRouterService, ChatSettings chatSettings, CompressorService compressorService, ChoicesService choicesService)
    {
        _analyzers = analyzers;
        _telegramService = telegramService;
        _personalitySheetsService = personalitySheetsService;
        _telegramBotClient = telegramBotClient;
        _personalities = personalities;
        _openAiClient = openAiClient;
        _openRouterService = openRouterService;
        _chatSettings = chatSettings;
        _compressorService = compressorService;
        _choicesService = choicesService;
    }

    public async Task HandleQuery()
    {
        
    } 

    public async Task HandleUpdate(long chatId, string command, string messageText, TFB.Models.Message message, CancellationToken cancellationToken, Telegram.Bot.Types.Message telegramMessage)
    {
        
        foreach (var analysisService in _analyzers)
        {
            analysisService.AddMessage(message);
            analysisService.CombinedMessages.Add(message);
        }

        if (command == "/wipe_context")
        {
            foreach (var analysisService in _analyzers)
            {
                analysisService.WipeMessages();
            }
            await _telegramBotClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"Wiped {_analyzers.Count().ToString()} personalities.",
                cancellationToken: cancellationToken, replyToMessageId: telegramMessage.ReplyToMessage?.MessageId);
        } else if (command == "/report")
        {
            await _telegramBotClient.SendTextMessageAsync(
                chatId: chatId,
                text: new ReportService(_analyzers.ToList()).GenerateReport(),
                cancellationToken: cancellationToken, replyToMessageId: telegramMessage.ReplyToMessage?.MessageId);
        }
        
        if ((DateTime.Now - _lastRanSpreadsheet).TotalSeconds > 60)
        {
            var refreshValues = _personalitySheetsService.LoadPersonalities();
            var personalitiesRefreshed = new List<Personality>();
            foreach (var personality in _personalities)
            {
                Personality? personalityToAdd;
                var newCommands = refreshValues.Select(e => e.Command).ToList();
                personalityToAdd = newCommands.Contains(personality.Command) ? refreshValues.FirstOrDefault(rf => rf.Command == personality.Command) : personality;
                if(personalityToAdd != null) {
                    personalitiesRefreshed.Add(personalityToAdd);
                }
            }

            var newPersonalities = refreshValues
                .Where(r => !personalitiesRefreshed.Select(e => e.Command).Contains(r.Command)).ToList();
            
            personalitiesRefreshed.AddRange(
                    newPersonalities
                );

            _personalities = personalitiesRefreshed;

            foreach (var analysisService in _analyzers)
            {
                var matchingPersonality = _personalities.FirstOrDefault(p => p.Command == analysisService.Command);
                if (matchingPersonality != null)
                {
                    analysisService.Template = matchingPersonality.PersonalityDescription;
                    analysisService.Name = matchingPersonality.Name;
                    analysisService.SetPersonality(matchingPersonality);
                }
            }

            foreach (var personality in newPersonalities)
            {
                _analyzers.Add(
                    AnalysisService.GetAnalyzer(personality, _openAiClient, _chatSettings, _openRouterService)
                    );
            }
            
            _lastRanSpreadsheet = DateTime.Now;
        }
        
        Console.WriteLine($"Got a message from {telegramMessage.From?.FirstName} with contents: {messageText}");
        string analysis;
        foreach (var analysisService in _analyzers)
        {
            if (command.ToLower().Trim() == analysisService.Command.Trim().ToLower())
            {
                if (messageText.Contains("requesting analysis"))
                {
                    await _telegramBotClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "I know this: " + analysisService.Compressed ?? "No context.",
                            cancellationToken: cancellationToken, replyToMessageId: telegramMessage.ReplyToMessage?.MessageId);
                    return;

                }

                Console.WriteLine($"Handling analysis with {analysisService.Name}");
                
                // tell the bot to "type"
                var timer = new Timer(state =>
                {
                    if (_telegramBotClient != null)
                        _telegramBotClient.SendChatActionAsync(chatId, ChatAction.Typing,
                            telegramMessage.ReplyToMessage?.MessageId, cancellationToken: cancellationToken);
                }, null, TimeSpan.Zero, TimeSpan.FromSeconds(3));

                
                analysis = await analysisService.Analysis();
                ChoicesResponse choices = null;
                if (analysis.Length > 0)
                {
                    // get choices
                    choices = await _choicesService.GetPotentialChoices(new ChoicesRequest()
                    {
                        Author = analysisService.Name,
                        Message = analysis,
                        Command = analysisService.Command
                    });
                }

                InlineKeyboardMarkup? inlineKeyboard = null;
                if (choices != null && choices.Choices != null && choices.Choices.Any())
                {
                    Console.WriteLine("choices!");
                    inlineKeyboard = new(
                        choices.Choices.Select(choice => new [] {InlineKeyboardButton.WithCallbackData(text:choice.Name, callbackData: choice.Command)})
                    );
                }
                
                // stop "typing"
                await timer.DisposeAsync();
                if (string.IsNullOrEmpty(analysis))
                {
                    await _telegramService.Send(
                        chatId, 
                        $"Something went wrong getting {analysisService.Name} to respond. Maybe they're sick?", 
                        telegramMessage.ReplyToMessage?.MessageId);
                    continue;
                }
                
                // send the text message
                await _telegramService.Send(chatId, analysis, telegramMessage.ReplyToMessage?.MessageId, inlineKeyboard);

                var compressionResponse = await _compressorService.RequestCompression(_compressorService.BuildBulkCompression(new string[]{ analysisService.BuildCombinedMessages() }));

                if (compressionResponse.Any(cr => cr.Success))
                {
                    analysisService.Compressed = compressionResponse.FirstOrDefault()?.Compressed;
                }

                var latestMessage = new TFB.Models.Message()
                {
                    Author = analysisService.Name,
                    DatePosted = DateTime.Now,
                    Value = analysis,
                    MessageType = MessageType.Bot
                };
                analysisService.UserBotDiscourse.Add(message);
                analysisService.UserBotDiscourse.Add(latestMessage);
                analysisService.CombinedMessages.Add(latestMessage);

                // give other analyzers your most recent message
                foreach (var otherAnalyzers in _analyzers.Where(a => a != analysisService).ToList())
                {
                    Console.WriteLine($"Giving {otherAnalyzers.Name} the most recent message [{analysis.Substring(0, 10)}...]");
                    if (compressionResponse.Any(cr => cr.Success))
                    {
                        otherAnalyzers.Compressed = compressionResponse.FirstOrDefault().Compressed;
                    }

                    otherAnalyzers.AddMessage(latestMessage);
                    otherAnalyzers.CombinedMessages.Add(latestMessage);
                }
            }
        }
    }

    public void SetAnalyzers(List<AnalysisService> analyzers)
    {
        _analyzers = analyzers;
    }
    
    public void SetTelegramBotClient(ITelegramBotClient telegramBotClient)
    {
        _telegramBotClient = telegramBotClient;
    }
}