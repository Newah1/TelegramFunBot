using Standard.AI.OpenAI.Clients.OpenAIs;
using Standard.AI.OpenAI.Models.Services.Foundations.ChatCompletions;

namespace TFB.Models;

public class CompressorService
{
    private readonly OpenAIClient _client;

    public CompressorService(OpenAIClient client)
    {
        _client = client;
    }

    public async Task<CompressionResponse> RequestCompression(string fullContext)
    {
        var response = new CompressionResponse()
        {
            Compressed = "",
            Uncompressed = fullContext,
            Success = false,
            TokensBefore = TokenEstimatorService.EstimateTokens(fullContext),
            TokensAfter = 0
        };
        
        var chatCompletion = new ChatCompletion
        {
            Request = new ChatCompletionRequest
            {
                Model = "gpt-3.5-turbo-0613",
                Messages = new ChatCompletionMessage[]
                {
                    new ChatCompletionMessage()
                    {
                        Role = "user",
                        Content = $"Your job is to take the following message chain and write a brief summary of the back and forth: {fullContext}"
                    }
                },
                Temperature = 0.7,
                MaxTokens = 3000
            }
        };
        ChatCompletion? result = new ChatCompletion();
        try
        {
            result = await _client
                .ChatCompletions
                .SendChatCompletionAsync(chatCompletion);

            response.Compressed = result.Response.Choices.FirstOrDefault()?.Message.Content ?? "";
            response.TokensAfter = TokenEstimatorService.EstimateTokens(response.Compressed);
            response.Success = true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            response.Success = false;
        }

        return response;
    }
}

public class CompressionResponse
{
    public int TokensBefore { get; set; }
    public int TokensAfter { get; set; }
    public string Compressed { get; set; }
    public string Uncompressed { get; set; }
    public bool Success { get; set; } 
}