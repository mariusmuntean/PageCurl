namespace PageCurl.Maui;

internal readonly record struct PageSnapshotCapture(int Generation, CancellationToken Token);

internal sealed class PageSnapshotState<TSnapshot> : IDisposable
    where TSnapshot : class, IDisposable
{
    private CancellationTokenSource? _captureCancellation;
    private int _generation;
    private bool _isDisposed;

    public TSnapshot? Front { get; private set; }

    public TSnapshot? Back { get; private set; }

    public PageSnapshotCapture BeginCapture(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        _captureCancellation?.Cancel();
        _captureCancellation?.Dispose();
        _captureCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        return new PageSnapshotCapture(++_generation, _captureCancellation.Token);
    }

    public bool TryReplaceFront(int generation, TSnapshot snapshot) =>
        TryReplace(generation, snapshot, isFront: true);

    public bool TryReplaceBack(int generation, TSnapshot snapshot) =>
        TryReplace(generation, snapshot, isFront: false);

    public void ClearFront()
    {
        var previous = Front;
        Front = null;
        previous?.Dispose();
    }

    public void ClearBack()
    {
        var previous = Back;
        Back = null;
        previous?.Dispose();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _captureCancellation?.Cancel();
        _captureCancellation?.Dispose();
        _captureCancellation = null;
        ClearFront();
        ClearBack();
    }

    private bool TryReplace(int generation, TSnapshot snapshot, bool isFront)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (_isDisposed
            || generation != _generation
            || _captureCancellation is null
            || _captureCancellation.IsCancellationRequested)
        {
            snapshot.Dispose();
            return false;
        }

        var previous = isFront ? Front : Back;
        if (isFront)
        {
            Front = snapshot;
        }
        else
        {
            Back = snapshot;
        }

        if (!ReferenceEquals(previous, snapshot))
        {
            previous?.Dispose();
        }

        return true;
    }
}
