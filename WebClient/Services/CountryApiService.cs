using System.Net.Http.Json;

namespace WebClient.Services;

public sealed class CountryApiService : ICountryApiService
{
    private readonly HttpClient _http;
    private List<CountryDto>? _cache;

    public CountryApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("public");
    }

    public async Task<ApiResult<List<CountryDto>>> GetCountriesAsync()
    {
        if (_cache is not null)
            return ApiResult<List<CountryDto>>.Success(_cache);

        try
        {
            var list = await _http.GetFromJsonAsync<List<CountryDto>>("countries");
            _cache = list ?? [];
            return ApiResult<List<CountryDto>>.Success(_cache);
        }
        catch (Exception ex)
        {
            return ApiResult<List<CountryDto>>.Failure(ex.Message);
        }
    }
}
