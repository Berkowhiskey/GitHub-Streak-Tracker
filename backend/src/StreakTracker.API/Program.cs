using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using StreakTracker.API.Data;
using StreakTracker.API.Jobs;
using StreakTracker.API.Middleware;
using StreakTracker.API.Options;
using StreakTracker.API.Services;
using StreakTracker.API.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Yapilandirma
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection tanimli degil. " +
        "Gelistirme icin appsettings.Development.json dosyasini olusturun " +
        "(ornek: appsettings.Development.example.json).");
}

// ---------------------------------------------------------------------------
// Servisler (DI)
// ---------------------------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
    {
        // GitHub API ve DB arasindaki gecici baglanti kopmalarina karsi otomatik yeniden deneme.
        npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
    }));

// Arka plan gorevleri (Faz 3'te StreakCheckJob burada zamanlanacak).
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));

builder.Services.AddHangfireServer();

// --- Yapilandirma bolumleri ---
builder.Services.Configure<GitHubOptions>(builder.Configuration.GetSection(GitHubOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

// --- Kimlik dogrulama (JWT) ---
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                 ?? throw new InvalidOperationException("Jwt yapilandirma bolumu bulunamadi.");

if (string.IsNullOrWhiteSpace(jwtOptions.Key))
{
    throw new InvalidOperationException(
        "Jwt:Key tanimli degil. appsettings.Development.json dosyasina en az 32 karakterlik bir anahtar girin.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Claim isimlerinin ASP.NET'in eski semasina donusturulmesini engeller;
        // boylece token icindeki "sub" claim'i oldugu gibi okunabilir.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// --- Uygulama servisleri ---
// Dis servis cagrilari icin adlandirilmis HttpClient'lar; soket tuketimini onler.
builder.Services.AddHttpClient(nameof(GitHubService), client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient(nameof(AuthService), client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<IGitHubService, GitHubService>();
builder.Services.AddScoped<IStreakService, StreakService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOnboardingService, OnboardingService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

// Rozet uretimi stateless'tir; tek ornek yeterlidir.
builder.Services.AddSingleton<ISvgBadgeService, SvgBadgeService>();

// Hangfire job siniflari DI uzerinden cozumlenir.
builder.Services.AddScoped<StreakCheckJob>();

// Yakalanmamis istisnalari tutarli ProblemDetails yanitlarina cevirir.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "StreakTracker API",
        Version = "v1",
        Description = "GitHub streak takibi, mobil push bildirimi ve dinamik SVG rozet servisi."
    });

    // Swagger UI'daki "Authorize" butonu ile JWT girilebilmesi icin.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "GitHub girisi sonrasi donen JWT'yi buraya yapistirin ('Bearer' onekine gerek yok)."
    });

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer"), new List<string>() }
    });
});

// Next.js frontend'inin (Faz 6) API'ye erisebilmesi icin.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? ["http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

app.UseExceptionHandler();

// ---------------------------------------------------------------------------
// Gelistirme ortaminda bekleyen migration'lari otomatik uygula
// ---------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// ---------------------------------------------------------------------------
// HTTP pipeline
// ---------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "StreakTracker API v1");
    });

    // TODO (Faz 5): Production'da dashboard mutlaka yetkilendirme filtresi arkasina alinmali.
    app.UseHangfireDashboard("/hangfire");
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors("FrontendPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ---------------------------------------------------------------------------
// Zamanlanmis gorevler
// ---------------------------------------------------------------------------
// Her saat basi calisir; o saati bildirim saati olarak secmis kullanicilari isler.
// Hangfire dashboard'undaki "Recurring jobs" sekmesinden elle de tetiklenebilir.
RecurringJob.AddOrUpdate<StreakCheckJob>(
    StreakCheckJob.RecurringJobId,
    job => job.ExecuteAsync(CancellationToken.None),
    StreakCheckJob.CronExpression,
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

// Servisin ve veritabani baglantisinin ayakta oldugunu dogrulayan basit health endpoint'i.
app.MapGet("/health", async (AppDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();
    return canConnect
        ? Results.Ok(new { status = "healthy", database = "connected" })
        : Results.Problem("Veritabanina baglanilamiyor.", statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.Run();
