using Newtonsoft.Json;

namespace TFB.Services.OpenRouter;

public class ChatCompletionRequest
{
    [JsonProperty("max_tokens")] public int MaxTokens { get; set; } = 3000;
    [JsonProperty("temperature")] 
    public double Temperature { get; set; } = 0.80;
    [JsonProperty("transforms")]
    public string[] Transforms { get; set; }
    [JsonProperty("model")]
    public string Model { get; set; }
    [JsonProperty("messages")]
    public Message[] Messages { get; set; }

}

public class LocalChatCompletionRequest : ChatCompletionRequest
{
    [JsonProperty("stream")]
    public bool Stream { get; set; }

}