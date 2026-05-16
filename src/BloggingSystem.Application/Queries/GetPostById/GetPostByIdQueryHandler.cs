using BloggingSystem.Application.DTOs;
using BloggingSystem.Application.Ports;
using BloggingSystem.Domain.Exceptions;
using MediatR;

namespace BloggingSystem.Application.Queries.GetPostById;

public sealed class GetPostByIdQueryHandler : IRequestHandler<GetPostByIdQuery, PostDto>
{
    private readonly IPostReadRepository _postRepository;

    public GetPostByIdQueryHandler(IPostReadRepository postRepository)
    {
        _postRepository = postRepository;
    }

    public async Task<PostDto> Handle(GetPostByIdQuery query, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdAsync(query.PostId.Value, query.IncludeAuthor, cancellationToken);
        if (post is null)
            throw new PostNotFoundException(query.PostId);

        AuthorDto? authorDto = null;
        if (query.IncludeAuthor && post.Author is not null)
            authorDto = new AuthorDto(post.Author.Id, post.Author.Name, post.Author.Surname);

        return new PostDto(post.Id, post.AuthorId, post.Title, post.Description, post.Content, authorDto);
    }
}
