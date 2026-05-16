using FluentValidation;

namespace BloggingSystem.Application.Commands.CreateAuthor;

public sealed class CreateAuthorCommandValidator : AbstractValidator<CreateAuthorCommand>
{
    public CreateAuthorCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.");

        RuleFor(x => x.Surname)
            .NotEmpty()
            .WithMessage("Surname is required.");
    }
}
