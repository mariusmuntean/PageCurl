using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;
using PageCurl.Core;

namespace PageCurl.Demo;

internal sealed class PageTurnHaptics
{
    private readonly PageTurnFeedbackPolicy _policy = new();

    public bool Enabled { get; set; }

    public static bool IsMobilePlatform =>
        DeviceInfo.Platform == DevicePlatform.Android
        || DeviceInfo.Platform == DevicePlatform.iOS;

    public void OnThreshold(PageChangeThresholdCrossedEventArgs args) =>
        Perform(_policy.GetThresholdPattern(args, Enabled));

    public void OnCompleted(PageTurnCompletedEventArgs args) =>
        Perform(_policy.GetCompletionPattern(args, Enabled));

    public void Reset() => _policy.Reset();

    private static void Perform(PageTurnFeedbackPattern pattern)
    {
        if (!IsMobilePlatform || pattern == PageTurnFeedbackPattern.None)
        {
            return;
        }

        try
        {
            HapticFeedback.Default.Perform(pattern == PageTurnFeedbackPattern.Strong
                ? HapticFeedbackType.LongPress
                : HapticFeedbackType.Click);
        }
        catch (FeatureNotSupportedException)
        {
        }
        catch (PermissionException)
        {
        }
    }
}
