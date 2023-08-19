using NUnit.Framework;

namespace Cringeometer.Tests;


public class TokenTest
{
    
    [Test]
    public void TestTokenEstimator()
    {
        string text = "This is a sample text for token estimation.";
        int tokens = TokenEstimatorService.EstimateTokens(text, "average");
        
        Assert.AreEqual(tokens, 11);
    }
}