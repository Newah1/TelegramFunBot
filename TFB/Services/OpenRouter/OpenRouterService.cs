using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TFB.DTOs.Settings;

namespace TFB.Services.OpenRouter;

public class OpenRouterService : IOpenRouterService
{
    private readonly ILogger<IOpenRouterService> _logger;
    public string BaseUrl = "https://openrouter.ai/api/v1/chat/completions";
    private string _apiKey = string.Empty;
    public string Model { get; internal set; }

    public OpenRouterService (OpenRouterSettings openRouterSettings, ILogger<IOpenRouterService> logger)
    {
        _logger = logger;
        _apiKey = openRouterSettings.ApiKey;
        Model = openRouterSettings.Model;
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
        request.Model = Model;

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
            _logger.LogError($"Error getting response from OpenRouter {httpResponse.StatusCode.ToString()}");
            string responseBody = await httpResponse.Content.ReadAsStringAsync();
            _logger.LogError($"Error getting response from OpenRouter {responseBody}");
        }


        return null;
    }

}