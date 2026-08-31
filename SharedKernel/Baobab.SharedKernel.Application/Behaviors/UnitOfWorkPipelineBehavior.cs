using Baobab.SharedKernel.Application.Abstractions.Data;
using MediatR;
using System.Transactions;

namespace Baobab.SharedKernel.Application.Behaviors;

public sealed class UnitOfWorkPipelineBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (IsNotCommand()) return await next();

        using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        var response = await next();

        var numberOfRows = await _unitOfWork.SaveChangesAsync(cancellationToken);

        transactionScope.Complete();

        return response;
    }

    private static bool IsNotCommand()
    {
        return !typeof(TRequest).Name.EndsWith("");
    }
}
