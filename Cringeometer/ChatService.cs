using Standard.AI.OpenAI.Clients.OpenAIs;
using Standard.AI.OpenAI.Models.Services.Foundations.ChatCompletions;

namespace Cringeometer;

public class ChatService
{
    public static async Task<ChatCompletion?> SendChat(ChatCompletionMessage msg, OpenAIClient client)
    {
        var chatCompletion = new ChatCompletion
        {
            Request = new ChatCompletionRequest
            {
                Model = "gpt-3.5-turbo",
                Messages = new [] { msg }, 
                Temperature = 0.2,
                MaxTokens = 800
            }
        };
        var result = await client
            .ChatCompletions
            .SendChatCompletionAsync(chatCompletion);

        return result;
    }
}