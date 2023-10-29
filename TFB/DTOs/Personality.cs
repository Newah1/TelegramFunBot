namespace TFB.DTOs;

public class Personality
{
    public int PersonalityId { get; set; }
    public string Name { get; set; }
    public string Command { get; set; }
    public string PersonalityDescription { get; set; }
    public double? Temperature { get; set; } = null;
    public string Model { get; set; }
    public bool HasOptions { get; set; }
    public List<Message> MessageHistory { get; set; }
    
    public int TotalCount { get; set; }

    public bool MatchesCommand(string command) => command.ToLower().Trim() == Command.Trim();
}