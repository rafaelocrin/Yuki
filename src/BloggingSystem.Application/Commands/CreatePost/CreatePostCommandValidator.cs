using FluentValidation;

namespace BloggingSystem.Application.Commands.CreatePost;

public sealed class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidator()
    {
        RuleFor(x => x.AuthorId)
            .NotEmpty()
            .WithMessage("AuthorId is required.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.");

        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Content is required.");
    }
}
