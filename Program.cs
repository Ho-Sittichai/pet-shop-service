using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PetShop.Api.Data;
using PetShop.Api.Repositories;
using PetShop.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger configuration with JWT support
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Pet Store & Adoption Management System API",
        Version = "v1",
        Description = "Enterprise RESTful API for Pet Store & Adoption Management System built with ASP.NET Core & Dapper."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token format: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "PetShopManagementSystemSecretKey2026!MustBeAtLeast32BytesLong!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "PetShopApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "PetShopWeb";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Data & Infrastructure
builder.Services.AddSingleton<IDbConnectionFactory, SqliteConnectionFactory>();
builder.Services.AddSingleton<JsonDatabaseManager>();

// Repositories (Dapper)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IPetRepository, PetRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPetShopService, PetShopService>();

var app = builder.Build();

// Initialize JSON Mock Database & SQLite Tables
using (var scope = app.Services.CreateScope())
{
    var dbManager = scope.ServiceProvider.GetRequiredService<JsonDatabaseManager>();
    await dbManager.InitializeAsync();
}

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pet Shop Management API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/", () => Results.Ok(new
{
    Status = "Healthy",
    Service = "Pet Shop Management System API",
    Timestamp = DateTime.UtcNow,
    Docs = "/swagger"
}));

app.Run();
