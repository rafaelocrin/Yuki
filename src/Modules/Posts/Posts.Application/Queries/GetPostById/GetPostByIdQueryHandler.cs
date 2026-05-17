using MediatR;
using Posts.Application.DTOs;
using Posts.Application.Ports;
using Posts.Domain.Exceptions;

namespace Posts.Application.Queries.GetPostById;

internal sealed class GetPostByIdQueryHandler : IRequestHandler<GetPostByIdQuery, PostDto>
{
    private readonly IPostReadRepository _postRepository;

    public GetPostByIdQueryHandler(IPostReadRepository postRepository)
    {
        _postRepository = postRepository;
    }

    public async Task<PostDto> Handle(GetPostByIdQuery query, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdAsync(query.PostId.Value, cancellationToken);
        if (post is null)
            throw new PostNotFoundException(query.PostId);

        AuthorDto? authorDto = query.IncludeAuthor
            ? new AuthorDto(post.AuthorId, post.AuthorName, post.AuthorSurname)
            : null;

        return new PostDto(post.Id, post.AuthorId, post.Title, post.Description, post.Content, authorDto);
    }
}
