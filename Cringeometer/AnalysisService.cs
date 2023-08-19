using Cringeometer.Models;
using Standard.AI.OpenAI.Clients.OpenAIs;
using Standard.AI.OpenAI.Models.Services.Foundations.ChatCompletions;

namespace Cringeometer;

public class AnalysisService
{
    public List<Message> Messages = new List<Message>();
    private OpenAIClient _aiClient;

    public string Template =
        "You are batman. Make sure to keep responses to one paragraph  Respond in-character as Batman to the following messages: {0}";

    public string MessageTemplate = "Author: {0} \n Message: {1} \n Date Posted {2} \n";
    
    public AnalysisService(OpenAIClient aiClient)
    {
        _aiClient = aiClient;
    }

    public void AddMessage(Message message)
    {
        if (Messages.Count >= 100)
        {
            Messages.RemoveAt(0);
        }
        
        Messages.Add(message);
    }


    private string BuildMessages()
    {
        var outputString = "";
        foreach (var message in Messages)
        {
            if (message.Value == "/batman" || message.Value[0] == '/')
            {
                continue;
            }
            outputString += String.Format(MessageTemplate, message.Author, message.Value,
                message.DatePosted.ToString("f"));
        }

        return outputString;
    }
    
    public async Task<string> Analysis()
    {
        var anslysisPrompt = String.Format(Template, BuildMessages());

        while (TokenEstimatorService.EstimateTokens(anslysisPrompt) > 4500)
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

        var completion = await ChatService.SendChat(new ChatCompletionMessage()
        {
            Content = anslysisPrompt,
            Role = "user"
        }, _aiClient);
        
        var message = completion.Response.Choices.FirstOrDefault().Message.Content ?? string.Empty;

        Messages = new List<Message>();
        
        return message;
    }
}