using Baobab.SharedKernel.Domain.Results;
using MediatR;

namespace Baobab.SharedKernel.Application.Abstractions.Messaging;

public interface IQuery<IResponse> : IRequest<IResult<IResponse>>
{
}
