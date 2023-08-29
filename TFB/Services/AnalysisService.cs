using Standard.AI.OpenAI.Clients.OpenAIs;
using Standard.AI.OpenAI.Models.Services.Foundations.ChatCompletions;
using Telegram.Bot.Types;
using TFB.Models;
using Message = TFB.Models.Message;

namespace TFB;

public class AnalysisService
{
    public List<Models.Message> Messages = new List<Models.Message>();
    public List<Models.Message> OwnMessages = new List<Models.Message>();
    public Personality _personality;
    
    private OpenAIClient _aiClient;
    private ChatSettings _chatSettings;
    public string Command = "/batman";
    public string Name = "Batman";
    public string Compressed = "";

    public string Template =
        @"You are Batman, 
Make sure to keep responses to one paragraph  Here is the context of the messages: {0}";

    public string MessageTemplate = "Author: {0} \n Message: {1} \n Date Posted {2} \n";
    
    public AnalysisService(OpenAIClient aiClient, ChatSettings chatSettings, Personality personality)
    {
        _aiClient = aiClient;
        _chatSettings = chatSettings;
        _personality = personality;
    }

    public void SetPersonality(Personality personality)
    {
        this._personality = personality;
    }

    public static AnalysisService GetAnalyzer(Personality personality, OpenAIClient aiClient, ChatSettings chatSettings)
    {
        var analyzer = new AnalysisService(aiClient, chatSettings, personality);
        analyzer.Template = personality.PersonalityDescription;
        analyzer.Command = personality.Command;
        analyzer.Name = personality.Name;
        return analyzer;
    }

    public void AddMessage(Message message)
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
                "Respond directly in character to this message, not to the previous context, but to the most recent message here: " +
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
        var anslysisPrompt = String.Format(Template, !string.IsNullOrEmpty(Compressed) ? Compressed : BuildMessages());

        while (TokenEstimatorService.EstimateTokens(anslysisPrompt) > 16000 && Messages.Count > 0)
        {
            try
            {
                Messages = Messages.GetRange((Messages.Count / 2) - 1, Messages.Count / 2);
                anslysisPrompt = String.Format(Template, BuildMessages());
            }
            catch (Exception e)
            {
                Console.WriteLine("We have a bad sitch " + e.Message);
                return "";
            }
        }
        Console.WriteLine(anslysisPrompt);

        var mostRecentPrompt = BuildMostRecentMessage();
        Console.WriteLine(mostRecentPrompt);

        var ownMessages = BuildMessagesHistory();
        Console.WriteLine(ownMessages);
        var msgs = new List<ChatCompletionMessage>()
        {
            new()
            {
                Content = ownMessages,
                Role = "assistant"
            },
            new()
            {
                Content = anslysisPrompt,
                Role = "system"
            },
            new()
            {
                Content = mostRecentPrompt,
                Role = "user"
            }
        };

        var completion = await ChatService.SendChat(msgs.ToArray(), _aiClient, _personality.Temperature ?? _chatSettings.Temperature);

        if (completion == null || completion.Response == null)
        {
            return "";
        }
        
        var message = completion.Response.Choices.FirstOrDefault()?.Message.Content ?? string.Empty;
        
        OwnMessages.Add(new Models.Message()
        {
            Value = message,
            DatePosted = DateTime.Now
        });

        ClearMessages();
        
        return message;
    }

    protected virtual void ClearMessages()
    {
        //Messages = new List<Message>();
    }
    
    public void WipeMessages()
    {
        Messages = new List<Models.Message>();
    }
}