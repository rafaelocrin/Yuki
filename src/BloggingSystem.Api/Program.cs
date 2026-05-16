using BloggingSystem.Api.Endpoints;
using BloggingSystem.Api.Middleware;
using BloggingSystem.Application.Behaviors;
using BloggingSystem.Application.Commands.CreatePost;
using BloggingSystem.Application.Projections;
using BloggingSystem.Infrastructure.DependencyInjection;
using FluentValidation;
using MediatR;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreatePostCommand).Assembly));

builder.Services.AddValidatorsFromAssembly(typeof(CreatePostCommand).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddScoped<PostProjection>();
builder.Services.AddScoped<AuthorProjection>();
builder.Services.AddInfrastructure(builder.Configuration);

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
            "**Seeded author IDs** (ready to use in POST /post):\n" +
            "- `11111111-1111-1111-1111-111111111111` — Jane Doe\n" +
            "- `22222222-2222-2222-2222-222222222222` — John Smith"
    });
});

var app = builder.Build();

app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Blogging System API v1");
    c.RoutePrefix = "swagger";
    c.DisplayRequestDuration();
    c.EnableDeepLinking();
});

app.MapCreatePostEndpoint();
app.MapGetPostEndpoint();
app.MapGetPostsEndpoint();
app.MapCreateAuthorEndpoint();

app.Run();

public partial class Program { }
