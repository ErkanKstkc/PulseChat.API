using MediatR;
using Microsoft.EntityFrameworkCore;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Application.Features.Auth.Common;
using PulseChat.Domain.Common;

namespace PulseChat.Application.Features.Auth.Me;

public record GetMeQuery(Guid UserId) : IRequest<Result<UserDto>>;

public class GetMeQueryHandler : IRequestHandler<GetMeQuery, Result<UserDto>>
{
    private readonly IPulseChatDbContext _context;

    public GetMeQueryHandler(IPulseChatDbContext context)
    {
        _context = context;
    }

    public async Task<Result<UserDto>> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            return Result.Failure<UserDto>(ErrorCodes.UserNotFound, "Kullanıcı bulunamadı.");
        }

        return Result.Success(new UserDto(user.Id, user.Email, user.Username, user.AvatarUrl, user.CreatedAt));
    }
}
