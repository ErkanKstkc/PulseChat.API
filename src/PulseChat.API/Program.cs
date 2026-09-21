using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PulseChat.API.Hubs;
using PulseChat.API.Middlewares;
using PulseChat.API.Services;
using PulseChat.Application;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Infrastructure;
using PulseChat.Infrastructure.Persistence.Mongo;
using PulseChat.Infrastructure.Security;
using PulseChat.Workers;

var builder = WebApplication.CreateBuilder(args);

// 1. Application Layer (CQRS, MediatR, FluentValidation)
builder.Services.AddApplication();

// 2. Infrastructure Layer (Postgres, Mongo, Redis, MinIO, RabbitMQ/MassTransit)
builder.Services.AddInfrastructure(builder.Configuration, bus =>
{
    bus.AddWorkerConsumers();
});

// 3. Web API Services & HttpContextAccessor
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 4. SignalR Real-Time Communication
builder.Services.AddSignalR();

// 5. JWT Authentication & WebSocket Query String Extraction
var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
var jwtSettings = jwtSection.Get<JwtSettings>() ?? new JwtSettings();
var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ClockSkew = TimeSpan.Zero
    };

    // WebSocket (SignalR) token extraction from query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// 6. CORS Policy (allowing Frontend apps with credentials for SignalR WebSockets)
builder.Services.AddCors(options =>
{
    options.AddPolicy("PulseChatCorsPolicy", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // In local development allows all origins
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 7. OpenAPI / Swagger with Bearer Authentication
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PulseChat.API",
        Version = "v1",
        Description = "PulseChat Gerçek Zamanlı Mesajlaşma Sistemi REST API & WebSocket Dokümantasyonu"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Örnek: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 8. Middleware Pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PulseChat.API v1");
        c.RoutePrefix = string.Empty; // Swagger UI root sayfasında açılsın (http://localhost:5000/)
    });
}

app.UseCors("PulseChatCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

// 9. Startup check: Ensure Mongo collections and indexes
try
{
    using var scope = app.Services.CreateScope();
    var mongoContext = scope.ServiceProvider.GetService<MongoDbContext>();
    if (mongoContext != null)
    {
        await mongoContext.EnsureIndexesAsync();
    }
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "MongoDB indeksleme başlangıçta ertelendi (veritabanı henüz hazır olmayabilir).");
}

app.Run();
