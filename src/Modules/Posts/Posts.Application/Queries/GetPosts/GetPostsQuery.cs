using MediatR;
using Posts.Application.DTOs;
using Shared.Application.DTOs;

namespace Posts.Application.Queries.GetPosts;

public sealed record GetPostsQuery(int Page, int PageSize, bool IncludeAuthor) : IRequest<PagedResult<PostDto>>;
