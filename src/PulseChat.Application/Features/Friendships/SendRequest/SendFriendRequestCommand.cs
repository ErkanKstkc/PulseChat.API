using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Domain.Common;
using PulseChat.Domain.Entities;
using PulseChat.Domain.Enums;

namespace PulseChat.Application.Features.Friendships.SendRequest;

public record SendFriendRequestCommand(
    Guid RequesterId,
    string TargetUsername
) : IRequest<Result<Guid>>;

public class SendFriendRequestCommandValidator : AbstractValidator<SendFriendRequestCommand>
{
    public SendFriendRequestCommandValidator()
    {
        RuleFor(x => x.TargetUsername)
            .NotEmpty().WithMessage("Hedef kullanıcı adı boş bırakılamaz.");
    }
}

public class SendFriendRequestCommandHandler : IRequestHandler<SendFriendRequestCommand, Result<Guid>>
{
    private readonly IPulseChatDbContext _context;

    public SendFriendRequestCommandHandler(IPulseChatDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(SendFriendRequestCommand request, CancellationToken cancellationToken)
    {
        var targetUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == request.TargetUsername.Trim().ToLower(), cancellationToken);

        if (targetUser == null)
        {
            return Result.Failure<Guid>(ErrorCodes.UserNotFound, "İstek gönderilecek kullanıcı bulunamadı.");
        }

        if (targetUser.Id == request.RequesterId)
        {
            return Result.Failure<Guid>(ErrorCodes.CannotAddSelfAsFriend, "Kendinize arkadaşlık isteği gönderemezsiniz.");
        }

        var existingFriendship = await _context.Friendships
            .FirstOrDefaultAsync(f =>
                (f.RequesterId == request.RequesterId && f.AddresseeId == targetUser.Id) ||
                (f.RequesterId == targetUser.Id && f.AddresseeId == request.RequesterId),
                cancellationToken);

        if (existingFriendship != null)
        {
            return Result.Failure<Guid>(ErrorCodes.FriendshipAlreadyExists, "Bu kullanıcı ile zaten mevcut bir arkadaşlık veya istek kaydınız var.");
        }

        var friendship = new Friendship
        {
            Id = Guid.NewGuid(),
            RequesterId = request.RequesterId,
            AddresseeId = targetUser.Id,
            Status = FriendshipStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Friendships.Add(friendship);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(friendship.Id);
    }
}
