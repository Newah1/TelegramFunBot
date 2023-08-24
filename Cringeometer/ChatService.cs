using Cringeometer.Models;
using Standard.AI.OpenAI.Clients.OpenAIs;
using Standard.AI.OpenAI.Models.Services.Foundations.ChatCompletions;
using Standard.AI.OpenAI.Models.Services.Foundations.ChatCompletions.Exceptions;

namespace Cringeometer;

public class ChatService
{
    
    public static ChatSettings ChatSettings { get; set; }
    public static async Task<ChatCompletion?> SendChat(ChatCompletionMessage[] msgs, OpenAIClient client, double temperature = 0.7)
    {
        ChatCompletion? result;
        try
        {
            var chatCompletion = new ChatCompletion
            {
                Request = new ChatCompletionRequest
                {
                    Model = "gpt-3.5-turbo-16k",
                    Messages = msgs,
                    Temperature = temperature,
                    MaxTokens = 8000
                }
            };
            result = await client
                .ChatCompletions
                .SendChatCompletionAsync(chatCompletion);
        }
        catch (InvalidChatCompletionException e)
        {
            Console.WriteLine($"Invalid completion {e} {e.InnerException?.Message}");
            result = new ChatCompletion();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            result = new ChatCompletion();
        }

        return result;
    }
}