using Microsoft.EntityFrameworkCore;
using PulseChat.Domain.Entities;

namespace PulseChat.Application.Common.Interfaces;

public interface IPulseChatDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Friendship> Friendships { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
