using LearningPlatform.Application;
using LearningPlatform.Application.Services;
using LearningPlatform.Domain;
using LearningPlatform.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace LearningPlatform.Tests;

public class AuthServiceTests
{
    private static IConfiguration CreateJwtConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "0123456789abcdef0123456789abcdef",
                ["Jwt:Issuer"] = "LearningPlatform",
                ["Jwt:Audience"] = "LearningPlatformClient"
            })
            .Build();

    [Fact]
    public async Task RegisterParent_creates_user_when_email_free()
    {
        User? added = null;
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(u => u.EmailExists(It.IsAny<string>())).ReturnsAsync(false);
        userRepo.Setup(u => u.Add(It.IsAny<User>()))
            .Callback<User>(u => added = u)
            .Returns(Task.CompletedTask);

        var childRepo = new Mock<IChildRepository>();
        var authRepo = new Mock<IAuthRepository>();
        authRepo.Setup(a => a.AddRefreshToken(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.SaveChanges()).Returns(Task.CompletedTask);

        var jwt = new JwtTokenService(CreateJwtConfig());
        var sut = new AuthService(
            userRepo.Object,
            childRepo.Object,
            authRepo.Object,
            uow.Object,
            jwt,
            new RegisterRequestValidator(),
            new LoginRequestValidator(),
            new ChildLoginRequestValidator());

        var response = await sut.RegisterParent(new RegisterRequest("  New@Example.COM ", "password1"));

        Assert.NotNull(added);
        Assert.Equal("new@example.com", added!.Email);
        Assert.Equal(UserRole.Parent, added.Role);
        Assert.NotEmpty(response.AccessToken);
        Assert.NotEmpty(response.RefreshToken);
        userRepo.Verify(u => u.Add(It.IsAny<User>()), Times.Once);
        authRepo.Verify(a => a.AddRefreshToken(It.IsAny<RefreshToken>()), Times.Once);
        uow.Verify(x => x.SaveChanges(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task RegisterParent_throws_when_email_taken()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(u => u.EmailExists(It.IsAny<string>())).ReturnsAsync(true);

        var sut = new AuthService(
            userRepo.Object,
            Mock.Of<IChildRepository>(),
            Mock.Of<IAuthRepository>(),
            Mock.Of<IUnitOfWork>(),
            new JwtTokenService(CreateJwtConfig()),
            new RegisterRequestValidator(),
            new LoginRequestValidator(),
            new ChildLoginRequestValidator());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.RegisterParent(new RegisterRequest("a@b.co", "password1")));
    }

    [Fact]
    public async Task Login_returns_tokens_for_valid_credentials()
    {
        var email = "login@test.dev";
        var password = "password1";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4),
            Role = UserRole.Parent
        };

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(u => u.GetByEmail(email)).ReturnsAsync(user);

        var authRepo = new Mock<IAuthRepository>();
        authRepo.Setup(a => a.AddRefreshToken(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.SaveChanges()).Returns(Task.CompletedTask);

        var sut = new AuthService(
            userRepo.Object,
            Mock.Of<IChildRepository>(),
            authRepo.Object,
            uow.Object,
            new JwtTokenService(CreateJwtConfig()),
            new RegisterRequestValidator(),
            new LoginRequestValidator(),
            new ChildLoginRequestValidator());

        var response = await sut.Login(new LoginRequest(email, password));

        Assert.Equal(user.Id, response.UserId);
        Assert.NotEmpty(response.AccessToken);
    }

    [Fact]
    public async Task Login_throws_when_password_wrong()
    {
        var email = "login@test.dev";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct", workFactor: 4),
            Role = UserRole.Parent
        };

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(u => u.GetByEmail(email)).ReturnsAsync(user);

        var sut = new AuthService(
            userRepo.Object,
            Mock.Of<IChildRepository>(),
            Mock.Of<IAuthRepository>(),
            Mock.Of<IUnitOfWork>(),
            new JwtTokenService(CreateJwtConfig()),
            new RegisterRequestValidator(),
            new LoginRequestValidator(),
            new ChildLoginRequestValidator());

        await Assert.ThrowsAsync<AppUnauthorizedException>(() =>
            sut.Login(new LoginRequest(email, "wrong-password")));
    }

    [Fact]
    public async Task ChildLogin_returns_child_role_response()
    {
        var childId = Guid.NewGuid();
        var pin = "1234";
        var child = new Child
        {
            Id = childId,
            ParentId = Guid.NewGuid(),
            Name = "N",
            Age = 7,
            AvatarUrl = "x",
            DisplayName = "Display",
            PinHash = BCrypt.Net.BCrypt.HashPassword(pin, workFactor: 4),
            CurrentProgramId = Guid.NewGuid()
        };

        var childRepo = new Mock<IChildRepository>();
        childRepo.Setup(c => c.GetById(childId)).ReturnsAsync(child);

        var authRepo = new Mock<IAuthRepository>();
        authRepo.Setup(a => a.AddRefreshToken(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.SaveChanges()).Returns(Task.CompletedTask);

        var sut = new AuthService(
            Mock.Of<IUserRepository>(),
            childRepo.Object,
            authRepo.Object,
            uow.Object,
            new JwtTokenService(CreateJwtConfig()),
            new RegisterRequestValidator(),
            new LoginRequestValidator(),
            new ChildLoginRequestValidator());

        var response = await sut.ChildLogin(new ChildLoginRequest(childId, pin));

        Assert.Equal(childId, response.UserId);
        Assert.Equal("Child", response.Role);
        Assert.Contains(child.DisplayName, response.Email);
    }

    [Fact]
    public async Task Logout_does_not_save_when_token_unknown()
    {
        var authRepo = new Mock<IAuthRepository>();
        authRepo.Setup(a => a.FindActiveRefreshToken("missing")).ReturnsAsync((RefreshToken?)null);

        var uow = new Mock<IUnitOfWork>();

        var sut = new AuthService(
            Mock.Of<IUserRepository>(),
            Mock.Of<IChildRepository>(),
            authRepo.Object,
            uow.Object,
            new JwtTokenService(CreateJwtConfig()),
            new RegisterRequestValidator(),
            new LoginRequestValidator(),
            new ChildLoginRequestValidator());

        await sut.Logout("missing");

        uow.Verify(x => x.SaveChanges(), Times.Never);
    }

    [Fact]
    public async Task Refresh_revokes_old_token_and_returns_new_pair()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "u@test.dev",
            PasswordHash = "x",
            Role = UserRole.Parent
        };

        var stored = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = "hash",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsRevoked = false
        };

        var authRepo = new Mock<IAuthRepository>();
        authRepo.Setup(a => a.FindValidRefreshToken("raw-refresh")).ReturnsAsync(stored);
        authRepo.Setup(a => a.AddRefreshToken(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(u => u.GetById(userId)).ReturnsAsync(user);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.SaveChanges()).Returns(Task.CompletedTask);

        var sut = new AuthService(
            userRepo.Object,
            Mock.Of<IChildRepository>(),
            authRepo.Object,
            uow.Object,
            new JwtTokenService(CreateJwtConfig()),
            new RegisterRequestValidator(),
            new LoginRequestValidator(),
            new ChildLoginRequestValidator());

        var response = await sut.Refresh("raw-refresh");

        Assert.True(stored.IsRevoked);
        Assert.Equal(userId, response.UserId);
        Assert.NotEmpty(response.AccessToken);
        authRepo.Verify(a => a.AddRefreshToken(It.IsAny<RefreshToken>()), Times.Once);
    }
}
