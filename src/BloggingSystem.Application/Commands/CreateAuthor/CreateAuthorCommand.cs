using MediatR;

namespace BloggingSystem.Application.Commands.CreateAuthor;

public sealed record CreateAuthorCommand(string Name, string Surname) : IRequest<Guid>;
