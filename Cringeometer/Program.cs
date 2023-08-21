// See https://aka.ms/new-console-template for more information

using Cringeometer;
using Cringeometer.Models;
using Microsoft.Extensions.Configuration;
using Standard.AI.OpenAI.Clients.OpenAIs;
using Standard.AI.OpenAI.Models.Configurations;
using Standard.AI.OpenAI.Models.Services.Foundations.ChatCompletions;
using Telegram.Bot;
using Telegram.Bot.Types;

Console.WriteLine("Hello, World!");

IConfiguration configurationBuilder = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();

var chatSettings = new ChatSettings();
configurationBuilder.GetSection("ChatSettings").Bind(chatSettings);
var telegramSettings = new TelegramSettings();
configurationBuilder.GetSection("TelegramSettings").Bind(telegramSettings);
var generalSettings = new GeneralSettings();
configurationBuilder.GetSection("GeneralSettings").Bind(generalSettings);

ChatService.ChatSettings = chatSettings;


var openAIConfigurations = new OpenAIConfigurations
{   
    ApiKey = chatSettings.SecretKey
};
var openAiClient = new OpenAIClient(openAIConfigurations);

var personalities = new List<Personality>();
configurationBuilder.GetSection("Personalities").Bind(personalities);


var analyzers = new List<AnalysisService>();

foreach (var personality in personalities)
{
    analyzers.Add(AnalysisService.GetAnalyzer(personality, openAiClient));
}

// Telegram 

var botClient = TelegramService.SetupClient(telegramSettings);

//botClient.OnUpdateActions.Add(HandleUpdateAsync);
botClient.OnUpdateActions.Add(HandleAnalysis);

await botClient.StartReceiving();

async void HandleAnalysis(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    // Only process Message updates: https://core.telegram.org/bots/api#message
    if (update.Message is not { } message)
        return;
    // Only process text messages
    if (message.Text is not { } messageText)
        return;
    
    var chatId = message.Chat.Id;

    var message1 = new Cringeometer.Models.Message()
    {
        Author = message.From.Username + " " + message.From.FirstName,
        DatePosted = message.Date,
        Value = messageText
    };

    foreach (var analysisService in analyzers)
    {
        analysisService.AddMessage(message1);
    }
    
    Console.WriteLine($"Got a message from {message.From.FirstName} with contents: {messageText}");
    string analysis = "";
    foreach (var analysisService in analyzers)
    {
        if (messageText.ToLower().Contains(analysisService.Command))
        {
            Console.WriteLine($"Handling analysis with ");
            analysis = await analysisService.Analysis();
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: analysis,
                cancellationToken: cancellationToken, replyToMessageId: message.ReplyToMessage?.MessageId);
            
            // give other analyzers your most recent message
            foreach (var otherAnalyzers in analyzers.Where(a => a != analysisService).ToList())
            {
                Console.WriteLine($"Giving {otherAnalyzers.Name} the most recent message [{analysis.Substring(0, 10)}...]");
                otherAnalyzers.AddMessage(
                        new Cringeometer.Models.Message()
                        {
                            Author = analysisService.Name,
                            DatePosted = DateTime.Now,
                            Value = analysis
                        }
                    );
            }
        }
    }
}

async void HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    // Only process Message updates: https://core.telegram.org/bots/api#message
    if (update.Message is not { } message)
        return;
    // Only process text messages
    if (message.Text is not { } messageText)
        return;
    var chatId = message.Chat.Id;

    
    
    var cringeMessage = "Oh no! Cringe! https://www.youtube.com/watch?v=oZaLXmkbO3E";

    var chatMsg = new ChatCompletionMessage() 
    { 
        Content = $"You are playing the role of a cringe-ometer. You will evaluate a passed in string and return a 5 place floating point number from 0 to 1 returning the estimated level of cringe. Make sure you don't misidentify jokes as cringe. You will not respond with any words other than your evaluation of cringe. Evaluate this: '{messageText}'", 
        Role = "user" 
    };

    var response = await ChatService.SendChat(new [] {chatMsg}, openAiClient);

    var cringeLevel = 0.0d;

    if (response != null)
    {
        try
        {
            cringeLevel = Double.Parse(response.Response.Choices.FirstOrDefault().Message.Content);
            Console.WriteLine($"{cringeLevel} < cringe!");
        }
        catch (Exception e)
        {
            Console.WriteLine("Error parsing cringe " + e.Message);
        }
    }

    if (cringeLevel >= generalSettings.MinCringeLevel)
    {
        Console.WriteLine("Uh oh, cringe!");
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: messageText + " " + cringeMessage,
            cancellationToken: cancellationToken, replyToMessageId: message.ReplyToMessage?.MessageId);
    }
    

    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");
    
}


Console.ReadLine();
Console.WriteLine("ending");