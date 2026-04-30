using Microsoft.EntityFrameworkCore;
using Tontine.Data;
using Tontine.Services;

var builder = WebApplication.CreateBuilder(args);

// 🔧 Configuration DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🔐 Services de sécurité
builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();
builder.Services.AddScoped<IShareCodeService, ShareCodeService>();
builder.Services.AddScoped<ISmsService, SmsService>();

// Session & MVC
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<SmsService>();
builder.Services.AddHttpClient<ISmsService, SmsService>();
var app = builder.Build();

// Migration automatique
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// 🔗 Routes personnalisées
app.MapControllerRoute(
    name: "groupe",
    pattern: "groupe/{codePartage}",
    defaults: new { controller = "Groupes", action = "AccederMembre" });

app.MapControllerRoute(
    name: "groupe",
    pattern: "groupe/{codePartage}",
    defaults: new { controller = "Groupes", action = "AccederMembre" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();