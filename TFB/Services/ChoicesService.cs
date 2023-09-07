using Newtonsoft.Json;
using Standard.AI.OpenAI.Clients.OpenAIs;
using Standard.AI.OpenAI.Models.Services.Foundations.ChatCompletions;

namespace TFB.Services;

public class ChoicesService
{
    private readonly OpenAIClient _client;
    public ChoicesService(OpenAIClient client)
    {
        _client = client;
    }

    public async Task<ChoicesResponse> GetPotentialChoices(ChoicesRequest request)
    {
        var messages = new string[]
        {
            $@"The following is a message from {request.Author}. Respond in a JSON object what you think the possible choices are given the message. This is the message: {request.Message}
Respond in JSON format like so:
{{
    'choices':[
    {{  
        'name':'Verb Action',
        'command':'first person response'
    }},
    {{  
        'name':'Verb Action',
        'command':'first person response'
    }}
    ]        
}} "
        };

        var chatCompletion = new ChatCompletion()
        {
            Request = new Standard.AI.OpenAI.Models.Services.Foundations.ChatCompletions.ChatCompletionRequest()
            {
                Model = "gpt-3.5-turbo-0613",
                Messages = messages.Select(m => new ChatCompletionMessage() { Content = m, Role = "user" }).ToArray(),
                Temperature = 0.7,
                MaxTokens = 3000
            }
        };
        
        ChatCompletion? result = new ChatCompletion();
        var response = new ChoicesResponse();
        try
        {
            result = await _client
                .ChatCompletions
                .SendChatCompletionAsync(chatCompletion);
            
            var content = result.Response.Choices.FirstOrDefault()?.Message?.Content ?? "";
            if (!string.IsNullOrEmpty(content))
            {
                response = JsonConvert.DeserializeObject<ChoicesResponse>(content);
            }


            foreach (var responseChoice in response.Choices)
            {
                responseChoice.Command = $"{request.Command} {responseChoice.Command}";
                if (responseChoice.Name.Length > 50)
                {
                    responseChoice.Name = responseChoice.Name[..50];
                }
                if (responseChoice.Command.Length > 50)
                {
                    responseChoice.Command = responseChoice.Command[..50];
                }
                
            }

            if (response.Choices.Count() > 4)
            {
                response.Choices = response.Choices.Take(4);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return response;
    }
}

public class ChoicesRequest
{
    public string Message { get; set; }
    public string Author { get; set; }
    public string Command { get; set; }
}

public class ChoicesResponse {
    [JsonProperty("choices")]
    public IEnumerable<ChoicesChoice> Choices { get; set; }
}

public class ChoicesChoice
{
    [JsonProperty("name")]
    public string Name { get; set; }
    [JsonProperty("command")]
    public string Command { get; set; }
}