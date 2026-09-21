namespace PulseChat.Infrastructure.Caching.Redis;

public class RedisSettings
{
    public const string SectionName = "RedisSettings";

    public string ConnectionString { get; set; } = "localhost:6379";
}
