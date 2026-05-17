using System.Text;
using Authors.Application.Commands.CreateAuthor;
using Authors.Infrastructure.DependencyInjection;
using BloggingSystem.Api.Auth;
using BloggingSystem.Api.Endpoints;
using BloggingSystem.Api.Middleware;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Posts.Application.Commands.CreatePost;
using Posts.Infrastructure.DependencyInjection;
using Serilog;
using Shared.Application.Behaviors;
using Shared.Application.Ports;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Shared.Infrastructure.DependencyInjection;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(30));

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext()
       .Enrich.WithMachineName()
       .Enrich.WithThreadId());

// ── Auth ─────────────────────────────────────────────────────────────────────

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("'Jwt' configuration section is required.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };

        // Return ProblemDetails-shaped JSON for 401 challenges instead of an empty body.
        opts.Events = new JwtBearerEvents
        {
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = 401;
                ctx.Response.ContentType = "application/problem+json";
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    title = "Unauthorized",
                    status = 401,
                    detail = "A valid Bearer token is required to access this resource."
                }));
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ICurrentRequestContext, CurrentRequestContext>();

// ── Application ───────────────────────────────────────────────────────────────

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(
        typeof(CreatePostCommand).Assembly,    // Posts.Application
        typeof(CreateAuthorCommand).Assembly   // Authors.Application
    );
});

builder.Services.AddValidatorsFromAssemblies(new[]
{
    typeof(CreatePostCommand).Assembly,
    typeof(CreateAuthorCommand).Assembly
}, includeInternalTypes: true);

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));

// ── Observability ─────────────────────────────────────────────────────────────

var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
if (!string.IsNullOrEmpty(otlpEndpoint))
{
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService("blogging-api"))
        .WithTracing(t => t
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)));
}

// ── Infrastructure / Modules ──────────────────────────────────────────────────

builder.Services.AddSharedInfrastructure(builder.Configuration, PostsModuleExtensions.AddPostsConsumers);
builder.Services.AddPostsModule(builder.Configuration);
builder.Services.AddAuthorsModule(builder.Configuration);

// ── API infrastructure ────────────────────────────────────────────────────────

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Blogging System API",
        Version = "v1",
        Description =
            "A blogging REST API built with .NET 8 using Hexagonal Architecture, CQRS, and Event Sourcing.\n\n" +
            "**Authentication:** call `POST /auth/token` with demo credentials to get a Bearer token, " +
            "then click **Authorize** and enter `Bearer <token>`.\n\n" +
            "**Seeded author IDs** (ready to use in POST /post):\n" +
            "- `11111111-1111-1111-1111-111111111111` — Jane Doe\n" +
            "- `22222222-2222-2222-2222-222222222222` — John Smith"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Obtain one from POST /auth/token."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Blogging System API v1");
    c.RoutePrefix = "swagger";
    c.DisplayRequestDuration();
    c.EnableDeepLinking();
});

app.MapHealthChecks("/healthz/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteJsonResponse
}).WithTags("Health");

app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
{
    Predicate = hc => hc.Tags.Contains("ready"),
    ResponseWriter = WriteJsonResponse
}).WithTags("Health");

app.MapAuthEndpoint();
app.MapCreatePostEndpoint();
app.MapGetPostEndpoint();
app.MapGetPostsEndpoint();
app.MapCreateAuthorEndpoint();

app.Run();

static Task WriteJsonResponse(HttpContext ctx, HealthReport report)
{
    ctx.Response.ContentType = "application/json";
    var result = JsonSerializer.Serialize(new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            description = e.Value.Description
        })
    });
    return ctx.Response.WriteAsync(result);
}

public partial class Program { }
