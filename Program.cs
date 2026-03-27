// Program.cs
using Microsoft.EntityFrameworkCore;
using STOREBOOKS.Data;
using STOREBOOKS.Hubs;
using STOREBOOKS.Models;
using STOREBOOKS.Services;
using Serilog;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/storebooks.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

Log.Information("Starting the application...");

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "STOREBOOKS API",
        Version = "v1",
        Description = "API Documentation cho hệ thống bán sách trực tuyến STOREBOOKS",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "STOREBOOKS Development Team",
            Email = "support@storebooks.com"
        }
    });

    // Thêm JWT Authentication vào Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Add SignalR
builder.Services.AddSignalR();

// Add Chatbot Service
builder.Services.AddSingleton<ChatbotService>();

// Add JWT Service
builder.Services.AddScoped<JwtService>();

// Add IHttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Add session support (.NET 8 - Session đã có sẵn trong framework)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Add Authentication - Cookie cho web, JWT cho API
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddCookie("MyCookieAuth", options =>
{
    options.Cookie.Name = "MyCookieAuth";
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
})
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var secretKey = jwtSettings["SecretKey"];
    var issuer = jwtSettings["Issuer"];
    var audience = jwtSettings["Audience"];

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add MoMo Service
var momoConfig = new MoMoConfig();
builder.Configuration.GetSection("MoMo").Bind(momoConfig);
builder.Services.AddSingleton(momoConfig);
builder.Services.AddScoped<MoMoService>();

// Test database connection (chỉ trong Development)
if (builder.Environment.IsDevelopment())
{
    using (var scope = builder.Services.BuildServiceProvider().CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        try
        {
            Log.Information("Checking database connection...");
            dbContext.Database.CanConnect();
            Log.Information("Database connection successful.");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Database connection check failed. Application will continue but database operations may fail.");
        }
    }
}

var app = builder.Build();

// ❌ DISABLED: Seed sample data - Chỉ sử dụng dữ liệu thật từ đơn hàng thực tế
// using (var scope = app.Services.CreateAsyncScope())
// {
//     var scopedProvider = scope.ServiceProvider;
//     try
//     {
//         var dbContext = scopedProvider.GetRequiredService<ApplicationDbContext>();
//         await SampleDataSeeder.SeedRevenueDataAsync(dbContext);
//     }
//     catch (Exception ex)
//     {
//         Log.Warning(ex, "Unable to seed sample revenue data. Continuing without demo data.");
//     }
// }

Log.Information("Configuring the HTTP request pipeline...");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Books/Error");
    app.UseHsts();
}

// Swagger UI - Chỉ hiển thị trong Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "STOREBOOKS API v1");
        options.RoutePrefix = "swagger"; // Truy cập tại /swagger thay vì /swagger/index.html
        options.DisplayRequestDuration();
        options.EnableTryItOutByDefault();
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseAuthentication(); // Add this before UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Books}/{action=Index}/{id?}");

// Map SignalR Hub
app.MapHub<ChatHub>("/chatHub");

Log.Information("Application is running...");
app.Run();

