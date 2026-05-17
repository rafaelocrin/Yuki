using MediatR;
using Posts.Application.DTOs;
using Posts.Application.Ports;
using Shared.Application.DTOs;

namespace Posts.Application.Queries.GetPosts;

internal sealed class GetPostsQueryHandler : IRequestHandler<GetPostsQuery, PagedResult<PostDto>>
{
    private readonly IPostReadRepository _postRepository;

    public GetPostsQueryHandler(IPostReadRepository postRepository)
        => _postRepository = postRepository;

    public async Task<PagedResult<PostDto>> Handle(GetPostsQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _postRepository.GetPagedAsync(
            query.Page, query.PageSize, cancellationToken);

        var dtos = items.Select(p =>
        {
            AuthorDto? author = query.IncludeAuthor
                ? new AuthorDto(p.AuthorId, p.AuthorName, p.AuthorSurname)
                : null;

            return new PostDto(p.Id, p.AuthorId, p.Title, p.Description, p.Content, author);
        }).ToList();

        return new PagedResult<PostDto>(dtos, totalCount, query.Page, query.PageSize);
    }
}
