using ApplicationDbContext.Data;
using AUIR_Software.Repositories;
using AUIR_Software.Services;
using AUIR_Software.Services.IServices;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
builder.Services.AddDbContext<ApplicationDbContext.Data.ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<ContactRepository>();
builder.Services.AddScoped<ContentRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IContentService, ContentService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.LoginPath = "/Admin/LoginPage";
        options.AccessDeniedPath = "/Admin/LoginPage";
    });


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Cache static files: 1 năm cho fonts/images, 1 tuần cho CSS/JS
// asp-append-version="true" trong layout đảm bảo cache busting tự động
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.File.Name;
        var headers = ctx.Context.Response.Headers;

        if (path.EndsWith(".woff2") || path.EndsWith(".woff") || path.EndsWith(".eot") ||
            path.EndsWith(".png") || path.EndsWith(".jpg") || path.EndsWith(".webp") ||
            path.EndsWith(".svg") || path.EndsWith(".ico"))
        {
            // Fonts và images: cache 1 năm
            headers["Cache-Control"] = "public, max-age=31536000, immutable";
        }
        else if (path.EndsWith(".css") || path.EndsWith(".js"))
        {
            // CSS/JS: cache 7 ngày (có cache busting qua asp-append-version)
            headers["Cache-Control"] = "public, max-age=604800";
        }
    }
});

app.UseRouting();



app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        string contentType = context.Response.ContentType ?? "";
        
        if (contentType.Contains("text/html") || contentType.Contains("application/json"))
        {
            context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Expires"] = "0";
        }
        return Task.CompletedTask;
    });

    await next();
});

app.MapControllerRoute(
    name: "admin-user",
    pattern: "Admin/User/{action=Index}/{id?}",
    defaults: new { controller = "User" });

app.MapControllerRoute(
    name: "admin-content",
    pattern: "Admin/Content/{action=Index}/{id?}",
    defaults: new { controller = "Content" });

app.MapControllerRoute(
    name: "admin-contact",
    pattern: "Admin/Contact/{action=Index}/{id?}",
    defaults: new { controller = "Contact" });

app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{action=Index}/{id?}",
    defaults: new { controller = "Admin" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");



app.Run();
