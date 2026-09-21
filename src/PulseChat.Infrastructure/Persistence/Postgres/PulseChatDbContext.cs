using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Domain.Entities;

namespace PulseChat.Infrastructure.Persistence.Postgres;

public class PulseChatDbContext : DbContext, IPulseChatDbContext
{
    public PulseChatDbContext(DbContextOptions<PulseChatDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Friendship> Friendships => Set<Friendship>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
