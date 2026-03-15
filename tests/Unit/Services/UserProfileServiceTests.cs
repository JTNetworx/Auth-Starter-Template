using Application.DTOs.User;
using Application.Services;
using Domain.Users;
using FluentAssertions;
using Infrastructure.Persistance;
using Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using SharedKernel;
using Xunit;

namespace Unit.Tests.Services;

/// <summary>
/// Unit tests for UserProfileService.
/// Uses EF Core InMemory provider for the DbContext and mocks UserManager.
/// </summary>
public class UserProfileServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<UserManager<User>> _umMock;
    private readonly Mock<IProfileImageStore> _imageMock;
    private readonly Mock<IDateTimeProvider> _dtMock;
    private readonly Mock<IAuditLogService> _auditMock;
    private readonly UserProfileService _service;

    public UserProfileServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // unique DB per test class instance
            .Options;
        _dbContext = new ApplicationDbContext(options);

        var store = new Mock<IUserStore<User>>();
        _umMock = new Mock<UserManager<User>>(
            store.Object, null, null, null, null, null, null, null, null);

        _imageMock = new Mock<IProfileImageStore>();
        _dtMock = new Mock<IDateTimeProvider>();
        _dtMock.Setup(d => d.UtcNow).Returns(DateTime.UtcNow);

        _auditMock = new Mock<IAuditLogService>();
        _auditMock
            .Setup(a => a.RecordAsync(It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object?>()))
            .Returns(Task.CompletedTask);

        _service = new UserProfileService(
            _umMock.Object, _dbContext, _imageMock.Object, _dtMock.Object, _auditMock.Object);
    }

    public void Dispose() => _dbContext.Dispose();

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task<User> SeedUserAsync(string id = "user-1",
        string username = "testuser", string email = "test@example.com")
    {
        var user = new User
        {
            Id = id,
            UserName = username,
            Email = email,
            NormalizedUserName = username.ToUpperInvariant(),
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = "User",
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }

    // ── GetProfileAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetProfileAsync_WithExistingUserId_ReturnsProfile()
    {
        await SeedUserAsync(id: "user-abc", username: "alice", email: "alice@example.com");

        var result = await _service.GetProfileAsync("user-abc");

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("user-abc");
        result.Value.UserName.Should().Be("alice");
        result.Value.Email.Should().Be("alice@example.com");
        result.Value.FirstName.Should().Be("Test");
        result.Value.LastName.Should().Be("User");
    }

    [Fact]
    public async Task GetProfileAsync_WithNonExistentUserId_ReturnsFailure()
    {
        // No user seeded — lookup should fail
        var result = await _service.GetProfileAsync("nonexistent-user");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("User not found");
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsEmailConfirmedFlag()
    {
        await SeedUserAsync(id: "user-confirmed");

        var result = await _service.GetProfileAsync("user-confirmed");

        result.IsSuccess.Should().BeTrue();
        result.Value.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task GetProfileAsync_FullName_IsDerivedFromFirstAndLastName()
    {
        var user = new User
        {
            Id = "user-fn",
            UserName = "john_doe",
            Email = "john@example.com",
            NormalizedUserName = "JOHN_DOE",
            NormalizedEmail = "JOHN@EXAMPLE.COM",
            EmailConfirmed = true,
            FirstName = "John",
            LastName = "Doe",
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetProfileAsync("user-fn");

        result.IsSuccess.Should().BeTrue();
        result.Value.FullName.Should().Be("John Doe");
    }

    // ── UpdateProfileAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProfileAsync_WithValidData_ReturnsUpdatedProfile()
    {
        var seeded = await SeedUserAsync(id: "user-upd");

        _umMock.Setup(m => m.FindByIdAsync("user-upd")).ReturnsAsync(seeded);
        _umMock.Setup(m => m.UpdateAsync(It.IsAny<User>()))
               .ReturnsAsync(IdentityResult.Success);

        // Simulate the EF tracking updating (UserManager.UpdateAsync calls SaveChanges internally
        // in a real scenario — with a mock we need to update the seeded entity manually so
        // GetProfileAsync sees the change in the InMemory store)
        _umMock.Setup(m => m.UpdateAsync(It.IsAny<User>()))
               .Callback<User>(u =>
               {
                   seeded.FirstName = u.FirstName;
                   seeded.LastName = u.LastName;
                   seeded.PhoneNumber = u.PhoneNumber;
                   _dbContext.SaveChanges();
               })
               .ReturnsAsync(IdentityResult.Success);

        var dto = new UpdateUserProfileDto
        {
            FirstName = "Updated",
            LastName = "Name",
            PhoneNumber = "555-0100"
        };

        var result = await _service.UpdateProfileAsync("user-upd", dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("Updated");
        result.Value.LastName.Should().Be("Name");
    }

    [Fact]
    public async Task UpdateProfileAsync_WithNonExistentUserId_ReturnsFailure()
    {
        _umMock.Setup(m => m.FindByIdAsync("missing-user")).ReturnsAsync((User?)null);

        var dto = new UpdateUserProfileDto { FirstName = "X", LastName = "Y" };
        var result = await _service.UpdateProfileAsync("missing-user", dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("User not found");
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenUserManagerFails_ReturnsFailure()
    {
        var seeded = await SeedUserAsync(id: "user-fail");

        _umMock.Setup(m => m.FindByIdAsync("user-fail")).ReturnsAsync(seeded);
        var error = new IdentityError { Description = "Concurrency failure" };
        _umMock.Setup(m => m.UpdateAsync(It.IsAny<User>()))
               .ReturnsAsync(IdentityResult.Failed(error));

        var dto = new UpdateUserProfileDto { FirstName = "X", LastName = "Y" };
        var result = await _service.UpdateProfileAsync("user-fail", dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Concurrency failure");
    }

    // ── DeleteAccountAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAccountAsync_WithNonExistentUser_ReturnsFailure()
    {
        _umMock.Setup(m => m.FindByIdAsync("ghost-user")).ReturnsAsync((User?)null);

        var result = await _service.DeleteAccountAsync("ghost-user");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("User not found");
    }
}
