using Newtonsoft.Json;

namespace TFB.Services.OpenRouter;

public class ChatCompletionResponse
{
    [JsonProperty("choices")]
    public Choice[] Choices { get; set; }
    [JsonProperty("model")]
    public string Model { get; set; }
}