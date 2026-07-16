namespace PageCurl.Maui;

public readonly record struct PageTurnPerformanceSnapshot(
    long FrameCount,
    double LastFrameMilliseconds,
    double AverageFrameMilliseconds,
    long LastFrameAllocatedBytes,
    long MaxFrameAllocatedBytes);
