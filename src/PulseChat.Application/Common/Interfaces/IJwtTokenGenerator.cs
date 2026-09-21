using PulseChat.Domain.Entities;

namespace PulseChat.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Guid? ValidateTokenAndGetUserId(string token);
}
