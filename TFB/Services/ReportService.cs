using TFB.Services;
using TFB.Services.Analysis;

namespace TFB;

public class ReportService
{
    private List<AnalysisService> _analyzers;
    private string _reportTemplate = @"
Personality Name: {0}
Truncated Description: {1}
Temp: {2}
Messages in Context: {3}
Command: {4}
";

    public ReportService(List<AnalysisService> analyzers)
    {
        _analyzers = analyzers;
    }

    public void UpdateAnalyzers(List<AnalysisService> analyzers)
    {
        _analyzers = analyzers;
    }

    public string GenerateReport()
    {
        string reportString = "";
        foreach (var analysisService in _analyzers)
        {
            var truncated = "";
            if (analysisService.Template.Length > 40)
            {
                truncated = analysisService.Template.Substring(0, 40);
            }
            else
            {
                truncated = analysisService.Template;
            }

            reportString += string.Format(_reportTemplate, 
                analysisService.Name, 
                $"{truncated}...",
                "no temp",
                analysisService.Messages.Count.ToString(),
                analysisService.Command
            );
        }

        return reportString;
    }
}