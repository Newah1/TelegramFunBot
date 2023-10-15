using System.ComponentModel.DataAnnotations;

namespace TFB.Models;

public class MessageHistory
{
    [Key]
    public int MessageHistoryId { get; set; }
    public string Author { get; set; }
    public string Role { get; set; }
    public DateTime DateCreated { get; set; }
    public string Message { get; set; }
    public int PersonalityId { get; set; }
    public string Summary { get; set; }
    public Personality Personality { get; set; }
}