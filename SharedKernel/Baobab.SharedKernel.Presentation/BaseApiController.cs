using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Baobab.SharedKernel.Presentation;

[Authorize]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class BaseApiController<T> : ControllerBase
    where T : class
{
    private IMediator? _mediatorInstance;
    private ILogger<T>? _loggerInstance;
    protected IMediator Mediator => _mediatorInstance ??= HttpContext.RequestServices.GetService<IMediator>()!;
    protected ILogger<T> Logger => _loggerInstance ??= HttpContext.RequestServices.GetService<ILogger<T>>()!;
}