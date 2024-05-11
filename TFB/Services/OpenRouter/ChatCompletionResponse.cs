using Newtonsoft.Json;

namespace TFB.Services.OpenRouter;

public class ChatCompletionResponse
{
    [JsonProperty("choices")]
    public Choice[] Choices { get; set; }
    [JsonProperty("model")]
    public string Model { get; set; }
}

public class LocalChatCompletionResponse
{
    [JsonProperty("model")] 
    public string Model { get; set; }
    [JsonProperty("response")]
    public string Response { get; set; }
    [JsonProperty("done")]
    public bool Done { get; set; }
    [JsonProperty("message")]
    public LocalChatCompletionMessages Choices { get; set; }

}


public class LocalChatCompletionMessages
{
    [JsonProperty("role")]
    public string Role { get; set; }
    [JsonProperty("content")]
    public string Content { get; set; }
}