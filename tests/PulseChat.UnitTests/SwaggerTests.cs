using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Extensions;
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
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "PulseChat.API",
                Version = "v1",
                Description = "PulseChat REST API"
            });
        });

        var serviceProvider = services.BuildServiceProvider();
        var swaggerProvider = serviceProvider.GetRequiredService<ISwaggerProvider>();

        var action = () => swaggerProvider.GetSwagger("v1");
        action.Should().NotThrow();
        var doc = swaggerProvider.GetSwagger("v1");
        doc.Should().NotBeNull();
        doc.Paths.Should().ContainKey("/api/Media/upload");
        doc.Paths.Should().ContainKey("/api/Chat/rooms/{roomId}/read");

        using var stringWriter = new StringWriter();
        var openApiWriter = new Microsoft.OpenApi.Writers.OpenApiJsonWriter(stringWriter);
        doc.SerializeAsV3(openApiWriter);
        var json = stringWriter.ToString();

        var frontendPath = @"C:\Projeler\PulseChat\PulseChat.Frontend\swagger.json";
        File.WriteAllText(frontendPath, json);
    }
}
