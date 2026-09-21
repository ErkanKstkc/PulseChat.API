namespace PulseChat.Application.Common.Interfaces;

public interface IStorageService
{
    Task<string> UploadFileAsync(Stream stream, string fileName, string contentType, string bucketName = "pulsechat-media", CancellationToken cancellationToken = default);
    Task<string> GetPresignedUrlAsync(string fileName, string bucketName = "pulsechat-media", TimeSpan? expiry = null, CancellationToken cancellationToken = default);
}
