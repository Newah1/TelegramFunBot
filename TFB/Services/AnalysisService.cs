using Standard.AI.OpenAI.Clients.OpenAIs;
using Standard.AI.OpenAI.Models.Services.Foundations.ChatCompletions;
using Telegram.Bot.Types;
using TFB.Models;
using TFB.Services;

namespace TFB;

public class AnalysisService
{
    public List<Models.Message> Messages = new List<Models.Message>();
    public List<Models.Message> OwnMessages = new List<Models.Message>();
    public List<Models.Message> UserBotDiscourse = new List<Models.Message>();
    public List<Models.Message> CombinedMessages = new List<Models.Message>();
    public Personality _personality;
    
    private OpenAIClient _aiClient;
    private OpenRouterService _aiRouterClient;
    private ChatSettings _chatSettings;
    private ChatTypes _chatType;
    public string Command = "/batman";
    public string Name = "Batman";
    public string Compressed = "";

    public string Template =
        @"You are Batman, 
Make sure to keep responses to one paragraph  Here is the context of the messages: {0}";

    public string MessageTemplate = "Author: {0} \n Message: {1} \n Date Posted {2} \n";
    
    public AnalysisService(OpenAIClient aiClient, ChatSettings chatSettings, Personality personality, ChatTypes chatType, OpenRouterService aiRouterClient)
    {
        _aiClient = aiClient;
        _chatSettings = chatSettings;
        _personality = personality;
        _chatType = chatType;
        _aiRouterClient = aiRouterClient;
    }

    public void SetPersonality(Personality personality)
    {
        this._personality = personality;
    }

    public static AnalysisService GetAnalyzer(Personality personality, OpenAIClient aiClient, ChatSettings chatSettings, OpenRouterService openRouterService)
    {
        var analyzer = new AnalysisService(aiClient, chatSettings, personality, ChatTypes.OpenAi, openRouterService);
        analyzer.Template = personality.PersonalityDescription;
        analyzer.Command = personality.Command;
        analyzer.Name = personality.Name;
        return analyzer;
    }

    public void AddMessage(Models.Message message)
    {
        if (Messages.Count >= 100)
        {
            Messages.RemoveAt(0);
        }
        
        Messages.Add(message);
    }


    public string BuildMessages()
    {
        var outputString = "";
        for(var i = 0; i < Messages.Count; i++)
        {
            var message = Messages[i];
            if (message.Value.ToLower() == Command)
            {
                continue;
            }

            message.Value = message.Value.Replace(Command, "");
            outputString += String.Format(MessageTemplate, message.Author, message.Value,
                message.DatePosted.ToString("f"));
        }

        return outputString;
    }

    public string BuildCombinedMessages()
    {
        var outputString = "";
        for(var i = 0; i < CombinedMessages.Count; i++)
        {
            var message = CombinedMessages[i];
            if (message.Value.ToLower() == Command)
            {
                continue;
            }
            outputString += String.Format(MessageTemplate, message.Author, message.Value,
                message.DatePosted.ToString("f"));
        }

        return outputString;
    }
    
    public string BuildMessagesHistory()
    {
        var outputString = "";
        var ownMessagesSelection = OwnMessages.GetRange(0, Math.Min(5, OwnMessages.Count));
        for(var i = ownMessagesSelection.Count - 1; i >= 0; i--)
        {
            var message = ownMessagesSelection[i];
            if (message.Value.ToLower() == Command)
            {
                continue;
            }

            outputString += message.Value;
        }

        return outputString;
    }

    public string BuildMostRecentMessage(bool preamble = true)
    {
        var outputString = "";

        var format = MessageTemplate;

        if (preamble)
        {
            format =
                "Respond directly in character to this message: " +
                MessageTemplate;
        }
        
        if (Messages.Count > 0)
        {
            var lastMessage = Messages[^1];
            outputString += String.Format(format, lastMessage.Author, lastMessage.Value,
                lastMessage.DatePosted.ToString("f"));
        }

        return outputString;
    }
    
    public async Task<string> Analysis()
    {
        var msgs = BuildChatCompletionMessagesBasedOnHistory();
        string message = String.Empty;
        
        switch (_chatType)
        {
            case ChatTypes.OpenAi:
            {
                var completion = await ChatService.SendChat(msgs.ToArray(), _aiClient,
                    _personality.Temperature ?? _chatSettings.Temperature);

                if (completion == null || completion.Response == null)
                {
                    return "";
                }

                message = completion.Response.Choices.FirstOrDefault()?.Message.Content ?? string.Empty;
                break;
            }
            case ChatTypes.OpenRouter:
            {
                var messages = msgs.Select(msg => new Services.Message()
                {
                    Content = msg.Content,
                    Role = msg.Role
                }).ToList();
                var completion = await ChatService.SendChat(messages.ToArray(), _aiRouterClient, temperature: _personality.Temperature ?? _chatSettings.Temperature, _personality.Model);

                message = completion?.Choices?.FirstOrDefault()?.Message.Content ?? "";
                break;
            }
        }

        OwnMessages.Add(new Models.Message()
        {
            Value = message,
            DatePosted = DateTime.Now
        });
        
        return message;
    }

    private List<ChatCompletionMessage> BuildChatCompletionMessagesBasedOnHistory()
    {
        var anslysisPrompt = String.Format(Template, !string.IsNullOrEmpty(Compressed) ? Compressed : BuildMessages());
        var mostRecentPrompt = BuildMostRecentMessage();
        var msgs = new List<ChatCompletionMessage>()
        {
            new()
            {
                Content = anslysisPrompt,
                Role = "system"
            }
        };

        foreach (var discourseMessage in UserBotDiscourse)
        {
            var chatCompletionMessage = new ChatCompletionMessage()
            {
                Content = discourseMessage.Value,
                Role = (discourseMessage.MessageType ?? MessageType.User) == MessageType.User ? "user" : "assistant"
            };
            
            msgs.Add(chatCompletionMessage);
        }
        
        msgs.Add(
            new()
            {
                Content = mostRecentPrompt,
                Role = "user"
            }
        );
        return msgs;
    }
    
    public void WipeMessages()
    {
        Messages = new List<Models.Message>();
        OwnMessages = new List<Models.Message>();
        CombinedMessages = new List<Models.Message>();
        UserBotDiscourse = new List<Models.Message>();
        Compressed = "";
    }
}