using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Repositories;

public interface ITokenRepository
{
    Task SaveRefreshTokenAsync(RefreshToken token);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task UpdateRefreshTokenAsync(RefreshToken token);
    Task DeleteAllRefreshTokensForUserAsync(string userId);
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

    public Task UpdateRefreshTokenAsync(RefreshToken token)
    {
        _context.RefreshTokens.Update(token);
        return Task.CompletedTask;
    }

    public async Task DeleteAllRefreshTokensForUserAsync(string userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId)
            .ToListAsync();

        _context.RefreshTokens.RemoveRange(tokens);
    }
}
