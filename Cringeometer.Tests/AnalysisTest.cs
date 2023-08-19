using Cringeometer.Models;
using Standard.AI.OpenAI.Clients.OpenAIs;
using Standard.AI.OpenAI.Models.Configurations;

namespace Cringeometer.Tests;

public class Tests
{
    private AnalysisService _analysisService;
    
    [SetUp]
    public void Setup()
    {
        _analysisService = new AnalysisService(new OpenAIClient(new OpenAIConfigurations()));
    }

    [Test]
    public void TestAnalysisServiceRolls100Messages()
    {
        _analysisService.AddMessage(new Message()
        {
            Author = "First",
            DatePosted = DateTime.Now,
            Value = "First Message"
        });

        Assert.AreEqual(_analysisService.Messages[0].Author, "First");

        for (int i = 0; i < 100; i++)
        {
            _analysisService.AddMessage(new Message()
            {
                Author = i + " Message",
                DatePosted = DateTime.Now,
                Value = i + " Message"
            });
        }
        
        Assert.AreEqual(_analysisService.Messages[0].Author, "0 Message");
        
        for (int i = 0; i < 500; i++)
        {
            _analysisService.AddMessage(new Message()
            {
                Author = i + " Message",
                DatePosted = DateTime.Now,
                Value = i + " Message"
            });
        }
        
        Assert.AreEqual(_analysisService.Messages[0].Author, "400 Message");
    }
}