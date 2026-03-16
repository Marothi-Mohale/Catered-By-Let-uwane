using cateredByLetsuwi.Data;
using cateredByLetsuwi.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// SQLite: force a single stable absolute DB path under ContentRootPath when relative.
var rawConn = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=catering.db";
var sqliteBuilder = new SqliteConnectionStringBuilder(rawConn);

if (string.IsNullOrWhiteSpace(sqliteBuilder.DataSource))
{
    sqliteBuilder.DataSource = "catering.db";
}

var resolvedDbPath = sqliteBuilder.DataSource;
if (!string.Equals(resolvedDbPath, ":memory:", StringComparison.OrdinalIgnoreCase) &&
    !Path.IsPathRooted(resolvedDbPath))
{
    resolvedDbPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, resolvedDbPath));
    sqliteBuilder.DataSource = resolvedDbPath;
}

var fixedConn = sqliteBuilder.ConnectionString;

// DB
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(fixedConn));

// Cookie Authentication (Admin login)
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
                var redirectUri = $"/Auth/Login?returnUrl={Uri.EscapeDataString(returnUrl)}";
                context.Response.Redirect(redirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
                var redirectUri = $"/Auth/Login?returnUrl={Uri.EscapeDataString(returnUrl)}&denied=true";
                context.Response.Redirect(redirectUri);
                return Task.CompletedTask;
            }
        };
    });

// Authorization Policy: AdminOnly
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("IsAdmin", "true"));
});

var app = builder.Build();

app.Logger.LogInformation("SQLite database path resolved to: {DbPath}", resolvedDbPath);

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    await db.Database.MigrateAsync();

    if (!await db.Services.AnyAsync())
    {
        db.Services.AddRange(
            new Service
            {
                Name = "Bespoke Wedding Banquet",
                Description = "Multi-course plated dining with curated canapes and dessert finale for elegant wedding receptions.",
                Price = 950m
            },
            new Service
            {
                Name = "Executive Corporate Dining",
                Description = "Refined buffet and plated options designed for executive meetings, launches, and premium business events.",
                Price = 680m
            },
            new Service
            {
                Name = "Private Chef Table Experience",
                Description = "Intimate chef-led dining with seasonal tasting menu and luxury table service for private celebrations.",
                Price = 1200m
            },
            new Service
            {
                Name = "Cocktail Reception Collection",
                Description = "Sophisticated finger food, grazing boards, and passed hors d'oeuvres for evening receptions.",
                Price = 540m
            },
            new Service
            {
                Name = "Heritage Celebration Feast",
                Description = "Elevated regional cuisine with modern presentation for milestone family gatherings and cultural events.",
                Price = 620m
            },
            new Service
            {
                Name = "Luxury Garden Brunch",
                Description = "Premium brunch spread with artisanal pastries, live stations, and curated beverage pairings.",
                Price = 590m
            }
        );

        await db.SaveChangesAsync();
        app.Logger.LogInformation("Seeded premium Services catalog because Services table was empty.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
