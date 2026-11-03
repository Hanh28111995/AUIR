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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using OpenAI.Chat;
using OpenAI.Embeddings;

Env.Load();
var builder = WebApplication.CreateBuilder(args);

// ======================
// ENV & CONFIGURATION
// ======================
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION");
var openAiKey = Environment.GetEnvironmentVariable("OPENAI_KEY");

// Nạp Key vào bộ cấu hình hệ thống
builder.Configuration["OpenAI:ApiKey"] = openAiKey;

// FIX: Khởi tạo và Bind dữ liệu chuẩn từ appsettings.json + Biến môi trường vào AppConfig
var appConfig = new AppConfig();
builder.Configuration.Bind(appConfig);
builder.Services.AddSingleton(appConfig); // Đăng ký để QdrantHttpClient và các service khác inject vào dùng trực tiếp

// ======================
// MVC + Swagger
// ======================
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AUIR API",
        Version = "v1",
        Description = "RAG Chatbot API"
    });
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
// AI Config Resolve (OpenAI & Ollama)
// ======================

// OpenAI ChatClient: Chỉ ném lỗi khi thực sự gọi đến OpenAI mà không có Key
builder.Services.AddSingleton(sp =>
{
    if (string.IsNullOrWhiteSpace(openAiKey))
    {
        // Không throw trực tiếp ở ngoài nữa, chỉ báo lỗi khi có service cố tình bốc ChatClient của OpenAI ra dùng
        throw new InvalidOperationException("Lỗi: OpenAI ApiKey đang bị trống! Hãy kiểm tra lại file .env hoặc biến môi trường.");
    }

    return new ChatClient(
        model: builder.Configuration["OpenAI:ChatModel"] ?? "gpt-4o-mini",
        apiKey: openAiKey
    );
});

// Đọc cấu hình Provider từ appsettings.json
var llmProvider = builder.Configuration["LLM:Provider"];

if (llmProvider == "Ollama")
{
    builder.Services.AddScoped<IEmbeddingProvider, OllamaEmbeddingProvider>();
    // Đăng ký thêm OllamaChat nếu có...
}
else if (llmProvider == "OpenAI")
{
    builder.Services.AddScoped<IEmbeddingProvider, OpenAIembeddingProvider>();
    // Đăng ký thêm OpenAIChat nếu có...
}
builder.Services.AddScoped<SearchDocTool>();
// Register RAG Pipeline (Mở khóa nếu bạn đã viết xong lớp này)
builder.Services.AddScoped<RagPipeline>();

// ======================
// AI Providers & Vector DB
// ======================
builder.Services.AddScoped<OpenAIchatProvider>();
builder.Services.AddHttpClient<OllamaAIchatProvider>();

builder.Services.AddScoped<ILLMProviderFactory, LLMProviderFactory>();
builder.Services.AddScoped<IChatService, ChatService>();

// Cấu hình HttpClient Factory cho Qdrant DB
builder.Services.AddHttpClient(nameof(QdrantHttpClient));
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