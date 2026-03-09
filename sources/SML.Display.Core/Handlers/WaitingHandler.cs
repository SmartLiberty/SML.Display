namespace SML.Display.Core.Handlers;

using Core.Interfaces.Handlers;

/// <summary>
/// Waiting handler.
/// </summary>
public class WaitingHandler : IWaitingHandler
{
    private readonly object _locker;
    private TaskCompletionSource _taskCompletionSource;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public WaitingHandler()
    {
        _locker = new();
        _taskCompletionSource = new();
    }

    /// <inheritdoc cref="IWaitingHandler"/>>
    public async Task<bool> WaitAsync(CancellationToken cancellationToken)
        => await WaitAsync(Task.Delay(Timeout.Infinite, cancellationToken));

    /// <inheritdoc cref="IWaitingHandler"/>>
    public async Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        => await WaitAsync(Task.Delay(timeout, cancellationToken));

    /// <inheritdoc cref="IWaitingHandler"/>>
    public void Set()
    {
        lock (_locker)
        {
            _taskCompletionSource.TrySetResult();
        }
    }

    /// <inheritdoc cref="IWaitingHandler"/>>
    public void Reset()
    {
        lock (_locker)
        {
            _taskCompletionSource = new TaskCompletionSource();
        }
    }

    private async Task<bool> WaitAsync(Task cancellationTask)
    {
        Task waitTask;
        lock (_locker)
        {
            waitTask = _taskCompletionSource.Task;
            if (waitTask.IsCompleted)
            {
                return true;
            }
        }
        await Task.WhenAny(waitTask, cancellationTask);
        return waitTask.IsCompleted;
    }
}
