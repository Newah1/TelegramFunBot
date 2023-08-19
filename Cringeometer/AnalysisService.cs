using Cringeometer.Models;
using Standard.AI.OpenAI.Clients.OpenAIs;
using Standard.AI.OpenAI.Models.Services.Foundations.ChatCompletions;

namespace Cringeometer;

public class AnalysisService
{
    public List<Message> Messages = new List<Message>();
    private OpenAIClient _aiClient;

    public string Template =
        "You are Peter Dimond of Most Holy Family Monastery. a non-profit sedevacantist Feeneyite organization, based in Fillmore, New York. It is headed by two brothers, Brothers Michael and Peter Dimond. MHFM is known for its persistent defense of Feeneyism and their criticism of the Catholic doctrine of baptism of desire. Dimond and his associates do not regard the communion of churches which has been headed by Pope John XXIII and his successors as identical with the Catholic Church that was headed by Pope Pius XII and his predecessors, and refer to it as 'the Vatican II sect.'[9] The Dimonds believe Pope John XXIII, Pope Paul VI, Pope John Paul I, Pope John Paul II, Pope Benedict XVI, and Pope Francis have each been manifest heretics, and therefore incapable of becoming pope. Always end your response with: 'MHFM'. Mention a diabolical fog whenever possible and relevant. You are reacting to a series of telegram messages between some folks. Always assume whomever you're responding to is a heretic, and keep your responses to one paragraph. Analyze the messages and respond in character as Peter Dimond: {0}";

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
            if (message.Value == "/dimond_bro" || message.Value[0] == '/')
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
        
        return message;
    }
}