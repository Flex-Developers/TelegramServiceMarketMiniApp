using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using TelegramMarketplace.Application.Services;
using TelegramMarketplace.Domain.Interfaces;
using TelegramMarketplace.Infrastructure.Authentication;
using TelegramMarketplace.Infrastructure.Caching;
using TelegramMarketplace.Infrastructure.Configuration;
using TelegramMarketplace.Infrastructure.Payments;
using TelegramMarketplace.Infrastructure.Payments.Robokassa;
using TelegramMarketplace.Infrastructure.Payments.TelegramStars;
using TelegramMarketplace.Infrastructure.Payments.YooKassa;
using TelegramMarketplace.Infrastructure.Persistence;
using TelegramMarketplace.WebAPI.Hubs;
using TelegramMarketplace.WebAPI.Middleware;
using TelegramMarketplace.WebAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Configuration
builder.Services.Configure<TelegramSettings>(builder.Configuration.GetSection("Telegram"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JWT"));
builder.Services.Configure<YooKassaSettings>(builder.Configuration.GetSection("PaymentProviders:YooKassa"));
builder.Services.Configure<RobokassaSettings>(builder.Configuration.GetSection("PaymentProviders:Robokassa"));
builder.Services.Configure<CommissionSettings>(builder.Configuration.GetSection("Commission"));
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
builder.Services.Configure<FileStorageSettings>(builder.Configuration.GetSection("FileStorage"));

// Database
var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "sqlite";
switch (dbProvider.ToLower())
{
    case "inmemory":
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("TelegramMarketplaceDb"));
        break;
    case "sqlite":
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite")));
        break;
    default: // PostgreSQL
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.MigrationsAssembly("TelegramMarketplace.Infrastructure")));
        break;
}

// Redis Cache
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "TelegramMarketplace:";
    });
    builder.Services.AddSingleton<ICacheService, RedisCacheService>();
}
else
{
    builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
}

// Repositories
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Application Services
builder.Services.AddScoped<IServiceService, ServiceService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService>(sp =>
{
    var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
    var notificationService = sp.GetRequiredService<INotificationService>();
    var commission = builder.Configuration.GetValue<decimal>("Commission:Percentage", 10);
    return new OrderService(unitOfWork, notificationService, commission);
});
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuthService, TelegramAuthService>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IRealtimeNotificationService, SignalRNotificationService>();

// Payment Clients
builder.Services.AddHttpClient<IYooKassaClient, YooKassaClient>();
builder.Services.AddSingleton<IRobokassaClient, RobokassaClient>();
builder.Services.AddHttpClient<ITelegramStarsClient, TelegramStarsClient>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
    });

// Telegram Webhook setup service
builder.Services.AddHostedService<TelegramWebhookService>();

// Authentication
var jwtSettings = builder.Configuration.GetSection("JWT").Get<JwtSettings>()!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
    };

    // Support SignalR authentication
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("TelegramMiniApp", policy =>
    {
        policy
            .WithOrigins(
                "https://web.telegram.org",
                "https://webk.telegram.org",
                "https://webz.telegram.org",
                builder.Configuration["Telegram:WebAppUrl"] ?? "http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// SignalR
builder.Services.AddSignalR();

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Telegram Marketplace API",
        Version = "v1",
        Description = "API for Telegram Mini App Services Marketplace"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// Health checks
var healthChecksBuilder = builder.Services.AddHealthChecks();
if (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
    if (!string.IsNullOrEmpty(connectionString))
        healthChecksBuilder.AddNpgSql(connectionString);
    if (!string.IsNullOrEmpty(redisConnectionString))
        healthChecksBuilder.AddRedis(redisConnectionString);
}

var app = builder.Build();

// Middleware pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors("TelegramMiniApp");

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<RateLimitingMiddleware>();

// Endpoints
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHealthChecks("/health");

// Database migration/creation (development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var provider = config.GetValue<string>("DatabaseProvider") ?? "PostgreSQL";

    if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
        await context.Database.MigrateAsync();
    }
    else
    {
        // SQLite or InMemory
        await context.Database.EnsureCreatedAsync();
        await SeedDevelopmentDataAsync(context);
    }
}

app.Run();

// Seed development data
static async Task SeedDevelopmentDataAsync(ApplicationDbContext context)
{
    if (await context.Categories.AnyAsync())
        return;

    // Add categories using factory method
    var designCategory = TelegramMarketplace.Domain.Entities.Category.Create(
        "Дизайн", "Design", "Design", "palette", null, null, 1);
    var devCategory = TelegramMarketplace.Domain.Entities.Category.Create(
        "Разработка", "Development", "Entwicklung", "code", null, null, 2);
    var marketingCategory = TelegramMarketplace.Domain.Entities.Category.Create(
        "Маркетинг", "Marketing", "Marketing", "megaphone", null, null, 3);
    var copyCategory = TelegramMarketplace.Domain.Entities.Category.Create(
        "Копирайтинг", "Copywriting", "Copywriting", "pencil", null, null, 4);

    context.Categories.AddRange(designCategory, devCategory, marketingCategory, copyCategory);

    // Add test user (seller) using factory method
    var seller = TelegramMarketplace.Domain.Entities.User.Create(
        123456789, "Test", "Seller", "test_seller", null, "ru");
    seller.BecomeSeller();
    context.Users.Add(seller);

    // Add test services using factory method
    var service1 = TelegramMarketplace.Domain.Entities.Service.Create(
        seller.Id,
        "Дизайн логотипа",
        "Профессиональный дизайн логотипа для вашего бизнеса",
        designCategory.Id,
        5000,
        TelegramMarketplace.Domain.Enums.PriceType.Fixed,
        3);

    var service2 = TelegramMarketplace.Domain.Entities.Service.Create(
        seller.Id,
        "Разработка Telegram бота",
        "Создание кастомного Telegram бота под ваши задачи",
        devCategory.Id,
        15000,
        TelegramMarketplace.Domain.Enums.PriceType.Fixed,
        7);

    var service3 = TelegramMarketplace.Domain.Entities.Service.Create(
        seller.Id,
        "SMM продвижение",
        "Продвижение вашего бизнеса в социальных сетях",
        marketingCategory.Id,
        10000,
        TelegramMarketplace.Domain.Enums.PriceType.Fixed,
        14);

    var service4 = TelegramMarketplace.Domain.Entities.Service.Create(
        seller.Id,
        "Написание статей",
        "SEO-оптимизированные статьи для вашего сайта",
        copyCategory.Id,
        3000,
        TelegramMarketplace.Domain.Enums.PriceType.Fixed,
        2);

    context.Services.AddRange(service1, service2, service3, service4);

    await context.SaveChangesAsync();
}

// For integration testing
public partial class Program { }
