using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Baobab.SharedKernel.Application.Telemetry;

/// <summary>
/// Central telemetry definitions for the SharedKernel.
/// Consuming services should register these sources via AddOpenTelemetryConfiguration
/// and use them to create custom spans and metrics for business-level observability.
/// </summary>
public static class SharedKernelTelemetry
{
    public const string SourceName = "Baobab.SharedKernel";

    public static readonly ActivitySource ActivitySource = new(SourceName);

    public static readonly Meter Meter = new(SourceName);
}
