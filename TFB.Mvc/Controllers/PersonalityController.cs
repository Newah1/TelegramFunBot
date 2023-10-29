using Microsoft.AspNetCore.Mvc;
using X.PagedList;
using TFB.Models;
using TFB.Services;

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
            IncludeMessageHistory = true,
            Limit = 1
        };

        var personalities = await _personalityService.GetPersonalities(personalityRequest);

        var personalityDto = personalities.ToDTOList().First();
        
        return View(personalityDto);
    }
    [Route("personalities/{page?}")]
    public async Task<IActionResult> Personalities(int page = 1)
    {
        var request = new PersonalityRequest()
        {
            IncludeMessageHistory = true
        };

        var personalities = await _personalityService.GetPersonalities(request);

        //var pageSize = _pageLimit / personalities?.FirstOrDefault()?.TotalCount ?? 0;
        return View(personalities.ToDTOList().ToPagedList(page, _pageLimit));
    }
}