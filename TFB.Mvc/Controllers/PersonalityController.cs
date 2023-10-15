using Microsoft.AspNetCore.Mvc;
using TFB.Models;
using TFB.Services;

namespace TFB.Mvc.Controllers;

public class PersonalityController : Controller
{
    private readonly ILogger<PersonalityController> _logger;
    private readonly IPersonalityService _personalityService;

    public PersonalityController(ILogger<PersonalityController> logger, IPersonalityService personalityService)
    {
        _logger = logger;
        _personalityService = personalityService;
    } 
    [Route("personality/{personalityId}")]
    public async Task<IActionResult> Index(int personalityId)
    {
        var personalityRequest = new PersonalityRequest()
        {
            PersonalityId = personalityId, IncludeMessageHistory = true
        };

        var personalities = await _personalityService.GetPersonalities(personalityRequest);

        var personalityDto = personalities.ToDTOList().First();
        
        return View(personalityDto);
    }
}