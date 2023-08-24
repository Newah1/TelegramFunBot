// See https://aka.ms/new-console-template for more information

using TFB;
using TFB.Models;
using Microsoft.Extensions.Configuration;
using Standard.AI.OpenAI.Clients.OpenAIs;
using Standard.AI.OpenAI.Models.Configurations;
using Standard.AI.OpenAI.Models.Services.Foundations.ChatCompletions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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


var personalitySheetService = new PersonalitySheetsService(sheetsSettings);
var personalitiesFromSheet = personalitySheetService.LoadPersonalities();

ChatService.ChatSettings = chatSettings;


var openAiConfigurations = new OpenAIConfigurations
{   
    ApiKey = chatSettings.SecretKey
};
var openAiClient = new OpenAIClient(openAiConfigurations);

var personalities = new List<Personality>();
configurationBuilder.GetSection("Personalities").Bind(personalities);

personalities.AddRange(personalitiesFromSheet);


var analyzers = new List<AnalysisService>();

foreach (var personality in personalities)
{
    analyzers.Add(AnalysisService.GetAnalyzer(personality, openAiClient, chatSettings));
}

var lastTimeRanSpreadsheet = DateTime.Now;

// Telegram 

var botClient = TelegramService.SetupClient(telegramSettings);

//botClient.OnUpdateActions.Add(HandleUpdateAsync);
botClient.OnUpdateActions.Add(HandleAnalysis);

await botClient.StartReceiving();

async void HandleAnalysis(ITelegramBotClient bClient, Update update, CancellationToken cancellationToken)
{
    // Only process Message updates: https://core.telegram.org/bots/api#message
    if (update.Message is not { } message)
        return;
        
    // Only process text messages
    if (message.Text is not { } messageText)
        return;
    
    var chatId = message.Chat.Id;

    var message1 = new TFB.Models.Message()
    {
        Author = message.From?.Username + " " + message.From?.FirstName,
        DatePosted = message.Date,
        Value = messageText
    };

    foreach (var analysisService in analyzers)
    {
        analysisService.AddMessage(message1);
    }

    if (messageText.ToLower() == "/wipe_context")
    {
        foreach (var analysisService in analyzers)
        {
            analysisService.WipeMessages();
        }
    }

    if ((DateTime.Now - lastTimeRanSpreadsheet).TotalSeconds > 60)
    {
        var refreshValues = personalitySheetService.LoadPersonalities();
        var personalitiesRefreshed = new List<Personality>();
        foreach (var personality in personalities)
        {
            Personality? personalityToAdd;
            var newCommands = refreshValues.Select(e => e.Command).ToList();
            personalityToAdd = newCommands.Contains(personality.Command) ? refreshValues.FirstOrDefault(rf => rf.Command == personality.Command) : personality;
            if(personalityToAdd != null) {
                personalitiesRefreshed.Add(personalityToAdd);
            }
        }

        var newPersonalities = refreshValues
            .Where(r => !personalitiesRefreshed.Select(e => e.Command).Contains(r.Command)).ToList();
        
        personalitiesRefreshed.AddRange(
                newPersonalities
            );

        personalities = personalitiesRefreshed;

        foreach (var analysisService in analyzers)
        {
            var matchingPersonality = personalities.FirstOrDefault(p => p.Command == analysisService.Command);
            if (matchingPersonality != null)
            {
                analysisService.Template = matchingPersonality.PersonalityDescription;
                analysisService.Name = matchingPersonality.Name;
                analysisService.SetPersonality(matchingPersonality);
            }
        }

        foreach (var personality in newPersonalities)
        {
            analyzers.Add(
                AnalysisService.GetAnalyzer(personality, openAiClient, chatSettings)
                );
        }
        
        lastTimeRanSpreadsheet = DateTime.Now;
    }
    
    Console.WriteLine($"Got a message from {message.From?.FirstName} with contents: {messageText}");
    string analysis;
    foreach (var analysisService in analyzers)
    {
        if (messageText.ToLower().Contains(analysisService.Command))
        {
            Console.WriteLine($"Handling analysis with ");
            
            // tell the bot to "type"
            var timer = new Timer(state =>
            {
                bClient.SendChatActionAsync(chatId, ChatAction.Typing, message.ReplyToMessage?.MessageId);
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(3));
            
            analysis = await analysisService.Analysis();
            if (string.IsNullOrEmpty(analysis)) continue;

            // stop "typing"
            await timer.DisposeAsync();
            
            // send the text message
            await bClient.SendTextMessageAsync(
                chatId: chatId,
                text: analysis,
                cancellationToken: cancellationToken, replyToMessageId: message.ReplyToMessage?.MessageId);

            
            
            // give other analyzers your most recent message
            foreach (var otherAnalyzers in analyzers.Where(a => a != analysisService).ToList())
            {
                Console.WriteLine($"Giving {otherAnalyzers.Name} the most recent message [{analysis.Substring(0, 10)}...]");
                otherAnalyzers.AddMessage(
                        new TFB.Models.Message()
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