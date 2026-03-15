namespace WebClient.Services;

public interface IAdminApiService
{
    Task<ApiResult<PaginatedResult<AdminUserSummaryDto>>> GetUsersAsync(int page, int pageSize, string? search = null);
    Task<ApiResult<AdminUserDetailDto>> GetUserByIdAsync(string userId);
    Task<ApiResult> AssignRoleAsync(string userId, string role);
    Task<ApiResult> RemoveRoleAsync(string userId, string role);
    Task<ApiResult> RevokeUserSessionAsync(string userId, Guid sessionId);
    Task<ApiResult> RevokeAllUserSessionsAsync(string userId);
    Task<ApiResult<PaginatedResult<AuditLogDto>>> GetLogsAsync(int page, int pageSize, string? userId = null, string? action = null, DateTime? from = null, DateTime? to = null);

    // Background jobs
    Task<ApiResult<List<JobStatusDto>>> GetJobsAsync();
    Task<ApiResult> TriggerJobAsync(string jobName);
}
