using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ProductAuthMicroservice.Gateway.API.Middlewares;
using ProductAuthMicroservice.Commons.Extensions;
using ProductAuthMicroservice.Commons.Services;
using ProductAuthMicroservice.Commons.Configs;
using ProductAuthMicroservice.Commons.Configurations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Ocelot
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Configure Authentication for Ocelot with specific scheme name
var jwtConfig = builder.Configuration.GetSection(JwtConfig.SectionName).Get<JwtConfig>();
if (jwtConfig != null)
{
    var key = System.Text.Encoding.UTF8.GetBytes(jwtConfig.Key);
    
    builder.Services.AddAuthentication()
        .AddJwtBearer("Bearer", options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = jwtConfig.ValidateIssuer,
                ValidateAudience = jwtConfig.ValidateAudience,
                ValidIssuer = jwtConfig.Issuer,
                ValidAudience = jwtConfig.Audience,
                ValidateIssuerSigningKey = jwtConfig.ValidateIssuerSigningKey,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = jwtConfig.ValidateLifetime,
                ClockSkew = TimeSpan.FromMinutes(jwtConfig.ClockSkewMinutes)
            };
        });
}

builder.Services.AddOcelot(builder.Configuration);

// Add Authorization
builder.Services.AddAuthorization();

// Configure JWT
builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection(JwtConfig.SectionName));
builder.Services.AddSingleton<IJwtService, JwtService>();

// Add distributed authentication
builder.Services.AddGatewayDistributedAuth(builder.Configuration);

// Add current user service
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient(); // Add HttpClientFactory for CurrentUserService
builder.Services.AddHttpClient("AuthService", client =>
{
    client.BaseAddress = new Uri("http://authservice-api");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// CORS not needed for API Gateway - handled by downstream services

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use distributed auth middleware
app.UseMiddleware<DistributedAuthMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Use Ocelot
await app.UseOcelot();

app.Run();