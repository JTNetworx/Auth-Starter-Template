using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Infrastructure.Persistance.Repositories;

public interface IAppCountryRepository
{
    Task<Result<List<AppCountry>>> GetAllCountriesAsync();
}

public sealed class AppCountryRepository : IAppCountryRepository
{
    private readonly ApplicationDbContext _dbContext;

    public AppCountryRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<List<AppCountry>>> GetAllCountriesAsync()
    {
        var countries = await _dbContext.AppCountries
            .AsNoTracking()
            .ToListAsync();
        if (countries.Count > 0)
            return Result<List<AppCountry>>.Success(countries);

        return Result<List<AppCountry>>.Success([]);
    }
}
