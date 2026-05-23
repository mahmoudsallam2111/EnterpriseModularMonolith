namespace BuildingBlocks.Application.BackgroundJobs;

/// <summary>
/// Schedules background work. Default implementation uses Quartz.NET. Modules
/// only depend on this interface, never on Quartz types directly.
/// </summary>
public interface IBackgroundJobScheduler
{
    Task EnqueueAsync<TJob>(TJob payload, CancellationToken cancellationToken = default)
        where TJob : class, IBackgroundJob;

    Task ScheduleAsync<TJob>(TJob payload, TimeSpan delay, CancellationToken cancellationToken = default)
        where TJob : class, IBackgroundJob;

    Task ScheduleRecurringAsync<TJob>(TJob payload, string cronExpression, CancellationToken cancellationToken = default)
        where TJob : class, IBackgroundJob;
}

public interface IBackgroundJob;

public interface IBackgroundJobHandler<in TJob>
    where TJob : IBackgroundJob
{
    Task ExecuteAsync(TJob job, CancellationToken cancellationToken);
}
