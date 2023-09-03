using Standard.AI.OpenAI.Clients.OpenAIs;
using Standard.AI.OpenAI.Models.Clients.Completions.Exceptions;
using Standard.AI.OpenAI.Models.Services.Foundations.ChatCompletions;
using Standard.AI.OpenAI.Models.Services.Foundations.ChatCompletions.Exceptions;
using Standard.AI.OpenAI.Models.Services.Foundations.Completions;
using TFB.Models;
using TFB.Services;

namespace TFB;

public class ChatService
{
    
    public static ChatSettings ChatSettings { get; set; }
    public static async Task<ChatCompletion?> SendChat(ChatCompletionMessage[] msgs, OpenAIClient client, double temperature = 0.7)
    {
        string error;
        ChatCompletion? result;
        try
        {
            var chatCompletion = new ChatCompletion
            {
                Request = new Standard.AI.OpenAI.Models.Services.Foundations.ChatCompletions.ChatCompletionRequest
                {
                    Model = "gpt-3.5-turbo-0613",
                    Messages = msgs,
                    Temperature = temperature,
                    MaxTokens = 3000
                }
            };
            result = await client
                .ChatCompletions
                .SendChatCompletionAsync(chatCompletion);
        }
        catch (InvalidChatCompletionException e)
        {
            Console.WriteLine($"Invalid completion {e.Message} {e.InnerException?.Message}");
            result = new ChatCompletion();
            error = "Something went wrong...";
        }
        catch (CompletionClientValidationException e)
        {
            Console.WriteLine($"Client validation exception {e.Message}");
            result = new ChatCompletion();
            
            error = "Something went wrong...";
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            result = new ChatCompletion();
            
            error = "Something went wrong...";
        }

        return result;
    }
    
    public static async Task<Services.ChatCompletionResponse?> SendChat(Services.Message[] msgs, OpenRouterService client, double temperature = 0.7)
    {
        string error;
        Services.ChatCompletionResponse? result;
        try
        {
            var chatCompletion = new Services.ChatCompletionRequest()
            {
                Model = client.Model,
                Messages = msgs,
                Temperature = temperature
            };
            result = await client.SendRequestAsync(chatCompletion);
        }
        catch (InvalidChatCompletionException e)
        {
            Console.WriteLine($"Invalid completion {e.Message} {e.InnerException?.Message}");
            result = new Services.ChatCompletionResponse();
            error = "Something went wrong...";
        }
        catch (CompletionClientValidationException e)
        {
            Console.WriteLine($"Client validation exception {e.Message}");
            result = new Services.ChatCompletionResponse();
            
            error = "Something went wrong...";
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            result = new Services.ChatCompletionResponse();
            
            error = "Something went wrong...";
        }

        return result;
    }

}