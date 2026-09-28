using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Application.Features.Auth.Common;
using PulseChat.Domain.Common;
using PulseChat.Domain.Entities;
using RefreshTokenEntity = PulseChat.Domain.Entities.RefreshToken;

namespace PulseChat.Application.Features.Auth.Register;

public record RegisterCommand(
    string Email,
    string Username,
    string Password
) : IRequest<Result<AuthResponse>>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta adresi boş bırakılamaz.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Kullanıcı adı boş bırakılamaz.")
            .MinimumLength(3).WithMessage("Kullanıcı adı en az 3 karakter olmalıdır.")
            .MaximumLength(30).WithMessage("Kullanıcı adı en fazla 30 karakter olabilir.")
            .Matches("^[a-zA-Z0-9_]+$").WithMessage("Kullanıcı adı yalnızca harf, rakam ve alt çizgi içerebilir.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Parola boş bırakılamaz.")
            .MinimumLength(6).WithMessage("Parola en az 6 karakter olmalıdır.");
    }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly IPulseChatDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterCommandHandler(
        IPulseChatDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var emailNormalized = request.Email.Trim().ToLowerInvariant();
        var usernameNormalized = request.Username.Trim();

        var existingEmail = await _context.Users
            .AnyAsync(u => u.Email.ToLower() == emailNormalized, cancellationToken);

        if (existingEmail)
        {
            return Result.Failure<AuthResponse>(ErrorCodes.EmailAlreadyExists, "Bu e-posta adresi zaten kullanımda.");
        }

        var existingUsername = await _context.Users
            .AnyAsync(u => u.Username.ToLower() == usernameNormalized.ToLower(), cancellationToken);

        if (existingUsername)
        {
            return Result.Failure<AuthResponse>(ErrorCodes.UsernameAlreadyExists, "Bu kullanıcı adı zaten kullanımda.");
        }

        var passwordHash = _passwordHasher.HashPassword(request.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = emailNormalized,
            Username = usernameNormalized,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

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

        _context.Users.Add(user);
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        var userDto = new UserDto(user.Id, user.Email, user.Username, user.AvatarUrl, user.CreatedAt);
        return Result.Success(new AuthResponse(accessToken, refreshTokenValue, userDto));
    }
}
