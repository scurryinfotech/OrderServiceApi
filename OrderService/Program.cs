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
    builder.Services.AddScoped<IVendorRepository, VendorRepository>();
    builder.Services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
    builder.Services.AddScoped<IVendorPaymentRepository, VendorPaymentRepository>();

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


app.Run();