using Domain.Users;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using SharedKernel;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace Unit.Tests.Services;

public class TokenServiceTests
{
    private static IConfiguration BuildConfig(
        string secret = "super-secret-test-key-that-is-long-enough-32chars",
        string issuer = "TestIssuer",
        string audience = "TestAudience",
        int expirationMinutes = 15,
        int expirationDays = 7)
    {
        var dict = new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = secret,
            ["Jwt:Issuer"] = issuer,
            ["Jwt:Audience"] = audience,
            ["Jwt:ExpirationMinutes"] = expirationMinutes.ToString(),
            ["Jwt:ExpirationDays"] = expirationDays.ToString(),
        };
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static TokenService CreateService(IConfiguration? config = null)
    {
        var dateTimeMock = new Mock<IDateTimeProvider>();
        dateTimeMock.Setup(d => d.UtcNow).Returns(DateTime.UtcNow);
        return new TokenService(config ?? BuildConfig(), dateTimeMock.Object);
    }

    private static User CreateTestUser(string id = "user-123") => new()
    {
        Id = id,
        UserName = "testuser",
        Email = "test@example.com",
        FirstName = "Test",
        LastName = "User"
    };

    // ── GenerateAccessToken ────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateAccessToken_ReturnsNonEmptyToken()
    {
        var service = CreateService();
        var user = CreateTestUser();

        var token = await service.GenerateAccessToken(user, ["User"], Guid.NewGuid());

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GenerateAccessToken_TokenContainsExpectedClaims()
    {
        var service = CreateService();
        var user = CreateTestUser("user-abc");
        var sessionId = Guid.NewGuid();

        var tokenString = await service.GenerateAccessToken(user, ["Admin", "User"], sessionId);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokenString);

        jwt.Subject.Should().Be("user-abc");
        jwt.Claims.Should().Contain(c => c.Type == "email" && c.Value == "test@example.com");
        jwt.Claims.Should().Contain(c => c.Type == "sid" && c.Value == sessionId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "Admin");
        jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "User");
    }

    [Fact]
    public async Task GenerateAccessToken_TokenIsValidJwt()
    {
        var service = CreateService();
        var user = CreateTestUser();

        var tokenString = await service.GenerateAccessToken(user, [], Guid.NewGuid());

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(tokenString).Should().BeTrue();
        var jwt = handler.ReadJwtToken(tokenString);
        jwt.Issuer.Should().Be("TestIssuer");
    }

    // ── GenerateRefreshToken ───────────────────────────────────────────────────

    [Fact]
    public void GenerateRefreshToken_ReturnsRandomUniqueToken()
    {
        var service = CreateService();

        var token1 = service.GenerateRefreshToken("user-1");
        var token2 = service.GenerateRefreshToken("user-1");

        token1.Token.Should().NotBeNullOrWhiteSpace();
        token2.Token.Should().NotBeNullOrWhiteSpace();
        token1.Token.Should().NotBe(token2.Token, "each refresh token must be unique");
    }

    [Fact]
    public void GenerateRefreshToken_HasCorrectUserId()
    {
        var service = CreateService();

        var token = service.GenerateRefreshToken("user-xyz");

        token.UserId.Should().Be("user-xyz");
    }

    [Fact]
    public void GenerateRefreshToken_SetsIpAddressAndUserAgent()
    {
        var service = CreateService();

        var token = service.GenerateRefreshToken("user-1", ipAddress: "127.0.0.1", userAgent: "TestAgent/1.0");

        token.IpAddress.Should().Be("127.0.0.1");
        token.UserAgent.Should().Be("TestAgent/1.0");
    }

    [Fact]
    public void GenerateRefreshToken_TruncatesLongUserAgent()
    {
        var service = CreateService();
        var longAgent = new string('A', 600); // exceeds 512 limit

        var token = service.GenerateRefreshToken("user-1", userAgent: longAgent);

        token.UserAgent!.Length.Should().Be(512);
    }

    [Fact]
    public void GenerateRefreshToken_HasUniqueId()
    {
        var service = CreateService();

        var t1 = service.GenerateRefreshToken("user-1");
        var t2 = service.GenerateRefreshToken("user-1");

        t1.Id.Should().NotBe(t2.Id);
    }

    // ── GetPrincipalFromExpiredToken ───────────────────────────────────────────

    [Fact]
    public async Task GetPrincipalFromExpiredToken_WithValidExpiredToken_ReturnsClaimsPrincipal()
    {
        // Build a token that is already expired by using -1 minute expiry
        var dateTimeMock = new Mock<IDateTimeProvider>();
        dateTimeMock.Setup(d => d.UtcNow).Returns(DateTime.UtcNow.AddMinutes(-30));
        var config = BuildConfig(expirationMinutes: -1);
        var service = new TokenService(config, dateTimeMock.Object);
        var user = CreateTestUser("user-expired");
        var sessionId = Guid.NewGuid();

        var tokenString = await service.GenerateAccessToken(user, ["User"], sessionId);

        // GetPrincipalFromExpiredToken should NOT validate lifetime
        var principal = service.GetPrincipalFromExpiredToken(tokenString);

        principal.Should().NotBeNull();
        // JwtSecurityTokenHandler maps "sub" → ClaimTypes.NameIdentifier by default
        var userId = principal.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)
                  ?? principal.FindFirstValue("sub");
        userId.Should().Be("user-expired");
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithInvalidToken_ThrowsException()
    {
        var service = CreateService();
        var invalidToken = "this.is.not.a.valid.jwt";

        var act = () => service.GetPrincipalFromExpiredToken(invalidToken);

        act.Should().Throw<Exception>("an invalid token should cause validation to fail");
    }

    [Fact]
    public async Task GetPrincipalFromExpiredToken_WithWrongKey_ThrowsException()
    {
        var configA = BuildConfig(secret: "secret-key-A-that-is-at-least-32-chars!!");
        var configB = BuildConfig(secret: "secret-key-B-that-is-at-least-32-chars!!");
        var dateTimeMock = new Mock<IDateTimeProvider>();
        dateTimeMock.Setup(d => d.UtcNow).Returns(DateTime.UtcNow);
        var serviceA = new TokenService(configA, dateTimeMock.Object);
        var serviceB = new TokenService(configB, dateTimeMock.Object);
        var user = CreateTestUser();

        var tokenString = await serviceA.GenerateAccessToken(user, [], Guid.NewGuid());

        var act = () => serviceB.GetPrincipalFromExpiredToken(tokenString);

        act.Should().Throw<Exception>("a token signed with a different key must be rejected");
    }
}
