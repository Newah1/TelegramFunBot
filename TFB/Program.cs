// See https://aka.ms/new-console-template for more information

using System.Text.RegularExpressions;
using TFB;
using TFB.Models;
using Microsoft.Extensions.Configuration;
using Standard.AI.OpenAI.Clients.OpenAIs;
using Standard.AI.OpenAI.Models.Configurations;
using Standard.AI.OpenAI.Models.Services.Foundations.ChatCompletions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TFB.Services;
using MessageType = TFB.Models.MessageType;

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

var sheetsSettings = new SheetsSettings();
configurationBuilder.GetSection("SheetsSettings").Bind(sheetsSettings);

var openRouterSettings = new OpenRouterSettings();
configurationBuilder.GetSection("OpenRouterSettings").Bind(openRouterSettings);


var personalitySheetService = new PersonalitySheetsService(sheetsSettings);
var personalitiesFromSheet = personalitySheetService.LoadPersonalities();

ChatService.ChatSettings = chatSettings;


var openAiConfigurations = new OpenAIConfigurations
{   
    ApiKey = chatSettings.SecretKey
};
var openAiClient = new OpenAIClient(openAiConfigurations);

var openRouterService = new OpenRouterService(openRouterSettings.ApiKey, openRouterSettings.Model);

var compressorService = new CompressorService(openAiClient);
var choicesService = new ChoicesService(openAiClient);

var personalities = new List<Personality>();
configurationBuilder.GetSection("Personalities").Bind(personalities);

personalities.AddRange(personalitiesFromSheet);


var analyzers = new List<AnalysisService>();

foreach (var personality in personalities)
{
    analyzers.Add(AnalysisService.GetAnalyzer(personality, openAiClient, chatSettings, openRouterService));
}

var lastTimeRanSpreadsheet = DateTime.Now;

// Telegram 

var botClient = TelegramService.SetupClient(telegramSettings);

var botHandlerService = new BotHandlerService(
    analyzers, botClient, personalitySheetService, 
    null, personalities, openAiClient, 
    openRouterService, chatSettings, compressorService, choicesService);

//botClient.OnUpdateActions.Add(HandleUpdateAsync);
botClient.OnUpdateActions.Add(HandleAnalysis);
botClient.CallBackQueryActions.Add(CallbackQuery);

await botClient.StartReceiving();


async void CallbackQuery(ITelegramBotClient bClient, Update update, CancellationToken cancellationToken)
{
    var messageText = update.CallbackQuery?.Data ?? "";
    var chatId = update.CallbackQuery?.Message?.Chat.Id ?? 0;
    string pattern = @"/(\w+)\b.*";

    Match match = Regex.Match(messageText, pattern);

    string command = string.Empty;
    if (match.Success)
    {
        command = match.Groups[1].Value;
        command = "/" + command;
        Console.WriteLine("Extracted word: " + command);
    }
    
    var message1 = new TFB.Models.Message()
    {
        Author = update.CallbackQuery?.From?.FirstName ?? "",
        DatePosted = update.Message?.Date ?? DateTime.Now,
        Value = messageText,
        MessageType = MessageType.User
    };

    try
    {

        await botClient.Send(chatId, $"{update.CallbackQuery?.From?.FirstName ?? ""} is sending message {messageText}", null);
        //await bClient.SendTextMessageAsync(chatId, $"Chose {update.CallbackQuery.Message.Text}",
          //  update.CallbackQuery.Message.Text, null);
        await bClient.EditMessageTextAsync(chatId, update.CallbackQuery?.Message.MessageId ?? 0,
            update.CallbackQuery.Message.Text, null);
    }
    catch (ApiRequestException e)
    {
        Console.WriteLine(e.Message);
        
    }

    await botHandlerService.HandleUpdate(chatId, command, messageText, message1, cancellationToken, update.Message ?? new Telegram.Bot.Types.Message());
}

async void HandleAnalysis(ITelegramBotClient bClient, Update update, CancellationToken cancellationToken)
{
    // Only process Message updates: https://core.telegram.org/bots/api#message
    if (update.Message is not { } message)
        return;
        
    // Only process text messages
    if (message.Text is not { } messageText)
        return;

    var command = "";
    if (update.Message?.Entities?.Length > 0 && update.Message.Entities.FirstOrDefault(type => type.Type == MessageEntityType.BotCommand) != null)
    {
        command = update.Message?.EntityValues?.FirstOrDefault() ?? "";
    }

    if (string.IsNullOrEmpty(command)) return;

    var chatId = message.Chat.Id;

    var message1 = new TFB.Models.Message()
    {
        Author = message.From?.FirstName,
        DatePosted = message.Date,
        Value = messageText,
        MessageType = MessageType.User
    };

    botHandlerService.SetTelegramBotClient(bClient);
    await botHandlerService.HandleUpdate(chatId, command, messageText, message1, cancellationToken, message);

}

async void HandleUpdateAsync(ITelegramBotClient bClient, Update update, CancellationToken cancellationToken)
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
            var empty = string.Empty;
            
            cringeLevel = Double.Parse(response.Response.Choices.FirstOrDefault()?.Message.Content ?? empty);
            
            Console.WriteLine($"{cringeLevel} < cringe!");
        }
        catch (Exception e)
        {
            Console.WriteLine("Error parsing cringe " + e.Message);
        }
    }

    if (cringeLevel >= generalSettings?.MinCringeLevel)
    {
        Console.WriteLine("Uh oh, cringe!");
        await bClient.SendTextMessageAsync(
            chatId: chatId,
            text: messageText + " " + cringeMessage,
            cancellationToken: cancellationToken, replyToMessageId: message.ReplyToMessage?.MessageId);
    }
    

    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");
    
}


Console.ReadLine();
Console.WriteLine("ending");