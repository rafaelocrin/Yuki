using FluentValidation;

namespace Authors.Application.Commands.CreateAuthor;

internal sealed class CreateAuthorCommandValidator : AbstractValidator<CreateAuthorCommand>
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
