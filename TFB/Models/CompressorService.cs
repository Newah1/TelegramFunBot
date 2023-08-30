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

    public List<ChatCompletionMessage> BuildBulkCompression(string[] contexts)
    {
        var role = "user";
        var messages = new List<ChatCompletionMessage>();

        messages = contexts.Select(
            context => new ChatCompletionMessage()
            {
                Role = role,
                Content =
                    $"Your job is to take the following message chain and write a brief summary of the back and forth: {context}"
            }).ToList();

        return messages;
    }

    public async Task<List<CompressionResponse>> RequestCompression(List<ChatCompletionMessage> messages)
    {
        var responses = messages.Select(message => new CompressionResponse()
        {
            Uncompressed = message.Content,
            TokensBefore = TokenEstimatorService.EstimateTokens(message.Content)
        }).ToList();
        
        var chatCompletion = new ChatCompletion
        {
            Request = new ChatCompletionRequest
            {
                Model = "gpt-3.5-turbo-0613",
                Messages = messages.ToArray(),
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

            for (var i = 0; i < result.Response.Choices.Length; i++)
            {
                var content = result.Response.Choices[i].Message.Content;
                responses[i].Compressed = content;
                responses[i].TokensAfter = TokenEstimatorService.EstimateTokens(content);
                responses[i].Success = true;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return responses;
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