using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TaskTracker.Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
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

        if (ShouldLogPayload(request))
        {
            _logger.LogInformation(
                "Handling request {RequestName} with payload {@Request}",
                requestName,
                request);
        }
        else
        {
            _logger.LogInformation(
                "Handling sensitive request {RequestName} with payload redacted",
                requestName);
        }

        try
        {
            var response = await next();

            stopwatch.Stop();

            _logger.LogInformation(
                "Handled request {RequestName} in {ElapsedMilliseconds} ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Error handling request {RequestName} after {ElapsedMilliseconds} ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    private static bool ShouldLogPayload(TRequest request)
    {
        var sensitivePropertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Password",
            "NewPassword",
            "RefreshToken",
            "Token",
            "OtpCode",
            "Code"
        };

        return !request!
            .GetType()
            .GetProperties()
            .Any(property => sensitivePropertyNames.Contains(property.Name));
    }
}
