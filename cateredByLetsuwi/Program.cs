using cateredByLetsuwi.Data;
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
    });

// Authorization Policy: AdminOnly
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("IsAdmin", "true"));
});

var app = builder.Build();

app.Logger.LogInformation("SQLite database path resolved to: {DbPath}", resolvedDbPath);

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
