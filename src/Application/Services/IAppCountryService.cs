using Domain.Users;
using SharedKernel;

namespace Application.Services;

public interface IAppCountryService
{
    Task<Result<List<AppCountry>>> GetAllCountriesAsync();
}
