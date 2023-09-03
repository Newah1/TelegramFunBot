namespace TFB.Services;


using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class OpenRouterService
{
    public string BaseUrl = "https://openrouter.ai/api/v1/chat/completions";
    private string _apiKey = string.Empty;
    private string _model;

    public OpenRouterService (string apiKey, string model = "meta-llama/llama-2-70b-chat")
    {
        _apiKey = apiKey;
        _model = model;
    }
    public async Task<ChatCompletionResponse?> SendRequestAsync(ChatCompletionRequest request)
    {
        // Create an HttpClient instance
        using var client = new HttpClient();
        // Prepare the request headers
        //client.DefaultRequestHeaders.Add("Content-Type", "application/json");
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        client.DefaultRequestHeaders.Add("HTTP-Referer", "https://google.com/");
        client.DefaultRequestHeaders.Add("X-Title", "TelegramFunBot");

        request.Transforms = new[] { "middle-out" };
        // Serialize the request model to JSON
        string jsonContent = JsonConvert.SerializeObject(request);
        jsonContent = jsonContent.Replace("\\n", "");
        request.Model = _model;

        // Convert the JSON content to a StringContent object
        StringContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Send the POST request and get the response
        HttpResponseMessage httpResponse = await client.PostAsync(BaseUrl, content);

        // Check if the request was successful
        if (httpResponse.IsSuccessStatusCode)
        {
            // Read the response content as a string
            string responseBody = await httpResponse.Content.ReadAsStringAsync();

            // Deserialize the JSON response into ChatCompletionResponse model
            ChatCompletionResponse? response = JsonConvert.DeserializeObject<ChatCompletionResponse>(responseBody);

            return response;
        }
        else
        {
            Console.WriteLine($"Request failed with status code: {httpResponse.StatusCode}");
        }


        return null;
    }

}


public class ChatCompletionRequest
{
    [JsonProperty("max_tokens")] public int MaxTokens { get; set; } = 8000;
    [JsonProperty("temperature")] 
    public double Temperature { get; set; } = 0.80;
    [JsonProperty("transforms")]
    public string[] Transforms { get; set; }
    [JsonProperty("model")]
    public string Model { get; set; }
    [JsonProperty("messages")]
    public Message[] Messages { get; set; }

}

public class ChatCompletionResponse
{
    [JsonProperty("choices")]
    public Choice[] Choices { get; set; }
    [JsonProperty("model")]
    public string Model { get; set; }
}

public class Choice
{
    [JsonProperty("finish_reason")]
    public string FinishReason { get; set; }
    [JsonProperty("message")]
    public Message Message { get; set; }
}

public class Message
{
    [JsonProperty("role")]
    public string Role { get; set; }
    [JsonProperty("content")]
    public string Content { get; set; }
}