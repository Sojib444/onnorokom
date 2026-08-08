using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AssignmentManagement.Application.Behaviors;

/// <summary>
/// Logs the start and completion of every MediatR request and warns when a request
/// exceeds the slow threshold. Only the request type name is logged, never the payload,
/// so command data such as passwords can never leak into the logs.
/// </summary>
/// <typeparam name="TRequest">The command or query type.</typeparam>
/// <typeparam name="TResponse">The handler's response type.</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly TimeSpan SlowThreshold = TimeSpan.FromMilliseconds(500);

    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Handling {RequestName} started.", requestName);

        try
        {
            var response = await next();
            stopwatch.Stop();

            _logger.LogInformation(
                "Handling {RequestName} completed in {ElapsedMs} ms.",
                requestName,
                stopwatch.ElapsedMilliseconds);

            if (stopwatch.Elapsed > SlowThreshold)
            {
                _logger.LogWarning(
                    "Handling {RequestName} took {ElapsedMs} ms which exceeds the {ThresholdMs} ms threshold.",
                    requestName,
                    stopwatch.ElapsedMilliseconds,
                    SlowThreshold.TotalMilliseconds);
            }

            return response;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            _logger.LogError(
                exception,
                "Handling {RequestName} failed after {ElapsedMs} ms.",
                requestName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
