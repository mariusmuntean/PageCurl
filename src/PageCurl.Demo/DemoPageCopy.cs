namespace PageCurl.Demo;

internal readonly record struct DemoPageCopy(string Title, string Eyebrow, string Body)
{
    public static IReadOnlyList<DemoPageCopy> Pages { get; } = Array.AsReadOnly(new DemoPageCopy[]
    {
        new("Drag to turn the page.", "PAGE TURNS", "Start at an edge or corner. Release to finish the turn or let the page return."),
        new("Choose an anchor.", "ANCHORS", "Hold an edge in place, or pin one corner. Drag from a free edge to turn the page."),
        new("Read in either direction.", "NAVIGATION", "Drag a page, or use Previous and Next to move through the book."),
        new("Try the interactive page.", "LIVE CONTENT", "Choose Content, then View. Tap the counter button, then turn the page."),
        new("Choose the page back.", "REVERSE SIDE", "Choose Backside to compare a separate page, mirrored content and a solid color."),
        new("Adjust the turn.", "RELEASE", "Set how far to drag before changing pages. Enable flicks to turn with a quick gesture."),
        new("Inspect a larger image.", "IMAGE DETAIL", "Single uses a larger image for this page. Compare its text and graphics during the turn."),
        new("Explore the sample.", "PAGECURL.MAUI", "Compare the four scenes and adjust the options. The source includes each example."),
    });
}
