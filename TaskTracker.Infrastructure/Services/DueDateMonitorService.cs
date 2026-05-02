using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskTracker.Application.Features.Notifications.Commands.EvaluateDueTasks;
using TaskTracker.Application.Options;

namespace TaskTracker.Infrastructure.Services;

/// Background service that runs on a configurable interval to detect tasks
/// approaching or past their due date and pushes notifications.

public class DueDateMonitorService : BackgroundService
{
    private readonly NotificationOptions _notificationOptions;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DueDateMonitorService> _logger;

    public DueDateMonitorService(
        IServiceScopeFactory scopeFactory,
        IOptions<NotificationOptions> notificationOptions,
        ILogger<DueDateMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _notificationOptions = notificationOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(_notificationOptions.DueDateMonitorIntervalMinutes);

        _logger.LogInformation("DueDateMonitorService started. Interval: {Interval}", interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckDueDatesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DueDateMonitorService check cycle");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task CheckDueDatesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new EvaluateDueTasksCommand(), ct);
    }
}
