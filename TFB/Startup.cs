using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Standard.AI.OpenAI.Clients.OpenAIs;
using Standard.AI.OpenAI.Models.Configurations;
using Telegram.Bot;
using TFB.DTOs.Settings;
using TFB.Models;
using TFB.Services;
using TFB.Services.Analysis;
using TFB.Services.BotHandler;
using TFB.Services.OpenRouter;

namespace TFB;

public class Startup : IHostedService
{
    private readonly ILogger<Startup> _logger;
    private readonly IImportPersonalityService _importPersonalityService;
    private readonly IConfiguration _configuration;
    private readonly IPersonService _personService;
    private readonly TelegramService _telegramService;
    private readonly BotHandlerService _botHandlerService;

    public Startup(ILogger<Startup> logger, IImportPersonalityService importPersonalityService, 
        IConfiguration configuration, IPersonService personService,
        TelegramService telegramService, BotHandlerService botHandlerService)
    {
        _logger = logger;
        _configuration = configuration;
        _personService = personService;
        _telegramService = telegramService;
        _botHandlerService = botHandlerService;
        _importPersonalityService = importPersonalityService;
    }
    
    public static async Task ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped(sp =>
        {
            var telegramSettings = sp.GetRequiredService<TelegramSettings>();
            return TelegramService.SetupClient(telegramSettings ?? new TelegramSettings());
        });

        serviceCollection.AddSingleton(sp =>
        {
            var chatSettings = sp.GetRequiredService<ChatSettings>();

            return new OpenAIClient(new OpenAIConfigurations()
            {
                ApiKey = chatSettings.SecretKey
            });
        });

        serviceCollection.AddSingleton<TelegramService>(sp =>
        {
            var telegramSettings = sp.GetRequiredService<TelegramSettings>();

            return TelegramService.SetupClient(telegramSettings);
        });
        serviceCollection.AddSingleton<CompressorService>();
        
        serviceCollection.AddSingleton<AnalysisService>();

        serviceCollection.AddSingleton<BotHandlerService>();
        
        serviceCollection.AddSingleton<IMessageHistoryService, MessageHistoryService>();
        serviceCollection.AddSingleton<ChoicesService>();

        serviceCollection.AddSingleton<IOpenRouterService, OpenRouterService>();
        serviceCollection.AddSingleton<IDatabaseService, DatabaseService>();
        serviceCollection.AddSingleton<IImportPersonalityService, ImportPersonalityService>();
        serviceCollection.AddScoped<IPersonService, PersonService>();
        serviceCollection.AddScoped<IPersonalityService, PersonalityService>();
        
        serviceCollection.AddSingleton<PersonalitySheetsService>();
        await Task.FromResult(true);
    }

    public static void GetConfigurations(IServiceCollection serviceCollection, IConfiguration configuration)
    {
        var chatSettings = new ChatSettings();
        configuration.GetSection("ChatSettings").Bind(chatSettings);
        var telegramSettings = new TelegramSettings();
        configuration.GetSection("TelegramSettings").Bind(telegramSettings);
        var generalSettings = new GeneralSettings();
        configuration.GetSection("GeneralSettings").Bind(generalSettings);
        var sheetsSettings = new SheetsSettings();
        configuration.GetSection("SheetsSettings").Bind(sheetsSettings);
        var openRouterSettings = new OpenRouterSettings();
        configuration.GetSection("OpenRouterSettings").Bind(openRouterSettings);
        
        serviceCollection.AddSingleton(_ => chatSettings);
        serviceCollection.AddSingleton(_ => telegramSettings);
        serviceCollection.AddSingleton(_ => generalSettings);
        serviceCollection.AddSingleton(_ => sheetsSettings);
        serviceCollection.AddSingleton(_ => openRouterSettings);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting up");
        
        
        
        _telegramService.OnUpdateActions.Add((async (botHandlerRequest, cancellationToken) =>
        {
            await _botHandlerService.HandleUpdate(botHandlerRequest);
        }));

        await _telegramService.StartReceiving();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Stopping");
    }
}