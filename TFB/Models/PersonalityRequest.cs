namespace TFB.Models;

public class PersonalityRequest : BaseRequest
{
    public bool IncludeMessageHistory { get; set; } = false;
    public int? PersonalityId { get; set; }
}