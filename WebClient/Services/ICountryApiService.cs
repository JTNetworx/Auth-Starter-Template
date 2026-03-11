namespace WebClient.Services;

public interface ICountryApiService
{
    Task<ApiResult<List<CountryDto>>> GetCountriesAsync();
}
