using TFB.DTOs;

namespace TFB.Models;

public static class ModelMapper
{
    public static List<DTOs.Personality> ToDTOList(this IEnumerable<Models.Personality> personalities)
    {
        var dtoPersonalities = personalities.Select(personality =>
        {
            var messageHistory = new List<DTOs.Message>();

            if (personality.MessageHistories.Count > 0  && personality.MessageHistories.Any(mh => mh != null))
            {
                try
                {
                    messageHistory = personality.MessageHistories.Select(mh => new DTOs.Message
                    {
                        Author = mh.Author,
                        DatePosted = mh.DateCreated,
                        ConversationWith = mh.ConversationWith,
                        Summary = mh.Summary,
                        MessageType = (mh.Role == "user") ? MessageType.User : MessageType.Bot,
                        Value = mh.Message
                    })
                    .OrderByDescending(mh => mh.DatePosted)
                    .ToList();
                }
                catch (Exception e)
                {
                    
                }
            }
            
            return new DTOs.Personality()
            {
                PersonalityId = personality.PersonalityId,
                Command = personality.Command,
                HasOptions = personality.HasOptions,
                PersonalityDescription = personality.PersonalityDescription,
                MessageHistory = messageHistory,
                Name = personality.Name,
                Temperature = personality.Temperature,
                Model = personality.Model,
                TotalCount = personality.TotalCount
            };
        });
        
        return dtoPersonalities.ToList();
    }
    
    public static DTOs.Personality MapToPersonalityDTO(this Models.Personality personality)
    {
        return new DTOs.Personality();
    }
}