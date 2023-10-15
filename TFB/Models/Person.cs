using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TFB.Models;

[Table("People")]
public class Person
{
    public Person()
    {
        LastSeen = DateTime.Now;
    }
    
    [Key]
    public string Username { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
}