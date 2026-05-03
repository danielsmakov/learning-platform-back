using FluentValidation;
using LearningPlatform.Application;
using LearningPlatform.Domain;
using LearningPlatform.Infrastructure.Repositories;

namespace LearningPlatform.Application.Services;

public class AuthService(
    IUserRepository userRepository,
    IChildRepository childRepository,
    IAuthRepository authRepository,
    IUnitOfWork unitOfWork,
    JwtTokenService tokenService,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    IValidator<ChildLoginRequest> childLoginValidator)
{
    public async Task<AuthResponse> RegisterParent(RegisterRequest request)
    {
        await registerValidator.ValidateAndThrowAsync(request);
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await userRepository.EmailExists(normalizedEmail))
            throw new InvalidOperationException("Email already exists.");

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
            Role = UserRole.Parent
        };
        await userRepository.Add(user);
        await unitOfWork.SaveChanges();

        return await BuildAuthResponse(user);
    }

    public async Task<AuthResponse> Login(LoginRequest request)
    {
        await loginValidator.ValidateAndThrowAsync(request);
        var user = await userRepository.GetByEmail(request.Email.Trim().ToLowerInvariant())
                   ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        return await BuildAuthResponse(user);
    }

    public async Task<AuthResponse> ChildLogin(ChildLoginRequest request)
    {
        await childLoginValidator.ValidateAndThrowAsync(request);
        var child = await childRepository.GetById(request.ChildId)
                    ?? throw new UnauthorizedAccessException("Invalid child credentials.");
        if (!BCrypt.Net.BCrypt.Verify(request.Pin, child.PinHash))
            throw new UnauthorizedAccessException("Invalid child credentials.");

        var shadowChildUser = new User
        {
            Id = child.Id,
            Email = $"{child.DisplayName}@child.local",
            Role = UserRole.Child
        };
        return await BuildAuthResponse(shadowChildUser);
    }

    public async Task<AuthResponse> Refresh(string refreshToken)
    {
        var storedToken = await authRepository.FindValidRefreshToken(refreshToken)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        storedToken.IsRevoked = true;
        var user = await userRepository.GetById(storedToken.UserId)
                   ?? throw new UnauthorizedAccessException("Token owner not found.");
        var response = await BuildAuthResponse(user);
        await unitOfWork.SaveChanges();
        return response;
    }

    public async Task Logout(string refreshToken)
    {
        var storedToken = await authRepository.FindActiveRefreshToken(refreshToken);

        if (storedToken is null) return;
        storedToken.IsRevoked = true;
        await unitOfWork.SaveChanges();
    }

    private async Task<AuthResponse> BuildAuthResponse(User user)
    {
        var (accessToken, expiresAt) = tokenService.GenerateAccessToken(user);
        var refresh = tokenService.GenerateRefreshToken();

        await authRepository.AddRefreshToken(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = BCrypt.Net.BCrypt.HashPassword(refresh),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await unitOfWork.SaveChanges();

        return new AuthResponse(user.Id, user.Email, user.Role.ToString(), accessToken, refresh, expiresAt);
    }
}
