using System.Numerics;
using System.Reflection;
using PageCurl.Core;
using PageCurl.Maui;
using SkiaSharp;

namespace PageCurl.Apple.Tests;

internal static class NativeMeshTests
{
    public static void Run(List<string> results)
    {
        var options = new PageCurlOptions { TurnAcrossHinge = true };
        var buffer = new PageMeshBuffer(options);
        var size = new Vector2(100, 100);
        var pose = PageCurlGeometry.ComputePose(size, new(100, 50), new(-100, 50),
            PageTurnDirection.Forward, 1, options);
        PageCurlGeometry.Build(buffer, size, pose, options);
        using var textureSurface = SKSurface.Create(new SKImageInfo(100, 100));
        textureSurface.Canvas.Clear(SKColors.Blue);
        using var red = new SKPaint { Color = SKColors.Red };
        textureSurface.Canvas.DrawRect(0, 0, 50, 100, red);
        using var texture = textureSurface.Snapshot();
        using var surface = SKSurface.Create(new SKImageInfo(100, 100));
        using var paint = new SKPaint();
        var points = new SKPoint[buffer.BackVertices.Length];
        var texs = new SKPoint[points.Length];
        var colors = new SKColor[points.Length];
        var draw = typeof(PageTurnView).GetMethod("DrawMesh", BindingFlags.Static | BindingFlags.NonPublic)!;
        object?[] args = [surface.Canvas, buffer.BackVertices, buffer.BackVertexCount, texture,
            PageBacksideMode.CustomContent, 1f, SKColors.White, paint, null, null,
            points, texs, colors, new SKPoint(100, 0), PageEdge.Right, 0];
        draw.Invoke(null, args);
        using var image = surface.Snapshot();
        using var bitmap = SKBitmap.FromImage(image);
        var left = bitmap.GetPixel(25, 50);
        var right = bitmap.GetPixel(75, 50);
        results.Add(left.Red > left.Blue && right.Blue > right.Red
            ? "PASS custom backside reads in landing orientation" : "FAIL custom backside texture is reversed");
        (args[8] as SKShader)?.Dispose();
        args[8] = null;
        args[9] = null;
        args[3] = null;
        args[4] = PageBacksideMode.SolidColor;
        args[13] = new SKPoint();
        args[1] = new MeshVertex[]
        {
            new(new(0, 0), Vector2.Zero, 1, 0), new(new(100, 0), Vector2.Zero, 1, 0),
            new(new(0, 100), Vector2.Zero, 1, 0), new(new(100, 100), Vector2.Zero, 1, 0),
            new(new(100, 0), Vector2.Zero, 1, 0), new(new(0, 100), Vector2.Zero, 1, 0),
        };
        args[2] = 6;
        draw.Invoke(null, args);
        surface.Canvas.Clear(SKColors.Transparent);
        args[2] = 3;
        draw.Invoke(null, args);
        using var shrunk = surface.Snapshot();
        using var shrunkBitmap = SKBitmap.FromImage(shrunk);
        results.Add(shrunkBitmap.GetPixel(80, 80).Alpha == 0
            ? "PASS shrinking mesh clears stale triangles" : "FAIL stale mesh tail is visible");
        args[1] = buffer.BackVertices;
        args[2] = buffer.BackVertexCount;
        args[13] = new SKPoint(100, 0);
        for (var i = 0; i < 20; i++) draw.Invoke(null, args);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        process.Refresh();
        var residentBefore = process.WorkingSet64;
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        for (var i = 0; i < 1000; i++) draw.Invoke(null, args);
        watch.Stop();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        process.Refresh();
        results.Add($"MESH_SUBMISSION frames=1000 elapsed_ms={watch.Elapsed.TotalMilliseconds:F1} managed_bytes={allocated} rss_delta_bytes={process.WorkingSet64 - residentBefore}");
    }
}
