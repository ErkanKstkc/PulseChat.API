using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using PulseChat.Workers.Consumers;

namespace PulseChat.Workers;

public static class DependencyInjection
{
    public static void AddWorkerConsumers(this IBusRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<MongoMessagePersistenceConsumer>();
        configurator.AddConsumer<PushNotificationConsumer>();
    }
}
