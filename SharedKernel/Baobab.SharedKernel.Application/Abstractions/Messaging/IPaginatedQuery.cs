using Baobab.SharedKernel.Domain.Results;
using MediatR;

namespace Baobab.SharedKernel.Application.Abstractions.Messaging;

public interface IPaginatedQuery<TResponse> : IRequest<PaginatedResult<TResponse>>
{
}
