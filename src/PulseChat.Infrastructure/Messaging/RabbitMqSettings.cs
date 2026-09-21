namespace PulseChat.Infrastructure.Messaging;

public class RabbitMqSettings
{
    public const string SectionName = "RabbitMqSettings";

    public string Host { get; set; } = "localhost";
    public ushort Port { get; set; } = 5672;
    public string Username { get; set; } = "pulsechat_mq";
    public string Password { get; set; } = "pulsechat_mq_password";
}
