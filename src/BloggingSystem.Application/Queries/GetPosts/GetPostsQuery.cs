using BloggingSystem.Application.DTOs;
using MediatR;

namespace BloggingSystem.Application.Queries.GetPosts;

public sealed record GetPostsQuery(int Page, int PageSize, bool IncludeAuthor) : IRequest<PagedResult<PostDto>>;
