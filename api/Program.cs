using Microsoft.EntityFrameworkCore;
using Bikeapelago.Api.Data;
using Bikeapelago.Api.Repositories;
using Bikeapelago.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Add HttpClient and PocketBase Service
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<PocketBaseService>();

// 2. Add Database Context (Maintained for future swap)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=bikeapelago.db"));

// 3. Register Repositories (Conditional for E2E/Tests)
if (builder.Configuration["USE_MOCK_AUTH"] == "true" || Environment.GetEnvironmentVariable("USE_MOCK_AUTH") == "true")
{
    builder.Services.AddSingleton<IUserRepository, MockUserRepository>();
    builder.Services.AddSingleton<IGameSessionRepository, MockSessionRepository>();
    builder.Services.AddSingleton<IMapNodeRepository, MockNodeRepository>();
}
else
{
    builder.Services.AddScoped<IUserRepository, PocketBaseUserRepository>();
    builder.Services.AddScoped<IGameSessionRepository, PocketBaseSessionRepository>();
    builder.Services.AddScoped<IMapNodeRepository, PocketBaseNodeRepository>();
}

// 3.5 Register External Services / Generators
builder.Services.AddHttpClient<OverpassService>();
builder.Services.AddScoped<NodeGenerationService>();

// 4. Add Authentication (Stubbed JWT)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "bikeapelago-api",
            ValidAudience = "bikeapelago-frontend",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-secret-key-at-least-32-chars-long"))
        };
    });

// 5. Add Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 6. Add CORS for React Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVite", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configure YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// 7. Database Initialization (EF Core)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// 8. Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowVite");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapReverseProxy();

app.Run();
