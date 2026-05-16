using BloggingSystem.Application.DTOs;
using BloggingSystem.Domain.ValueObjects;
using MediatR;

namespace BloggingSystem.Application.Queries.GetPostById;

public sealed record GetPostByIdQuery(PostId PostId, bool IncludeAuthor) : IRequest<PostDto>;
