using Baobab.SharedKernel.Domain.Results;
using MediatR;

namespace Baobab.SharedKernel.Application.Abstractions.Messaging;

public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, IResult<TResponse>>
    where TQuery : IQuery<TResponse>
{
}