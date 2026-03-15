using Application.DTOs.Admin;
using SharedKernel;

namespace Application.Services;

public interface IAdminService
{
    Task<PaginatedResultWithStatus<AdminUserSummaryDto>> GetUsersAsync(int page, int pageSize, string? search);
    Task<Result<AdminUserDetailDto>> GetUserByIdAsync(string userId);
    Task<Result> AssignRoleAsync(string userId, string role);
    Task<Result> RemoveRoleAsync(string userId, string role);
    Task<Result> RevokeUserSessionAsync(string userId, Guid sessionId);
    Task<Result> RevokeAllUserSessionsAsync(string userId);
}
