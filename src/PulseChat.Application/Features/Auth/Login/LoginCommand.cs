using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Application.Features.Auth.Common;
using PulseChat.Domain.Common;
using PulseChat.Domain.Entities;
using RefreshTokenEntity = PulseChat.Domain.Entities.RefreshToken;

namespace PulseChat.Application.Features.Auth.Login;

public record LoginCommand(
    string EmailOrUsername,
    string Password
) : IRequest<Result<AuthResponse>>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.EmailOrUsername)
            .NotEmpty().WithMessage("E-posta veya kullanıcı adı boş bırakılamaz.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Parola boş bırakılamaz.");
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IPulseChatDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IPulseChatDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var input = request.EmailOrUsername.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == input || u.Username.ToLower() == input, cancellationToken);

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Result.Failure<AuthResponse>(ErrorCodes.InvalidCredentials, "E-posta/kullanıcı adı veya şifre hatalı.");
        }

        if (!user.IsActive)
        {
            return Result.Failure<AuthResponse>(ErrorCodes.Forbidden, "Hesabınız askıya alınmıştır.");
        }

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var refreshTokenValue = _jwtTokenGenerator.GenerateRefreshToken();

        var refreshToken = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        var userDto = new UserDto(user.Id, user.Email, user.Username, user.AvatarUrl, user.CreatedAt);
        return Result.Success(new AuthResponse(accessToken, refreshTokenValue, userDto));
    }
}
