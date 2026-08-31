using Baobab.SharedKernel.Domain.Results;
using MediatR;

namespace Baobab.SharedKernel.Application.Abstractions.Messaging;

public interface ICommand : IRequest<IResult>
{
}

public interface ICommand<TResponse> : IRequest<IResult<TResponse>>
{
}