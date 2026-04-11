namespace Bikeapelago.Api.Services;

/// <summary>
/// Abstraction for queuing elevation download jobs, decoupled from the BackgroundService.
/// </summary>
public interface IElevationJobQueue
{
    Task QueueElevationDownloadAsync(string tileCode, Guid sessionId, int delayMs = 0, int chunkIndex = 0);
}
