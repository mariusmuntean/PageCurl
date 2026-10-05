using System.Numerics;
using PageCurl.Core;
using PageCurl.Maui;
using Microsoft.Maui.Hosting;
using SkiaSharp.Views.Maui.Controls.Hosting;

// This consumer is copied outside the repo before restore. It must resolve
// Core transitively from the MAUI package, without any ProjectReference.
var options = new PageCurlOptions { PageChangeThreshold = .4f, FlickEnabled = false };
var controller = new PageTurnController(options);
var completed = 0;
controller.PageTurnCompleted += (_, args) => { if (args.Committed) completed++; };
if (!controller.BeginProgrammaticTurn(new Vector2(300, 400), PageTurnDirection.Forward, animate: false)
    || completed != 1 || controller.Snapshot.State != PageTurnState.Idle)
    throw new InvalidOperationException("Packaged controller did not complete its public command.");
var mesh = new PageMeshBuffer(options);
PageCurlGeometry.Build(mesh, new(300, 400), controller.GetCurlPose(new(300, 400)), options);
if (mesh.FrontVertexCount == 0 || mesh.BackVertexCount != 0)
    throw new InvalidOperationException("Packaged geometry did not produce an idle front page.");
Console.WriteLine($"PASS package consumer: {typeof(PageTurnView).Assembly.GetName().Name}, transitive Core, command and mesh");

// Compile representative binding, refresh, event and command calls from the
// public consumer surface without requiring a native window in this check.
internal static class ConsumerApi
{
    public static MauiAppBuilder RegisterHandlers(MauiAppBuilder builder) => builder.UseSkiaSharp();

    public static Task UseContent(PageCurlView view, CancellationToken token)
    {
        view.LockedAnchor = PageAnchor.TopLeft;
        view.PageChangeThreshold = .4f;
        view.ReduceMotion = true;
        view.PageTurnCompleted += OnCompleted;
        view.TryTurn(PageTurnDirection.Backward);
        view.CancelInteraction();
        view.PageTurnCompleted -= OnCompleted;
        return view.RefreshSnapshotAsync(token);
    }

    public static Task UseSpread(PageSpreadView view, CancellationToken token)
    {
        view.BacksideMode = PageBacksideMode.CustomContent;
        if (view.CanTurnForward) view.TryTurnForward();
        if (view.CanTurnBackward) view.TryTurnBackward();
        view.CancelInteraction();
        return view.RefreshSnapshotAsync(token);
    }

    private static void OnCompleted(object? sender, PageTurnCompletedEventArgs args) { }
}
