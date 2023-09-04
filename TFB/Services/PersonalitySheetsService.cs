using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using TFB.Models;

namespace TFB;

public class PersonalitySheetsService
{
    private readonly SheetsSettings _sheetsSettings;
    
    private readonly string[] _scopes = { Google.Apis.Sheets.v4.SheetsService.Scope.Spreadsheets };
    private readonly string _applicationName = "Personalities";
    private readonly string _sheet = "";
    private readonly string _spreadsheetId = "";
    private SheetsService _service;

    public PersonalitySheetsService(SheetsSettings sheetsSettings)
    {
        _sheetsSettings = sheetsSettings;
        _spreadsheetId = sheetsSettings.SpreadsheetId;
        _sheet = sheetsSettings.Sheet;
        Init();
    }

    private void Init()
    {
        GoogleCredential credential;
        //Reading Credentials File...
        using (var stream = new FileStream("app_client_secret.json", FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential.FromStream(stream)
                .CreateScoped(_scopes);
        }
        // Creating Google Sheets API service...
        _service = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = _applicationName,
        });
    }

    public List<Personality> LoadPersonalities()
    {
        var range = "A2:E";
        
        SpreadsheetsResource.ValuesResource.GetRequest request =
            _service.Spreadsheets.Values.Get(_spreadsheetId, range);
        
        var response = request.Execute();
        
        IList<IList<object>> values = response.Values;
        var personalities = new List<Personality>();
        if (values != null && values.Count > 0)
        {
            foreach (var row in values)
            {
                // Writing Data on Console...
                try
                {
                    //Console.WriteLine("{0} | {1} | {2} ", row[0], row[1], row[2]);

                    var newPerson = new Personality()
                    {
                        Command = row[0].ToString(),
                        PersonalityDescription = row[1].ToString(),
                        Name = row[2].ToString(),
                        Model = row.ElementAtOrDefault(4)?.ToString() ?? string.Empty
                    };
                    
                    // try to get the temp
                    if (row.ElementAtOrDefault(3) != null)
                    {
                        double temp;
                        Double.TryParse((string)row[3], out temp);
                        newPerson.Temperature = temp;
                    }

                    personalities.Add(
                        newPerson
                    );
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error adding personality from sheet: {e.Message}");
                }
            }
        }
        else
        {
            Console.WriteLine("No data found.");
        }

        return personalities;
    }
}