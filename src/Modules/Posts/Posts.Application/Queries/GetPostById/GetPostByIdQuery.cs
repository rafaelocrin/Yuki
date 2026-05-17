using MediatR;
using Posts.Application.DTOs;
using Posts.Domain.ValueObjects;

namespace Posts.Application.Queries.GetPostById;

public sealed record GetPostByIdQuery(PostId PostId, bool IncludeAuthor) : IRequest<PostDto>;
