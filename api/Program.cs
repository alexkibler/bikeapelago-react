using Bikeapelago.Api.Repositories;
using Bikeapelago.Api.Services;
using Bikeapelago.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Bikeapelago.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);




// 2.5 Common Services
builder.Services.AddHttpContextAccessor();

var connString = builder.Configuration.GetConnectionString("PostGis") ?? "Host=localhost;Port=5432;Database=bikeapelago;Username=osm;Password=osm_secret";
builder.Services.AddDbContext<BikeapelagoDbContext>(options => 
    options.UseNpgsql(connString, o => o.UseNetTopologySuite())
);

// 3. Register Repositories (Conditional for E2E/Tests)
if (builder.Configuration["USE_MOCK_AUTH"] == "true" || Environment.GetEnvironmentVariable("USE_MOCK_AUTH") == "true")
{
    builder.Services.AddSingleton<IUserRepository, MockUserRepository>();
    builder.Services.AddSingleton<IGameSessionRepository, MockSessionRepository>();
    builder.Services.AddSingleton<IMapNodeRepository, MockNodeRepository>();
}
else
{
    builder.Services.AddScoped<IUserRepository, EfCoreUserRepository>();
    builder.Services.AddScoped<IGameSessionRepository, EfCoreSessionRepository>();
    builder.Services.AddScoped<IMapNodeRepository, EfCoreMapNodeRepository>();
}

// 3.5 Register External Services / Generators
builder.Services.AddHttpClient<OverpassOsmDiscoveryService>();
builder.Services.AddScoped<PbfOsmDiscoveryService>(sp => 
{
    var logger = sp.GetRequiredService<ILogger<PbfOsmDiscoveryService>>();
    var path = builder.Configuration["OsmDiscovery:PbfPath"] ?? "./data/map.osm.pbf";
    return new PbfOsmDiscoveryService(logger, path);
});
builder.Services.AddScoped<PostGisOsmDiscoveryService>();
builder.Services.AddScoped<IOsmDiscoveryService, OsmDiscoveryService>();
builder.Services.AddScoped<NodeGenerationService>();
builder.Services.AddScoped<FitAnalysisService>();

builder.Services.AddSignalR();
builder.Services.AddSingleton<ArchipelagoService>();


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
        var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseMiddleware<ErrorLoggingMiddleware>();

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
app.MapHub<ArchipelagoHub>("/hubs/archipelago");


app.Run();
