namespace PulseChat.Application.Features.Auth.Common;

public record UserDto(
    Guid Id,
    string Email,
    string Username,
    string? AvatarUrl,
    DateTime CreatedAt
);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    UserDto User
);
