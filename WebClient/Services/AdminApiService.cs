using System.Net.Http.Json;

namespace WebClient.Services;

/// <summary>
/// Calls the backend /api/admin/* endpoints.
/// Uses the "api" HttpClient which has the AuthHttpMessageHandler attached.
/// </summary>
public sealed class AdminApiService : IAdminApiService
{
    private readonly HttpClient _http;

    public AdminApiService(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient("api");
    }

    public async Task<ApiResult<PaginatedResult<AdminUserSummaryDto>>> GetUsersAsync(int page, int pageSize, string? search = null)
    {
        try
        {
            var url = $"admin/users?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search))
                url += $"&search={Uri.EscapeDataString(search)}";

            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return ApiResult<PaginatedResult<AdminUserSummaryDto>>.Failure(await ReadErrorAsync(response));

            var result = await response.Content.ReadFromJsonAsync<PaginatedResult<AdminUserSummaryDto>>();
            return ApiResult<PaginatedResult<AdminUserSummaryDto>>.Success(result ?? new());
        }
        catch (Exception ex)
        {
            return ApiResult<PaginatedResult<AdminUserSummaryDto>>.Failure(ex.Message);
        }
    }

    public async Task<ApiResult<AdminUserDetailDto>> GetUserByIdAsync(string userId)
    {
        try
        {
            var response = await _http.GetAsync($"admin/users/{userId}");
            if (!response.IsSuccessStatusCode)
                return ApiResult<AdminUserDetailDto>.Failure(await ReadErrorAsync(response));

            var user = await response.Content.ReadFromJsonAsync<AdminUserDetailDto>();
            return user is null
                ? ApiResult<AdminUserDetailDto>.Failure("Invalid response.")
                : ApiResult<AdminUserDetailDto>.Success(user);
        }
        catch (Exception ex)
        {
            return ApiResult<AdminUserDetailDto>.Failure(ex.Message);
        }
    }

    public async Task<ApiResult> AssignRoleAsync(string userId, string role)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"admin/users/{userId}/roles", new { role });
            return response.IsSuccessStatusCode
                ? ApiResult.Success()
                : ApiResult.Failure(await ReadErrorAsync(response));
        }
        catch (Exception ex)
        {
            return ApiResult.Failure(ex.Message);
        }
    }

    public async Task<ApiResult> RemoveRoleAsync(string userId, string role)
    {
        try
        {
            var response = await _http.DeleteAsync($"admin/users/{userId}/roles/{Uri.EscapeDataString(role)}");
            return response.IsSuccessStatusCode
                ? ApiResult.Success()
                : ApiResult.Failure(await ReadErrorAsync(response));
        }
        catch (Exception ex)
        {
            return ApiResult.Failure(ex.Message);
        }
    }

    public async Task<ApiResult> RevokeUserSessionAsync(string userId, Guid sessionId)
    {
        try
        {
            var response = await _http.DeleteAsync($"admin/users/{userId}/sessions/{sessionId}");
            return response.IsSuccessStatusCode
                ? ApiResult.Success()
                : ApiResult.Failure(await ReadErrorAsync(response));
        }
        catch (Exception ex)
        {
            return ApiResult.Failure(ex.Message);
        }
    }

    public async Task<ApiResult> RevokeAllUserSessionsAsync(string userId)
    {
        try
        {
            var response = await _http.DeleteAsync($"admin/users/{userId}/sessions");
            return response.IsSuccessStatusCode
                ? ApiResult.Success()
                : ApiResult.Failure(await ReadErrorAsync(response));
        }
        catch (Exception ex)
        {
            return ApiResult.Failure(ex.Message);
        }
    }

    public async Task<ApiResult<PaginatedResult<AuditLogDto>>> GetLogsAsync(
        int page, int pageSize,
        string? userId = null, string? action = null,
        DateTime? from = null, DateTime? to = null)
    {
        try
        {
            var url = $"admin/logs?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(userId)) url += $"&userId={Uri.EscapeDataString(userId)}";
            if (!string.IsNullOrWhiteSpace(action)) url += $"&action={Uri.EscapeDataString(action)}";
            if (from.HasValue) url += $"&from={Uri.EscapeDataString(from.Value.ToString("o"))}";
            if (to.HasValue) url += $"&to={Uri.EscapeDataString(to.Value.ToString("o"))}";

            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return ApiResult<PaginatedResult<AuditLogDto>>.Failure(await ReadErrorAsync(response));

            var result = await response.Content.ReadFromJsonAsync<PaginatedResult<AuditLogDto>>();
            return ApiResult<PaginatedResult<AuditLogDto>>.Success(result ?? new());
        }
        catch (Exception ex)
        {
            return ApiResult<PaginatedResult<AuditLogDto>>.Failure(ex.Message);
        }
    }

    public async Task<ApiResult<List<JobStatusDto>>> GetJobsAsync()
    {
        try
        {
            var response = await _http.GetAsync("admin/jobs");
            if (!response.IsSuccessStatusCode)
                return ApiResult<List<JobStatusDto>>.Failure(await ReadErrorAsync(response));

            var jobs = await response.Content.ReadFromJsonAsync<List<JobStatusDto>>();
            return ApiResult<List<JobStatusDto>>.Success(jobs ?? []);
        }
        catch (Exception ex)
        {
            return ApiResult<List<JobStatusDto>>.Failure(ex.Message);
        }
    }

    public async Task<ApiResult> TriggerJobAsync(string jobName)
    {
        try
        {
            var response = await _http.PostAsync($"admin/jobs/{Uri.EscapeDataString(jobName)}/trigger", null);
            return response.IsSuccessStatusCode
                ? ApiResult.Success()
                : ApiResult.Failure(await ReadErrorAsync(response));
        }
        catch (Exception ex)
        {
            return ApiResult.Failure(ex.Message);
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return body?.Error ?? body?.Title ?? response.ReasonPhrase ?? "An error occurred.";
        }
        catch
        {
            return response.ReasonPhrase ?? "An error occurred.";
        }
    }

    private record ErrorResponse(string? Error, string? Title);
}
