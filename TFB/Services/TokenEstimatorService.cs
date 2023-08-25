namespace TFB;

public class TokenEstimatorService
{
    public static int EstimateTokens(string text, string method = "max")
    {
        // Method can be "average", "words", "chars", "max", "min", defaults to "max"
        // "average" is the average of words and chars
        // "words" is the word count divided by 0.75
        // "chars" is the char count divided by 4
        // "max" is the max of word and char
        // "min" is the min of word and char
        string[] words = text.Split(' ');
        int wordCount = words.Length;
        int charCount = text.Length;
        double tokensCountWordEst = wordCount / 0.75;
        double tokensCountCharEst = charCount / 4.0;
        double output = 0;

        if (method == "average")
        {
            output = (tokensCountWordEst + tokensCountCharEst) / 2;
        }
        else if (method == "words")
        {
            output = tokensCountWordEst;
        }
        else if (method == "chars")
        {
            output = tokensCountCharEst;
        }
        else if (method == "max")
        {
            output = Math.Max(tokensCountWordEst, tokensCountCharEst);
        }
        else if (method == "min")
        {
            output = Math.Min(tokensCountWordEst, tokensCountCharEst);
        }
        else
        {
            // Return invalid method message
            return -1; // or throw an exception
        }

        return (int)Math.Round(output);
    }
}