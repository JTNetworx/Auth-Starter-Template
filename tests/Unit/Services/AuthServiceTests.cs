using Application.DTOs.Auth;
using Application.Services;
using Domain.Users;
using FluentAssertions;
using Infrastructure.Persistance.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using SharedKernel;
using Xunit;

namespace Unit.Tests.Services;

/// <summary>
/// Unit tests for AuthService. All external collaborators are mocked so tests
/// run entirely in-memory without a database or real UserManager.
/// </summary>
public class AuthServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Mock<UserManager<User>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<User>>();
        // UserManager has a long ctor — pass nulls for optional services
        return new Mock<UserManager<User>>(
            store.Object, null, null, null, null, null, null, null, null);
    }

    private static Mock<RoleManager<IdentityRole>> CreateRoleManagerMock()
    {
        var store = new Mock<IRoleStore<IdentityRole>>();
        return new Mock<RoleManager<IdentityRole>>(
            store.Object, null, null, null, null);
    }

    private static AuthService CreateService(
        Mock<UserManager<User>>? umMock = null,
        Mock<RoleManager<IdentityRole>>? rmMock = null,
        Mock<ITokenService>? tokenMock = null,
        Mock<ITokenRepository>? tokenRepoMock = null,
        Mock<IAppEmailSender>? emailMock = null,
        Mock<IUnitOfWork>? uowMock = null,
        Mock<IDateTimeProvider>? dtMock = null,
        Mock<IHttpContextAccessor>? httpMock = null,
        Mock<IAuditLogService>? auditMock = null)
    {
        umMock ??= CreateUserManagerMock();
        rmMock ??= CreateRoleManagerMock();
        tokenMock ??= new Mock<ITokenService>();
        tokenRepoMock ??= new Mock<ITokenRepository>();
        emailMock ??= new Mock<IAppEmailSender>();
        uowMock ??= new Mock<IUnitOfWork>();
        dtMock ??= new Mock<IDateTimeProvider>();
        httpMock ??= new Mock<IHttpContextAccessor>();
        auditMock ??= new Mock<IAuditLogService>();

        dtMock.Setup(d => d.UtcNow).Returns(DateTime.UtcNow);
        auditMock
            .Setup(a => a.RecordAsync(It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object?>()))
            .Returns(Task.CompletedTask);

        return new AuthService(
            umMock.Object, rmMock.Object, tokenMock.Object,
            tokenRepoMock.Object, emailMock.Object,
            uowMock.Object, dtMock.Object, httpMock.Object, auditMock.Object);
    }

    private static User MakeUser(string id = "user-1", bool emailConfirmed = true) => new()
    {
        Id = id,
        UserName = "testuser",
        Email = "test@example.com",
        FirstName = "Test",
        LastName = "User",
        EmailConfirmed = emailConfirmed
    };

    private static void SetupTransactionMocks(Mock<IUnitOfWork> uow)
    {
        uow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);
        uow.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .ReturnsAsync(1);
    }

    // ── RegisterAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_WithValidData_CreatesUserAndReturnsSuccess()
    {
        var umMock = CreateUserManagerMock();
        var rmMock = CreateRoleManagerMock();
        var emailMock = new Mock<IAppEmailSender>();

        umMock.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
              .ReturnsAsync(IdentityResult.Success);
        rmMock.Setup(m => m.RoleExistsAsync("User")).ReturnsAsync(true);
        umMock.Setup(m => m.AddToRoleAsync(It.IsAny<User>(), "User"))
              .ReturnsAsync(IdentityResult.Success);
        umMock.Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()))
              .ReturnsAsync("confirm-token");
        emailMock.Setup(e => e.SendEmailConfirmationAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(umMock: umMock, rmMock: rmMock, emailMock: emailMock);

        var dto = new RegisterDto("Test", "User", "testuser", "test@example.com", "Password1!", "Password1!");
        var result = await service.RegisterAsync(dto);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_WhenPasswordsDoNotMatch_ReturnsFailure()
    {
        var service = CreateService();
        var dto = new RegisterDto("Test", "User", "testuser", "test@example.com", "Password1!", "Different1!");

        var result = await service.RegisterAsync(dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Passwords do not match");
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ReturnsFailure()
    {
        var umMock = CreateUserManagerMock();
        var error = new IdentityError { Description = "Email 'test@example.com' is already taken." };
        umMock.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
              .ReturnsAsync(IdentityResult.Failed(error));

        var service = CreateService(umMock: umMock);
        var dto = new RegisterDto("Test", "User", "testuser", "test@example.com", "Password1!", "Password1!");

        var result = await service.RegisterAsync(dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already taken");
    }

    [Fact]
    public async Task RegisterAsync_WhenRoleAssignmentFails_ReturnsFailure()
    {
        var umMock = CreateUserManagerMock();
        var rmMock = CreateRoleManagerMock();

        umMock.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
              .ReturnsAsync(IdentityResult.Success);
        rmMock.Setup(m => m.RoleExistsAsync("User")).ReturnsAsync(true);

        var roleError = new IdentityError { Description = "Role assignment failed" };
        umMock.Setup(m => m.AddToRoleAsync(It.IsAny<User>(), "User"))
              .ReturnsAsync(IdentityResult.Failed(roleError));

        var service = CreateService(umMock: umMock, rmMock: rmMock);
        var dto = new RegisterDto("Test", "User", "testuser", "test@example.com", "Password1!", "Password1!");

        var result = await service.RegisterAsync(dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Role assignment failed");
    }

    // ── LoginAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokens()
    {
        var umMock = CreateUserManagerMock();
        var tokenMock = new Mock<ITokenService>();
        var tokenRepoMock = new Mock<ITokenRepository>();
        var uowMock = new Mock<IUnitOfWork>();
        var user = MakeUser(emailConfirmed: true);

        umMock.Setup(m => m.FindByNameAsync("testuser")).ReturnsAsync(user);
        umMock.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        umMock.Setup(m => m.CheckPasswordAsync(user, "Password1!")).ReturnsAsync(true);
        umMock.Setup(m => m.ResetAccessFailedCountAsync(user))
              .ReturnsAsync(IdentityResult.Success);
        umMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["User"]);
        umMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "refresh-abc",
            UserId = user.Id,
            ExpiresUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };
        tokenMock.Setup(t => t.GenerateRefreshToken(user.Id, It.IsAny<string?>(), It.IsAny<string?>()))
                 .Returns(refreshToken);
        tokenMock.Setup(t => t.GenerateAccessToken(user, It.IsAny<IList<string>>(), refreshToken.Id))
                 .ReturnsAsync("access-token-xyz");

        tokenRepoMock.Setup(r => r.SaveRefreshTokenAsync(It.IsAny<RefreshToken>()))
                     .Returns(Task.CompletedTask);

        SetupTransactionMocks(uowMock);

        var service = CreateService(umMock: umMock, tokenMock: tokenMock,
            tokenRepoMock: tokenRepoMock, uowMock: uowMock);

        var result = await service.LoginAsync(new LoginDto("testuser", "Password1!"));

        result.IsSuccess.Should().BeTrue();
        result.Value.RequiresTwoFactor.Should().BeFalse();
        result.Value.AccessToken.Should().Be("access-token-xyz");
        result.Value.RefreshToken.Should().Be("refresh-abc");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsFailure()
    {
        var umMock = CreateUserManagerMock();
        var user = MakeUser();

        umMock.Setup(m => m.FindByNameAsync("testuser")).ReturnsAsync(user);
        umMock.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        umMock.Setup(m => m.CheckPasswordAsync(user, "WrongPassword!")).ReturnsAsync(false);
        umMock.Setup(m => m.AccessFailedAsync(user)).ReturnsAsync(IdentityResult.Success);

        var service = CreateService(umMock: umMock);

        var result = await service.LoginAsync(new LoginDto("testuser", "WrongPassword!"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid credentials");
    }

    [Fact]
    public async Task LoginAsync_WithUnknownUser_ReturnsFailure()
    {
        var umMock = CreateUserManagerMock();
        umMock.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        umMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var service = CreateService(umMock: umMock);

        var result = await service.LoginAsync(new LoginDto("nobody", "Password1!"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid credentials");
    }

    [Fact]
    public async Task LoginAsync_WithUnconfirmedEmail_ReturnsFailure()
    {
        var umMock = CreateUserManagerMock();
        var user = MakeUser(emailConfirmed: false);

        umMock.Setup(m => m.FindByNameAsync("testuser")).ReturnsAsync(user);
        umMock.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        umMock.Setup(m => m.CheckPasswordAsync(user, "Password1!")).ReturnsAsync(true);
        umMock.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);

        var service = CreateService(umMock: umMock);

        var result = await service.LoginAsync(new LoginDto("testuser", "Password1!"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not been confirmed");
    }

    [Fact]
    public async Task LoginAsync_WhenAccountLockedOut_ReturnsFailure()
    {
        var umMock = CreateUserManagerMock();
        var user = MakeUser();

        umMock.Setup(m => m.FindByNameAsync("testuser")).ReturnsAsync(user);
        umMock.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true);

        var service = CreateService(umMock: umMock);

        var result = await service.LoginAsync(new LoginDto("testuser", "Password1!"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("locked");
    }

    // ── RefreshTokenAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ReturnsFailure()
    {
        var tokenRepoMock = new Mock<ITokenRepository>();
        var expiredToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "old-refresh",
            UserId = "user-1",
            ExpiresUtc = DateTime.UtcNow.AddDays(-1), // expired
            CreatedAtUtc = DateTime.UtcNow.AddDays(-8),
            RevokedAtUtc = null
        };

        tokenRepoMock.Setup(r => r.GetRefreshTokenAsync("old-refresh"))
                     .ReturnsAsync(expiredToken);

        var service = CreateService(tokenRepoMock: tokenRepoMock);

        var result = await service.RefreshTokenAsync("old-refresh");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("expired");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithRevokedToken_RevokesAllSessionsAndReturnsFailure()
    {
        var tokenRepoMock = new Mock<ITokenRepository>();
        var uowMock = new Mock<IUnitOfWork>();

        var revokedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "revoked-refresh",
            UserId = "user-1",
            ExpiresUtc = DateTime.UtcNow.AddDays(5),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            RevokedAtUtc = DateTime.UtcNow.AddHours(-1) // already revoked
        };

        tokenRepoMock.Setup(r => r.GetRefreshTokenAsync("revoked-refresh"))
                     .ReturnsAsync(revokedToken);
        tokenRepoMock.Setup(r => r.DeleteAllRefreshTokensForUserAsync("user-1", null))
                     .Returns(Task.CompletedTask);
        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(1);

        var service = CreateService(tokenRepoMock: tokenRepoMock, uowMock: uowMock);

        var result = await service.RefreshTokenAsync("revoked-refresh");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Security violation");
        tokenRepoMock.Verify(r => r.DeleteAllRefreshTokensForUserAsync("user-1", null), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithNonExistentToken_ReturnsFailure()
    {
        var tokenRepoMock = new Mock<ITokenRepository>();
        tokenRepoMock.Setup(r => r.GetRefreshTokenAsync("unknown")).ReturnsAsync((RefreshToken?)null);

        var service = CreateService(tokenRepoMock: tokenRepoMock);

        var result = await service.RefreshTokenAsync("unknown");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid refresh token");
    }

    // ── LogoutAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task LogoutAsync_WithValidToken_RevokesTokenAndReturnsSuccess()
    {
        var tokenRepoMock = new Mock<ITokenRepository>();
        var uowMock = new Mock<IUnitOfWork>();

        var activeToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "active-refresh",
            UserId = "user-1",
            ExpiresUtc = DateTime.UtcNow.AddDays(5),
            CreatedAtUtc = DateTime.UtcNow,
            RevokedAtUtc = null
        };

        tokenRepoMock.Setup(r => r.GetRefreshTokenAsync("active-refresh"))
                     .ReturnsAsync(activeToken);
        tokenRepoMock.Setup(r => r.UpdateRefreshTokenAsync(activeToken))
                     .Returns(Task.CompletedTask);
        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(1);

        var service = CreateService(tokenRepoMock: tokenRepoMock, uowMock: uowMock);

        var result = await service.LogoutAsync("active-refresh");

        result.IsSuccess.Should().BeTrue();
        activeToken.RevokedAtUtc.Should().NotBeNull();
        tokenRepoMock.Verify(r => r.UpdateRefreshTokenAsync(activeToken), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_WithUnknownToken_ReturnsFailure()
    {
        var tokenRepoMock = new Mock<ITokenRepository>();
        tokenRepoMock.Setup(r => r.GetRefreshTokenAsync("unknown")).ReturnsAsync((RefreshToken?)null);

        var service = CreateService(tokenRepoMock: tokenRepoMock);

        var result = await service.LogoutAsync("unknown");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Token not found");
    }
}
