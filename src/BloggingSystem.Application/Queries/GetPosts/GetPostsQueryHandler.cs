using BloggingSystem.Application.DTOs;
using BloggingSystem.Application.Ports;
using MediatR;

namespace BloggingSystem.Application.Queries.GetPosts;

public sealed class GetPostsQueryHandler : IRequestHandler<GetPostsQuery, PagedResult<PostDto>>
{
    private readonly IPostReadRepository _postRepository;

    public GetPostsQueryHandler(IPostReadRepository postRepository)
        => _postRepository = postRepository;

    public async Task<PagedResult<PostDto>> Handle(GetPostsQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _postRepository.GetPagedAsync(
            query.Page, query.PageSize, query.IncludeAuthor, cancellationToken);

        var dtos = items.Select(p =>
        {
            AuthorDto? author = query.IncludeAuthor && p.Author is not null
                ? new AuthorDto(p.Author.Id, p.Author.Name, p.Author.Surname)
                : null;

            return new PostDto(p.Id, p.AuthorId, p.Title, p.Description, p.Content, author);
        }).ToList();

        return new PagedResult<PostDto>(dtos, totalCount, query.Page, query.PageSize);
    }
}
