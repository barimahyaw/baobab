using Baobab.SharedKernel.Domain.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Baobab.SharedKernel.Application.Behaviors;

public class LoggingPipelineBehavior<TRequest, TResponse>(ILogger<TRequest> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResult
{
    private readonly ILogger<TRequest> _logger = logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting request {RequestType}, {@DateTimeUtc}",
            typeof(TRequest).Name,
            DateTime.UtcNow);

        var result = await next();

        if (!result.Succeeded)
        {
            _logger.LogError("Request failure {RequestType}, {@Errors}, {@DateTimeUtc}",
                typeof(TRequest).Name,
                result.Messages,
                DateTime.UtcNow);
        }

        _logger.LogInformation("Completed request {RequestType}, {@DateTimeUtc}",
            typeof(TRequest).Name,
            DateTime.UtcNow);

        return result;
    }
}
