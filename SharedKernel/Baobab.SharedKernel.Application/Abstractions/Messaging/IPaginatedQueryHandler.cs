using Baobab.SharedKernel.Domain.Results;
using MediatR;

namespace Baobab.SharedKernel.Application.Abstractions.Messaging;

public interface IPaginatedQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, PaginatedResult<TResponse>>
    where TQuery : IPaginatedQuery<TResponse>
{
}