using Standard.AI.OpenAI.Clients.OpenAIs;
using Standard.AI.OpenAI.Models.Services.Foundations.ChatCompletions;
using TFB.DTOs.Settings;
using TFB.Models;
using TFB.Services.Analysis;
using TFB.Services.OpenRouter;
using Message = TFB.DTOs.Message;
using MessageType = TFB.DTOs.MessageType;
using Personality = TFB.DTOs.Personality;

namespace TFB.Services.BotHandler;

public class BotHandlerService
{
    private TelegramService _telegramService;
    private OpenAIClient _openAiClient;
    private readonly IMessageHistoryService _messageHistoryService;
    private PersonalitySheetsService _personalitySheetsService;
    private IPersonalityService _personalityService;
    private IOpenRouterService _openRouterService;
    private ChatSettings _chatSettings;
    private CompressorService _compressorService;
    private ChoicesService _choicesService;
    private AnalysisService _analysisService;

    private DateTime _lastRanSpreadsheet = DateTime.Now;
    public BotHandlerService(AnalysisService analysisService, IPersonalityService personalityService, TelegramService telegramService, 
        PersonalitySheetsService personalitySheetsService,
        OpenAIClient openAiClient, IMessageHistoryService messageHistoryService,
        IOpenRouterService openRouterService, ChatSettings chatSettings, 
        CompressorService compressorService, ChoicesService choicesService)
    {
        _analysisService = analysisService;
        _personalityService = personalityService;
        _telegramService = telegramService;
        _personalitySheetsService = personalitySheetsService;
        _openAiClient = openAiClient;
        _messageHistoryService = messageHistoryService;
        _openRouterService = openRouterService;
        _chatSettings = chatSettings;
        _compressorService = compressorService;
        _choicesService = choicesService;
        
    }

    private async Task<MessageHistory> SaveMessage(TFB.DTOs.Message message, Personality personality)
    {
        var messageHistory = new MessageHistory()
        {
            Author = message.Author,
            Message = message.Value,
            DateCreated = message.DatePosted,
            PersonalityId = personality.PersonalityId,
            Role = message.MessageType == MessageType.User ? "user" : "system"
        };

        return await _messageHistoryService.AddMessage(messageHistory);
    }

    private async Task GetCompressedVersion(Personality personality, MessageHistory messageHistory, List<ChatCompletionMessage> chatCompletion)
    {
        string MessageTemplate = "Author: {0} \n Message: {1} \n Date Posted {2} \n";
        var combined =
            AnalysisService.BuildCombinedMessages(personality.MessageHistory, personality.Command, MessageTemplate);
        var compressorReq = _compressorService.BuildBulkCompression(new string[] { combined });
        var compression = await _compressorService.RequestCompression(compressorReq);

        var compressed = compression.FirstOrDefault()?.Compressed ?? "";

        messageHistory.Summary = compressed;
        
        await _messageHistoryService.UpdateMessageSummary(messageHistory);
    }
    
    public async Task HandleUpdate(BotHandlerRequest request)
    {
        var personalityModels = await _personalityService.GetPersonalities(new PersonalityRequest()
        {
            IncludeMessageHistory = true
        });

        var personalities = personalityModels.ToDTOList();
        
        foreach (var personality in personalities)
        {
            if (!personality.MatchesCommand(request.Command))
            {
                continue;
            }

            if (request.MessageText.Contains("wipe_context"))
            {
                var deleted = await _messageHistoryService.WipeMessagesByPersonality(personality.PersonalityId);
                personality.MessageHistory = new List<Message>();
                await _telegramService.Send(request.ChatId, $"Wiped {deleted} messages for {personality.Name}.",
                    request.TelegramMessage.ReplyToMessage?.MessageId, null);
                continue;
            }

            // save the user message
            await SaveMessage(request.Message, personality);

            personality.MessageHistory.Add(request.Message);
            
            // getting analysis
            var req = new AnalysisRequest()
            {
                Personality = personality
            };
            var response = await _analysisService.Analysis(req);
            
            if (!response.Success)
            {
                await _telegramService.Send(
                    request.ChatId, 
                    $"Something went wrong getting {personality.Name} to respond. Maybe they're sick?", 
                    request.TelegramMessage.ReplyToMessage?.MessageId);
                continue;
            }
            
            // send the text message
            await _telegramService.Send(request.ChatId, response.Message, request.TelegramMessage.ReplyToMessage?.MessageId, null);

            var botMessage = new TFB.DTOs.Message()
            {
                Author = personality.Name,
                DatePosted = DateTime.UtcNow,
                MessageType = MessageType.Bot,
                Value = response.Message
            };
            
            personality.MessageHistory.Add(botMessage);
            
            // save the bot message
            var savedBotMessage = await SaveMessage(botMessage, personality);
            

            Task.Run(async () => { await GetCompressedVersion(personality,savedBotMessage, response.ChatCompletionChoices); });

        }
        
        /*foreach (var analysisService in personalities)
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
        
        
        Console.WriteLine($"Got a message from {telegramMessage.From?.FirstName} with contents: {messageText}");
        string analysis;
        var matchingAnylizers = _analyzers.Where(analyzer => analyzer.MatchesCommand(command));
        foreach (var analysisService in matchingAnylizers)
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
            
            
            ChoicesResponse? choices = null;
            if (analysis.Length > 0 && analysisService._personality.HasOptions)
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
            if (choices != null && choices?.Choices != null && choices.Choices.Any())
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

            var latestMessage = new TFB.DTOs.Message()
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
            
        }*/
    }
}