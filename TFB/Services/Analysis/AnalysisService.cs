using Standard.AI.OpenAI.Clients.OpenAIs;
using Standard.AI.OpenAI.Models.Services.Foundations.ChatCompletions;
using TFB.DTOs;
using TFB.DTOs.Settings;
using TFB.Models;
using TFB.Services.OpenRouter;
using Message = TFB.DTOs.Message;
using Personality = TFB.DTOs.Personality;

namespace TFB.Services.Analysis;

public class AnalysisService
{
    public List<DTOs.Message> Messages;
    public List<DTOs.Message> OwnMessages;
    public List<DTOs.Message> UserBotDiscourse;
    public List<DTOs.Message> CombinedMessages;
    
    private OpenAIClient _aiClient;
    private IOpenRouterService _aiRouterClient;
    private ChatSettings _chatSettings;
    
    public string Command = "/batman";
    public string Name = "Batman";
    public string Compressed = "";

    public string Template =
        @"You are Batman, 
Make sure to keep responses to one paragraph  Here is the context of the messages: {0}";

    public string MessageTemplate = "Author: {0} \n Message: {1} \n Date Posted {2} \n";
    
    public AnalysisService(OpenAIClient aiClient, ChatSettings chatSettings, IOpenRouterService aiRouterClient)
    {
        _aiClient = aiClient;
        _chatSettings = chatSettings;
        _aiRouterClient = aiRouterClient;

        Messages = new List<Message>();
        OwnMessages = new List<Message>();
        CombinedMessages = new List<Message>();
        UserBotDiscourse = new List<Message>();
    }

    public bool MatchesCommand(string command)
    {
        return command.ToLower().Trim() == Command.Trim().ToLower();
    }

    public void AddMessage(DTOs.Message message)
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

    public static string BuildCombinedMessages(List<Message> messages, string command, string messageTemplate)
    {
        var outputString = "";
        for(var i = 0; i < messages.Count; i++)
        {
            var message = messages[i];
            if (message.Value.ToLower() == command)
            {
                continue;
            }
            outputString += String.Format(messageTemplate, message.Author, message.Value,
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
    
    public async Task<AnalysisResponse> Analysis(AnalysisRequest request)
    {
        AnalysisResponse response = new AnalysisResponse();

        var msgs = BuildChatCompletionMessagesBasedOnHistory(request.Personality);
        response.ChatCompletionChoices = msgs;
        string message = String.Empty;
        
        switch (request.ChatTypes)
        {
            case ChatTypes.OpenAi:
            {
                var completion = await ChatService.SendChat(msgs.ToArray(), _aiClient,
                    request.Personality.Temperature ?? _chatSettings.Temperature);

                if (completion == null || completion.Response == null)
                {
                    return response;
                }

                message = completion.Response.Choices.FirstOrDefault()?.Message.Content ?? string.Empty;
                break;
            }
            case ChatTypes.OpenRouter:
            {
                var messages = msgs.Select(msg => new Services.OpenRouter.Message()
                {
                    Content = msg.Content,
                    Role = msg.Role
                }).ToList();
                var completion = await ChatService.SendChat(messages.ToArray(), _aiRouterClient, temperature: request.Personality.Temperature ?? _chatSettings.Temperature, request.Personality.Model);

                message = completion?.Choices?.FirstOrDefault()?.Message.Content ?? "";
                break;
            }
        }

        OwnMessages.Add(new DTOs.Message()
        {
            Value = message,
            DatePosted = DateTime.Now
        });

        response.Message = message;
        if (!string.IsNullOrEmpty(message))
        {
            response.Success = true;
        }

        return response;
    }

    private List<ChatCompletionMessage> BuildChatCompletionMessagesBasedOnHistory(Personality personality)
    {
        var msgs = new List<ChatCompletionMessage>();
        
        msgs.Add(new ChatCompletionMessage()
        {
            Role = "system",
            Content = String.Format(personality.PersonalityDescription, "")
        });
        
        foreach (var message in personality.MessageHistory)
        {
            msgs.Add(new ChatCompletionMessage()
            {
                Content = message.Value,
                Role = message.MessageType == MessageType.Bot ? "assistant" : "user"
            });
        }
        
        return msgs;
    }
    
    public void WipeMessages()
    {
        Messages = new List<DTOs.Message>();
        OwnMessages = new List<DTOs.Message>();
        CombinedMessages = new List<DTOs.Message>();
        UserBotDiscourse = new List<DTOs.Message>();
        Compressed = "";
    }
}