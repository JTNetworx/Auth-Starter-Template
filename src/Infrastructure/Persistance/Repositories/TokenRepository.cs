using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Repositories;

public interface ITokenRepository
{
    Task SaveRefreshTokenAsync(RefreshToken token);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task<RefreshToken?> GetActiveTokenByIdAsync(Guid id, string userId);
    Task<List<RefreshToken>> GetActiveSessionsForUserAsync(string userId);
    Task UpdateRefreshTokenAsync(RefreshToken token);
    Task DeleteAllRefreshTokensForUserAsync(string userId, Guid? excludeId = null);
}

public sealed class TokenRepository : ITokenRepository
{
    private readonly ApplicationDbContext _context;

    public TokenRepository(ApplicationDbContext context) => _context = context;

    // No SaveChangesAsync here — callers control the save boundary via IUnitOfWork.

    public Task SaveRefreshTokenAsync(RefreshToken token)
    {
        _context.RefreshTokens.Add(token);
        return Task.CompletedTask;
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token) =>
        await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token);

    public async Task<RefreshToken?> GetActiveTokenByIdAsync(Guid id, string userId) =>
        await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId
                && t.RevokedAtUtc == null && t.ExpiresUtc > DateTime.UtcNow);

    public async Task<List<RefreshToken>> GetActiveSessionsForUserAsync(string userId) =>
        await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresUtc > DateTime.UtcNow)
            .OrderByDescending(t => t.LastUsedUtc ?? t.CreatedAtUtc)
            .ToListAsync();

    public Task UpdateRefreshTokenAsync(RefreshToken token)
    {
        _context.RefreshTokens.Update(token);
        return Task.CompletedTask;
    }

    public async Task DeleteAllRefreshTokensForUserAsync(string userId, Guid? excludeId = null)
    {
        var tokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && (excludeId == null || t.Id != excludeId))
            .ToListAsync();

        _context.RefreshTokens.RemoveRange(tokens);
    }
}
