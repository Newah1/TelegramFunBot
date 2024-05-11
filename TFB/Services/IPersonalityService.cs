using TFB.Models;

namespace TFB.Services;

public interface IPersonalityService
{
    IEnumerable<Personality> GetPersonalities();
    Task<IEnumerable<Models.Personality>> GetPersonalities(PersonalityRequest request);
    Task<int?> UpdatePersonality(int personalityId, string name, string personalityDescription);
}