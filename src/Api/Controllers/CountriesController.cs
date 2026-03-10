using Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CountriesController : ControllerBase
{
    private readonly IAppCountryService _appCountryService;

    public CountriesController(IAppCountryService appCountryService)
    {
        _appCountryService = appCountryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCountriesAsync()
    {
        var countries = await _appCountryService.GetAllCountriesAsync();
        if (countries.IsSuccess)
        {
            return Ok(countries.Value);
        }
        else
        {
            return BadRequest(countries.Error);
        }
    }
}
