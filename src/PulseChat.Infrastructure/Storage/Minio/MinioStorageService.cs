using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using PulseChat.Application.Common.Interfaces;

namespace PulseChat.Infrastructure.Storage.Minio;

public class MinioStorageService : IStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly MinioSettings _settings;

    public MinioStorageService(IOptions<MinioSettings> settings)
    {
        _settings = settings.Value;
        var builder = new MinioClient()
            .WithEndpoint(_settings.Endpoint)
            .WithCredentials(_settings.AccessKey, _settings.SecretKey);

        if (_settings.UseSsl)
        {
            builder = builder.WithSSL();
        }

        _minioClient = builder.Build();
    }

    private async Task EnsureBucketExistsAsync(string bucketName, CancellationToken cancellationToken)
    {
        var beArgs = new BucketExistsArgs().WithBucket(bucketName);
        var found = await _minioClient.BucketExistsAsync(beArgs, cancellationToken);
        if (!found)
        {
            var mbArgs = new MakeBucketArgs().WithBucket(bucketName);
            await _minioClient.MakeBucketAsync(mbArgs, cancellationToken);
        }
    }

    public async Task<string> UploadFileAsync(Stream stream, string fileName, string contentType, string bucketName = "pulsechat-media", CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(bucketName, cancellationToken);

        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";

        var putObjectArgs = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(uniqueFileName)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType(contentType);

        await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);

        return $"/{bucketName}/{uniqueFileName}";
    }

    public async Task<string> GetPresignedUrlAsync(string fileName, string bucketName = "pulsechat-media", TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var presignedArgs = new PresignedGetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(fileName)
            .WithExpiry((int)(expiry ?? TimeSpan.FromHours(1)).TotalSeconds);

        return await _minioClient.PresignedGetObjectAsync(presignedArgs);
    }
}
