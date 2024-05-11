namespace TFB.DTOs;

public class Message
{
    public string Author { get; set; }
    public DateTime DatePosted { get; set; }
    public string Value { get; set; }
    public string ConversationWith { get; set; }
    public string Summary { get; set; }
    public MessageType? MessageType { get; set; }
}

public enum MessageType
{
    Bot = 1,
    User = 2
}