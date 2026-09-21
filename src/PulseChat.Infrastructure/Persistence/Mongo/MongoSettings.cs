namespace PulseChat.Infrastructure.Persistence.Mongo;

public class MongoSettings
{
    public const string SectionName = "MongoSettings";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "pulsechat_mongo";
}
