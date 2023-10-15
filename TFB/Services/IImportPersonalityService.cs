using TFB.Models;
using Personality = TFB.DTOs.Personality;

namespace TFB.Services;

public interface IImportPersonalityService
{
    public Task<IEnumerable<Personality>> ImportPersonalities();
}