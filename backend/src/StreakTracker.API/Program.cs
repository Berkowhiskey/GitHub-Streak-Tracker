using System.Text;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using StreakTracker.API.Controllers;
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

// "App" bolumu asagida hem DataProtection hem de cerez politikasi icin gerekli.
var appOptions = builder.Configuration.GetSection(AppOptions.SectionName).Get<AppOptions>() ?? new AppOptions();

// SameSite=None yalnizca Secure cerezlerle calisir; yanlis yapilandirma sessizce
// "giris calismiyor" olarak tezahur eder. Bu yuzden acilista dogruluyoruz.
var cookieSameSite = appOptions.ResolveCookieSameSite();

if (cookieSameSite == SameSiteMode.None && builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        "App:CookieSameSite=None yalnizca HTTPS uzerinden calisir ve gelistirme ortami HTTP kullanir. " +
        "Gelistirmede 'Lax' kullanin.");
}

// ---------------------------------------------------------------------------
// Servisler (DI)
// ---------------------------------------------------------------------------
// Access token sifrelemesi icin kullanilan anahtarlar diske kalici olarak yazilir.
// Anahtarlar kaybolursa kayitli token'lar cozulemez ve kullanicilar yeniden giris yapmak zorunda kalir.
// Container ortaminda bu yol kalici bir volume'e baglanmalidir (App:DataProtectionKeysPath).
var dataProtectionKeysPath = string.IsNullOrWhiteSpace(appOptions.DataProtectionKeysPath)
    ? Path.Combine(builder.Environment.ContentRootPath, ".dataprotection-keys")
    : appOptions.DataProtectionKeysPath;

Directory.CreateDirectory(dataProtectionKeysPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("StreakTracker");

builder.Services.AddSingleton<ITokenProtector, TokenProtector>();

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
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.SectionName));
builder.Services.Configure<GitHubAppOptions>(builder.Configuration.GetSection(GitHubAppOptions.SectionName));

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

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Tarayici arayuzu token'i HttpOnly cerezde tasir (XSS'e karsi daha guvenli).
                // Authorization basligi varsa ona dokunulmaz; Swagger ve curl calismaya devam eder.
                if (string.IsNullOrEmpty(context.Token) &&
                    context.Request.Cookies.TryGetValue(AuthController.AuthCookieName, out var cookieToken))
                {
                    context.Token = cookieToken;
                }

                return Task.CompletedTask;
            }
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
builder.Services.AddScoped<IGitHubAppService, GitHubAppService>();
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

// Reverse proxy (Caddy/nginx) arkasinda calisirken istegin gercekte HTTPS uzerinden
// geldigini yalnizca X-Forwarded-Proto basligi soyler. Bu middleware olmadan
// Request.IsHttps false doner; cerez Secure isaretlenmez ve tarayici SameSite=None
// cerezini reddeder - yani giris hic calismaz. Pipeline'in EN BASINDA olmalidir.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // Docker aginda proxy'nin IP'si onceden bilinemez; varsayilan kisitlar
    // temizlenmezse basliklar sessizce yok sayilir.
    KnownNetworks = { },
    KnownProxies = { }
});

app.UseExceptionHandler();

// ---------------------------------------------------------------------------
// Veritabani hazirligi
// ---------------------------------------------------------------------------
// Gelistirmede her zaman, production'da yalnizca App:RunMigrationsOnStartup=true ise.
if (app.Environment.IsDevelopment() || appOptions.RunMigrationsOnStartup)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var protector = scope.ServiceProvider.GetRequiredService<ITokenProtector>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    await db.Database.MigrateAsync();

    // Backfill semaya bagimlidir; migration ile ayni blokta ve ondan SONRA calismalidir.
    await TokenEncryptionBackfill.RunAsync(db, protector, logger);
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

    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new HangfireDashboardAuthorizationFilter(allowAllInDevelopment: true)]
    });
}
else
{
    app.UseHttpsRedirection();

    // Production'da dashboard yalnizca sunucunun kendisinden erisilebilir.
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new HangfireDashboardAuthorizationFilter(allowAllInDevelopment: false)]
    });
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
