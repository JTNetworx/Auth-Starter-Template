using Application.Services;
using Domain.Users;
using Infrastructure.Persistance.Repositories;
using SharedKernel;

namespace Infrastructure.Services;

public class AppCountryService : IAppCountryService
{
    private readonly IAppCountryRepository _appCountryRepository;

    public AppCountryService(IAppCountryRepository appCountryRepository)
    {
        _appCountryRepository = appCountryRepository;
    }

    public async Task<Result<List<AppCountry>>> GetAllCountriesAsync()
    {
        return await _appCountryRepository.GetAllCountriesAsync();
    }
}
