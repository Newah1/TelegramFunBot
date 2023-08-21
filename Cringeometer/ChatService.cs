using Cringeometer.Models;
using Standard.AI.OpenAI.Clients.OpenAIs;
using Standard.AI.OpenAI.Models.Services.Foundations.ChatCompletions;

namespace Cringeometer;

public class ChatService
{
    
    public static ChatSettings ChatSettings { get; set; }
    public static async Task<ChatCompletion?> SendChat(ChatCompletionMessage[] msgs, OpenAIClient client)
    {
        var chatCompletion = new ChatCompletion
        {
            Request = new ChatCompletionRequest
            {
                Model = "gpt-3.5-turbo",
                Messages = msgs, 
                Temperature = ChatSettings.Temperature,
                MaxTokens = 800
            }
        };
        var result = await client
            .ChatCompletions
            .SendChatCompletionAsync(chatCompletion);

        return result;
    }
}