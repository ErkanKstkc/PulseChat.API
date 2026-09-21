namespace PulseChat.Application.Common.Interfaces;

public interface IIdempotencyService
{
    Task<bool> TryAcquireMessageLockAsync(string clientMessageId, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
}
