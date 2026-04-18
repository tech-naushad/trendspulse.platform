using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;
using TrendsPulse.Platform.API.Middleware;
using TrendsPulse.Platform.API.Services;
using TrendsPulse.Platform.Application.Common.Interfaces;
using TrendsPulse.Platform.Application.Extensions;
using TrendsPulse.Platform.Infrstructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

#region Controllers + JSON
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
#endregion

#region Swagger
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Trend Pulse Platform API",
        Version = "v1",
        Description = "TrendPulse.Platform for managing trends, categories, and dynamic data sources with scalable clean architecture and cloud-ready design."
    });

    // JWT Auth in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token like: Bearer {your token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    // XML comments (safe check)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});
#endregion

#region JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Secret"]!)),

            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();
#endregion

#region DI
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
#endregion

#region CORS
builder.Services.AddCors(o =>
{
    o.AddPolicy("AllowFrontend", p =>
        p.WithOrigins(
            builder.Configuration["Cors:AllowedOrigins"]?.Split(',')
            ?? new[] { "http://localhost:3000" }
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});
#endregion

#region Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);
#endregion

var app = builder.Build();

#region Middleware pipeline

// Global exception handler
app.UseMiddleware<ExceptionMiddleware>();

// Swagger (IMPORTANT: keep outside environment to avoid confusion)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Trend Platform API v1");

    // Keep Swagger at root (optional)
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

#endregion

app.Run();

// Required for integration tests
public partial class Program { }