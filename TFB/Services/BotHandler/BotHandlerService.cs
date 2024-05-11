using Microsoft.Extensions.Logging;
using Standard.AI.OpenAI.Models.Services.Foundations.ChatCompletions;
using TFB.DTOs.Settings;
using TFB.Models;
using TFB.Services.Analysis;
using Message = TFB.DTOs.Message;
using MessageType = TFB.DTOs.MessageType;
using Personality = TFB.DTOs.Personality;

namespace TFB.Services.BotHandler;

public class BotHandlerService
{
    private TelegramService _telegramService;
    private readonly IMessageHistoryService _messageHistoryService;
    private IPersonalityService _personalityService;
    private CompressorService _compressorService;
    private AnalysisService _analysisService;
    private GeneralSettings _generalSettings;
    private ILogger<BotHandlerService> _logger;
    public BotHandlerService(AnalysisService analysisService, 
        IPersonalityService personalityService, 
        TelegramService telegramService,
        IMessageHistoryService messageHistoryService,
        CompressorService compressorService,
        GeneralSettings generalSettings,
        ILogger<BotHandlerService> logger)
    {
        _analysisService = analysisService;
        _personalityService = personalityService;
        _telegramService = telegramService;
        _messageHistoryService = messageHistoryService;
        _compressorService = compressorService;
        _generalSettings = generalSettings;
        _logger = logger;
    }

    private async Task<MessageHistory> SaveMessage(Message message, Personality personality)
    {
        var messageHistory = new MessageHistory()
        {
            Author = message.Author,
            Message = message.Value,
            DateCreated = message.DatePosted,
            PersonalityId = personality.PersonalityId,
            Role = message.MessageType == MessageType.User ? "user" : "system",
            ConversationWith = message.ConversationWith
        };

        return await _messageHistoryService.AddMessage(messageHistory);
    }

    private async Task GenerateCompressedVersion(Personality personality, MessageHistory messageHistory, List<ChatCompletionMessage> chatCompletion)
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

            var author = (request.TelegramMessage?.From?.Username ?? (request.TelegramMessage?.From?.FirstName ?? "Default*User"));

            personality.MessageHistory = personality.MessageHistory
                .OrderByDescending(mh => mh.DatePosted)
                .Where(mh =>
                {
                    return mh.ConversationWith == author;
                })
                .TakeWhile(mh => string.IsNullOrEmpty(mh.Summary))
                .ToList();

            string? summary = null;
            summary = await _messageHistoryService.GetSummary(personality.PersonalityId, author);

            if (personality.MessageHistory.Count > _generalSettings.MaxHistoryBeforeSummary)
            {
                _logger.LogDebug($"{personality.Name} has exceeded message history. Attempting to get the summary.");
                summary = await _messageHistoryService.GetSummary(personality.PersonalityId, author);

                if (string.IsNullOrEmpty(summary))
                {
                    _logger.LogDebug($"There was no summary for {personality.Name} and {author}.");
                }
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
                Personality = personality,
                ChatTypes = ChatTypes.Local,
                Summary = summary // nullable
            };
            var response = await _analysisService.Analysis(req);
            
            if (!response.Success)
            {
                await _telegramService.Send(
                    request.ChatId, 
                    $"...", 
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
                Value = response.Message,
                ConversationWith = author
            };
            
            personality.MessageHistory.Add(botMessage);
            
            // save the bot message
            var savedBotMessage = await SaveMessage(botMessage, personality);

            if (personality.MessageHistory.Count > _generalSettings.MaxHistoryBeforeSummary)
            {
                _ = Task.Run(async () => { await GenerateCompressedVersion(personality, savedBotMessage, response.ChatCompletionChoices); });
                
            }

        }
        
    }
}