namespace PulseChat.Infrastructure.Storage.Minio;

public class MinioSettings
{
    public const string SectionName = "MinioSettings";

    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadminpassword";
    public bool UseSsl { get; set; } = false;
    public string DefaultBucket { get; set; } = "pulsechat-media";
}
