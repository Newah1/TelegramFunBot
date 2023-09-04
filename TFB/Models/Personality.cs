namespace TFB.Models;

public class Personality
{
    public string Name { get; set; }
    public string Command { get; set; }
    public string PersonalityDescription { get; set; }
    public double? Temperature { get; set; } = null;
    public string Model { get; set; }
}