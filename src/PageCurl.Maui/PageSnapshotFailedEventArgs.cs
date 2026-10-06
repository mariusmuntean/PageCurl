using Microsoft.Maui.Controls;

namespace PageCurl.Maui;

public sealed class PageSnapshotFailedEventArgs : EventArgs
{
    public PageSnapshotFailedEventArgs(View content, bool isBackside, Exception exception)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        IsBackside = isBackside;
    }

    public View Content { get; }

    /// <summary>
    /// Gets whether the failed content belongs to a physical sheet backside.
    /// For a spread, this is the previous-right or next-left page.
    /// </summary>
    public bool IsBackside { get; }

    public Exception Exception { get; }
}
