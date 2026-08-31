using Baobab.SharedKernel.Domain.Results;
using MediatR;

namespace Baobab.SharedKernel.Application.Abstractions.Messaging;

public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, IResult>
    where TCommand : ICommand
{
}

public interface ICommandHandler<TCommand, TResponse>
    : IRequestHandler<TCommand, IResult<TResponse>>
    where TCommand : ICommand<TResponse>
{
}
