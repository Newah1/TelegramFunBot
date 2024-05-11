using Microsoft.AspNetCore.Mvc;
using X.PagedList;
using TFB.Models;
using TFB.Services;
using TFB.Mvc.Models;

namespace TFB.Mvc.Controllers;

public class PersonalityController : Controller
{
    private readonly ILogger<PersonalityController> _logger;
    private readonly IPersonalityService _personalityService;

    private const int _pageLimit = 10;
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
            PersonalityId = personalityId, 
            IncludeMessageHistory = true
        };

        var personalities = await _personalityService.GetPersonalities(personalityRequest);

        var personalityDto = personalities.ToDTOList().First();
        
        return View(personalityDto);
    }

    [Route("personality/{personalityId}/edit")]
    public async Task<IActionResult> Edit(int personalityId)
    {
         var personalityRequest = new PersonalityRequest()
        {
            PersonalityId = personalityId,
            IncludeMessageHistory = true
        };

        var personalities = await _personalityService.GetPersonalities(personalityRequest);

        var personalityDto = personalities.ToDTOList().First();

        return View(personalityDto);
    }

    [Route("personality/{personalityId}/edit")]
    [HttpPost]
    public async Task<IActionResult> Edit(int personalityId, PersonalityUpdateRequestModel request)
    {
        var valid = base.TryValidateModel(request);

        var personalityRequest = new PersonalityRequest()
        {
            PersonalityId = personalityId,
            IncludeMessageHistory = true
        };

        var personalityDto = await _personalityService.GetPersonalities(personalityRequest);

        var returnedId = await _personalityService.UpdatePersonality(personalityId, request.Name, request.PersonalityDescription);

        if(returnedId == null)
        {
            _logger.LogError($"Could not update. {personalityId}");
        }

        return View(personalityDto.ToDTOList().FirstOrDefault());
    }


    [Route("personalities/{page?}")]
    public async Task<IActionResult> Personalities(int page = 1)
    {
        var request = new PersonalityRequest()
        {
            IncludeMessageHistory = true
        };

        var personalities = await _personalityService.GetPersonalities(request);

        return View(personalities.ToDTOList().ToPagedList(page, _pageLimit));
    }
}