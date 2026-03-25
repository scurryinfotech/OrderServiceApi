using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OrderService.Repository.Interface;
using OrderService.Repository.Service;
using OrderService.Repository.SQLite;
using OrderService.Services;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

if (string.IsNullOrEmpty(secretKey))
    throw new Exception("JWT SecretKey is missing from configuration.");

var key = Encoding.ASCII.GetBytes(secretKey);

builder.WebHost.UseUrls("http://localhost:5050");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; 
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });


builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .AddJsonOptions(opt => opt.JsonSerializerOptions.PropertyNameCaseInsensitive = true);

builder.Services.AddTransient<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddTransient<IStaffRepository, StaffRepository>();
builder.Services.AddTransient<IShopExpenseRepository, ShopExpenseRepository>();
builder.Services.AddTransient<IDailyExpenseRepository, DailyExpenseRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IPayrollRepository, PayrollRepository>();
builder.Services.AddScoped<ISalaryDashboardRepository, SalaryDashboardRepository>();
builder.Services.AddScoped<ISalaryPaymentRepository, SalaryPaymentRepository>();

//SQLite ka DI  testing di
builder.Services.AddScoped<IDailyExpenseRepository, DailyExpenseSQLiteRepository>();
builder.Services.AddTransient<IShopExpenseRepository, ShopExpenseSQLiteRepository>();
builder.Services.AddTransient<IStaffRepository, StaffSQLiteRepository>();
//builder.Services.AddScoped<ISalaryPaymentRepository,SalaryPaymentSQLiteRepository>();
//builder.Services.AddScoped<ISalaryDashboardRepository,SalaryDashboardSQLiteRepository>();


builder.Services.AddMemoryCache();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
builder.Services.AddHostedService<SyncService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();


app.MapGet("/", () => Results.Redirect("/swagger"));


app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using var scope = app.Services.CreateScope();
try
{
    scope.ServiceProvider.GetRequiredService<IRoleRepository>();
}
catch (Exception ex)
{
    Console.WriteLine("DI resolution failed: " + ex);
    throw;
}

app.Run();
