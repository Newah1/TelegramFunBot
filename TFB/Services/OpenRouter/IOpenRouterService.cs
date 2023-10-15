namespace TFB.Services.OpenRouter;

public interface IOpenRouterService
{
    string Model { get; }
    Task<ChatCompletionResponse?> SendRequestAsync(ChatCompletionRequest request);
}