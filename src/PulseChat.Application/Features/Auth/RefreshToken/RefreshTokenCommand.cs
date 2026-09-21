using MediatR;
using Microsoft.EntityFrameworkCore;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Application.Features.Auth.Common;
using PulseChat.Domain.Common;
using PulseChat.Domain.Entities;

namespace PulseChat.Application.Features.Auth.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthResponse>>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IPulseChatDbContext _context;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RefreshTokenCommandHandler(
        IPulseChatDbContext context,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var existingToken = await _context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken, cancellationToken);

        if (existingToken == null || existingToken.IsRevoked)
        {
            return Result.Failure<AuthResponse>(ErrorCodes.InvalidToken, "Geçersiz veya iptal edilmiş oturum anahtarı.");
        }

        if (existingToken.ExpiresAt < DateTime.UtcNow)
        {
            return Result.Failure<AuthResponse>(ErrorCodes.TokenExpired, "Oturum süresi dolmuş, lütfen tekrar giriş yapın.");
        }

        var user = existingToken.User;
        if (!user.IsActive)
        {
            return Result.Failure<AuthResponse>(ErrorCodes.Forbidden, "Hesabınız askıya alınmıştır.");
        }

        // Revoke old token and rotate
        existingToken.IsRevoked = true;

        var newAccessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var newRefreshTokenValue = _jwtTokenGenerator.GenerateRefreshToken();

        var newRefreshToken = new Domain.Entities.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = newRefreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        var userDto = new UserDto(user.Id, user.Email, user.Username, user.AvatarUrl, user.CreatedAt);
        return Result.Success(new AuthResponse(newAccessToken, newRefreshTokenValue, userDto));
    }
}
