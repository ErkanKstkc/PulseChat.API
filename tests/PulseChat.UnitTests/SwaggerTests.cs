using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using Xunit;

namespace PulseChat.UnitTests;

public class SwaggerTests
{
    [Fact]
    public void SwaggerDoc_ShouldGenerateSuccessfully_WithoutErrors()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var envMock = new Moq.Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        services.AddSingleton(envMock.Object);
        services.AddSingleton<Microsoft.Extensions.Hosting.IHostEnvironment>(envMock.Object);
        services.AddControllers()
            .AddApplicationPart(typeof(PulseChat.API.Controllers.MediaController).Assembly);
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        var serviceProvider = services.BuildServiceProvider();
        var swaggerProvider = serviceProvider.GetRequiredService<ISwaggerProvider>();

        var action = () => swaggerProvider.GetSwagger("v1");
        action.Should().NotThrow();
        var doc = swaggerProvider.GetSwagger("v1");
        doc.Should().NotBeNull();
        doc.Paths.Should().ContainKey("/api/Media/upload");
    }
}
