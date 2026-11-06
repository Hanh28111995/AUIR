using ApplicationDbContext.Data;
using AUIR_Software.Models.LLM;
using AUIR_Software.Repositories;
using AUIR_Software.Services;
using AUIR_Software.Services.Embedding;
using AUIR_Software.Services.IServices;
using AUIR_Software.Services.Tool;
using AUIR_Software.Services.Vector;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;

const long maxUploadRequestBytes = 210L * 1024L * 1024L;

var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (!File.Exists(envFile))
{
    envFile = Path.Combine(Directory.GetCurrentDirectory(), "AUIR_Software", ".env");
}
if (File.Exists(envFile))
{
    Env.Load(envFile);
}

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxUploadRequestBytes;
});
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = maxUploadRequestBytes;
});

// ======================
// ENV & CONFIGURATION
// ======================
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION");
var geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_KEY");
var qdrantApiKey = Environment.GetEnvironmentVariable("QDRANT_KEY");
var qdrantEndpoint = Environment.GetEnvironmentVariable("QDRANT_ENDPOINT");

if (!string.IsNullOrWhiteSpace(geminiApiKey))
{
    builder.Configuration["Gemini:ApiKey"] = geminiApiKey;
}
if (!string.IsNullOrWhiteSpace(qdrantApiKey))
{
    builder.Configuration["Qdrant:ApiKey"] = qdrantApiKey;
}
if (!string.IsNullOrWhiteSpace(qdrantEndpoint))
{
    builder.Configuration["Qdrant:Endpoint"] = qdrantEndpoint;
}

// FIX: Khởi tạo và Bind dữ liệu chuẩn từ appsettings.json + Biến môi trường vào AppConfig
var appConfig = new AppConfig();
builder.Configuration.Bind(appConfig);
builder.Services.AddSingleton(appConfig); // Đăng ký để QdrantHttpClient và các service khác inject vào dùng trực tiếp

// ======================
// MVC + Swagger
// ======================
builder.Services.AddControllersWithViews();
builder.Services.AddRequestTimeouts(options =>
{
    options.AddPolicy("RagUpload", TimeSpan.FromMinutes(10));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AUIR API",
        Version = "v1",
        Description = """
            API kiểm tra Gemini, chat RAG và nạp tài liệu.
            Để thử các endpoint quản trị trong Swagger, hãy đăng nhập Admin trên cùng host trước.
            Endpoint upload yêu cầu antiforgery token; lấy token qua GET /api/querry/upload-token
            rồi gửi trong header RequestVerificationToken.
            """
    });
    options.IncludeXmlComments(
        Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
});

builder.Services
    .AddRazorPages()
    .AddRazorRuntimeCompilation();

// ======================
// DB
// ======================
builder.Services.AddDbContext<ApplicationDbContext.Data.ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddHttpContextAccessor();

// ======================
// Repository
// ======================
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<ContactRepository>();
builder.Services.AddScoped<ContentRepository>();

// ======================
// App Services
// ======================
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IContentService, ContentService>();

// ======================
// AI Config Resolve (Gemini)
// ======================

// Đọc cấu hình Provider từ appsettings.json
builder.Services.AddScoped<IEmbeddingProvider, GeminiEmbeddingProvider>();
builder.Services.AddScoped<SearchDocTool>();
// Register RAG Pipeline (Mở khóa nếu bạn đã viết xong lớp này)
builder.Services.AddScoped<RagPipeline>();
builder.Services.AddSingleton<RagUploadQueue>();
builder.Services.AddHostedService(serviceProvider =>
    serviceProvider.GetRequiredService<RagUploadQueue>());
builder.Services.AddSingleton<ApiRequestPacer>();

// ======================
// AI Providers & Vector DB
// ======================
builder.Services.AddHttpClient<GeminiApiClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(appConfig.Rag.GeminiRequestTimeoutSeconds);
});
builder.Services.AddScoped<GeminiChatProvider>();

builder.Services.AddScoped<ILLMProviderFactory, LLMProviderFactory>();
builder.Services.AddScoped<IChatService, ChatService>();

// Cấu hình HttpClient Factory cho Qdrant DB
builder.Services.AddHttpClient(nameof(QdrantHttpClient), client =>
{
    client.Timeout = TimeSpan.FromSeconds(appConfig.Rag.QdrantRequestTimeoutSeconds);
});
builder.Services.AddTransient<IQdrantClient, QdrantHttpClient>();

// ======================
// Auth
// ======================
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/LoginPage";
        options.AccessDeniedPath = "/Admin/LoginPage";
        options.Events.OnRedirectToLogin = async context =>
        {
            if (context.Request.Path.StartsWithSegments("/api") ||
                context.Request.Path.StartsWithSegments("/chat"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."
                });
                return;
            }

            context.Response.Redirect(context.RedirectUri);
        };
        options.Events.OnRedirectToAccessDenied = async context =>
        {
            if (context.Request.Path.StartsWithSegments("/api") ||
                context.Request.Path.StartsWithSegments("/chat"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Bạn không có quyền thực hiện thao tác này."
                });
                return;
            }

            context.Response.Redirect(context.RedirectUri);
        };
    });

var app = builder.Build();

// ======================
// Pipeline (Middleware)
// ======================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "AUIR API V1");
        options.RoutePrefix = "swagger";
    });
}

// Static files cache
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
            headers["Cache-Control"] = "public, max-age=31536000, immutable";
        }
        else if (path.EndsWith(".css") || path.EndsWith(".js"))
        {
            headers["Cache-Control"] = "public, max-age=604800";
        }
    }
});

app.UseRouting();
app.UseRequestTimeouts();
app.UseAuthentication();
app.UseAuthorization();

// Middleware chống cache trang HTML công cộng / API để dữ liệu cập nhật thời gian thực
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

// ======================
// Routes Mappings
// ======================
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