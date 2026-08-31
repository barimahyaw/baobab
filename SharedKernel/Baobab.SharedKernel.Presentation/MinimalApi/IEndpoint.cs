using Microsoft.AspNetCore.Routing;

namespace Baobab.SharedKernel.Presentation.MinimalApi;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}