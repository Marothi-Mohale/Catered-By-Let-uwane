using cateredByLetsuwi.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// ===============================
// ✅ SQLite path fix (absolute DB path)
// ===============================
var rawConn = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=catering.db";

// If the connection string already contains "Data Source=..."
var dataSourcePrefix = "Data Source=";

// Extract the DB path (works for "Data Source=catering.db" and "Data Source=/abs/path.db")
var dbPathPart = rawConn;
var prefixIndex = rawConn.IndexOf(dataSourcePrefix, StringComparison.OrdinalIgnoreCase);
if (prefixIndex >= 0)
{
    dbPathPart = rawConn.Substring(prefixIndex + dataSourcePrefix.Length).Trim();
}

// If it's relative, convert to absolute using the project folder (ContentRootPath)
if (!Path.IsPathRooted(dbPathPart))
{
    dbPathPart = Path.Combine(builder.Environment.ContentRootPath, dbPathPart);
}

// Rebuild a clean, absolute SQLite connection string
var fixedConn = $"{dataSourcePrefix}{dbPathPart}";

// DB
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(fixedConn));

// ===============================
// ✅ Cookie Authentication (Admin login)
// ===============================
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

// ===============================
// ✅ Authorization Policy: AdminOnly
// ===============================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("IsAdmin", "true"));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

// ✅ must be in this order
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();