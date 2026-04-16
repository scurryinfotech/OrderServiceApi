using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OrderService.Repository.Interface;
using OrderService.Repository.Service;
using OrderService.Repository.SQLite;
using OrderService.Services;
using System.Text;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────
// Configuration
// ─────────────────────────────────────────────────────────────
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// ─────────────────────────────────────────────────────────────
// URLs  (must match what the frontend calls)
// appsettings.json can override via "Urls": "https://localhost:7104"
// ─────────────────────────────────────────────────────────────
builder.WebHost.UseUrls(
    builder.Configuration["Urls"] ?? "https://localhost:7104"
);

// ─────────────────────────────────────────────────────────────
// JWT Authentication
// ─────────────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]
    ?? throw new Exception("JWT SecretKey is missing from configuration.");

var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ─────────────────────────────────────────────────────────────
// Razorpay HTTP Client
// ─────────────────────────────────────────────────────────────
builder.Services.AddHttpClient("Razorpay", client =>
{
    var keyId = builder.Configuration["Razorpay:KeyId"];
    var keySecret = builder.Configuration["Razorpay:KeySecret"];

    if (string.IsNullOrEmpty(keyId) || string.IsNullOrEmpty(keySecret))
        throw new Exception("Razorpay KeyId or KeySecret is missing from configuration.");

    var credentials = Convert.ToBase64String(
        Encoding.ASCII.GetBytes($"{keyId}:{keySecret}")
    );
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
});

// ─────────────────────────────────────────────────────────────
// Application Services
// ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IPayrollRepository, PayrollRepository>();
builder.Services.AddScoped<ISalaryDashboardRepository, SalaryDashboardRepository>();
builder.Services.AddScoped<ISalaryPaymentRepository, SalaryPaymentRepository>();

// ── Repositories: SQL vs SQLite ──────────────────────────────
// Set UseSQLite = true in appsettings.json to switch to SQLite for
// DailyExpense, ShopExpense, and Staff repositories.
// Only ONE implementation should be registered per interface.
// ─────────────────────────────────────────────────────────────
bool useSQLite = builder.Configuration.GetValue<bool>("UseSQLite");

if (useSQLite)
{
    builder.Services.AddScoped<IDailyExpenseRepository, DailyExpenseSQLiteRepository>();
    builder.Services.AddTransient<IShopExpenseRepository, ShopExpenseSQLiteRepository>();
    builder.Services.AddTransient<IStaffRepository, StaffSQLiteRepository>();
}
else
{
    builder.Services.AddScoped<IDailyExpenseRepository, DailyExpenseRepository>();
    builder.Services.AddTransient<IShopExpenseRepository, ShopExpenseRepository>();
    builder.Services.AddTransient<IStaffRepository, StaffRepository>();
}

// These do not have SQLite variants yet
builder.Services.AddTransient<IOrderRepository, OrderRepository>();
// Register IOrderService implementation so PaymentService can resolve it
builder.Services.AddTransient<IOrderService, OrderRepository>();

// ─────────────────────────────────────────────────────────────
// Infrastructure
// ─────────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.AddHostedService<SyncService>();

// ─────────────────────────────────────────────────────────────
// CORS
// ─────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// ─────────────────────────────────────────────────────────────
// Controllers & Swagger
// ─────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
        opt.JsonSerializerOptions.PropertyNameCaseInsensitive = true);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ─────────────────────────────────────────────────────────────
// Build & Middleware Pipeline
// ─────────────────────────────────────────────────────────────
var app = builder.Build();

// Swagger (available in all environments; restrict in prod if needed)
app.UseSwagger();
app.UseSwaggerUI();
app.MapGet("/", () => Results.Redirect("/swagger"));

// Only redirect to HTTPS in non-development environments
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");        // Must come BEFORE Auth middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ─────────────────────────────────────────────────────────────
// DI Smoke Test on Startup
// ─────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    try
    {
        scope.ServiceProvider.GetRequiredService<IRoleRepository>();
        scope.ServiceProvider.GetRequiredService<IPaymentService>();
    }
    catch (Exception ex)
    {
        Console.WriteLine("DI resolution failed: " + ex);
        throw;
    }
}

// Ensure PaymentTransactions table exists to avoid runtime SQL errors
try
{
    var connStr = app.Configuration.GetConnectionString("ConnStringDb");
    if (!string.IsNullOrEmpty(connStr))
    {
        using var con = new SqlConnection(connStr);
        con.Open();
        var cmdText = @"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PaymentTransactions]
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RazorpayOrderId NVARCHAR(100) NULL,
        RazorpayPaymentId NVARCHAR(100) NULL,
        RazorpaySignature NVARCHAR(200) NULL,
        Amount DECIMAL(18,2) NULL,
        Currency NVARCHAR(10) NULL,
        Status NVARCHAR(50) NULL,
        Receipt NVARCHAR(200) NULL,
        UserId INT NULL,
        OrderDbId NVARCHAR(100) NULL,
        FailureReason NVARCHAR(500) NULL,
        CreatedAt DATETIME DEFAULT GETDATE(),
        UpdatedAt DATETIME NULL
    )
END";

        using var cmd = new SqlCommand(cmdText, con);
        cmd.ExecuteNonQuery();
    }
}
catch (Exception ex)
{
    Console.WriteLine("Failed to ensure PaymentTransactions table exists: " + ex.Message);
}

app.Run();