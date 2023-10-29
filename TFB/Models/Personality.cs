using System.ComponentModel.DataAnnotations;

namespace TFB.Models;

public class Personality
{
    public Personality()
    {
        DateCreated = DateTime.Now;
        DateModified = DateTime.Now;
        MessageHistories = new List<MessageHistory>();
    }
    
    [Key]
    public int PersonalityId { get; set; }
    public string Name { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }
    public string Command { get; set; }
    public List<MessageHistory> MessageHistories { get; set; } = new List<MessageHistory>();
    public bool HasOptions { get; set; }
    public string PersonalityDescription { get; set; }
    public double Temperature { get; set; }
    public string Model { get; set; }
    
    public int TotalCount { get; set; }
}