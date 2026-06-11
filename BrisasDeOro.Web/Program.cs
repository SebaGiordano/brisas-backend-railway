using BrisasDeOro.Web.Data;
using BrisasDeOro.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BrisasDeOro.Web.Infrastructure;
using Microsoft.OpenApi.Models;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");

string npgsqlConn;
if (databaseUrl != null)
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    npgsqlConn = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
}
else
{
    npgsqlConn = defaultConnection!;
}

builder.Services.AddDbContext<PostgresApplicationDbContext>(options => options.UseNpgsql(npgsqlConn));
builder.Services.AddScoped<ApplicationDbContext>(sp => sp.GetRequiredService<PostgresApplicationDbContext>());

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath        = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccesoDenegado";
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.None;
    options.Cookie.SameSite     = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.None;
    options.Cookie.SameSite     = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
});

builder.Services.AddControllersWithViews();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp", policy =>
        policy.WithOrigins("http://localhost:3000", "https://brisas-de-oro-next.vercel.app")
              .AllowAnyMethod()
              .AllowAnyHeader());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Brisas de Oro",
        Version     = "v1",
        Description = "Backend ASP.NET Core MVC — Hotel y Cabañas Brisas de Oro"
    });
    c.ResolveConflictingActions(api => api.First());
    c.DocumentFilter<SwaggerMvcDocumentFilter>();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Brisas de Oro v1");
        c.RoutePrefix = "swagger";
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseCors("AllowWebApp");
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Inicio}/{action=Index}/{id?}");

app.Run();
