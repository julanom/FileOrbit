using System.Runtime.Versioning;
using FileOrbit.Platform.MacOS;

namespace FileOrbit.Platform.Tests;

[SupportedOSPlatform("macos")]
public sealed class MacOSNativeTests
{
    [Fact]
    public void Radial_overlay_joins_every_space_without_conflicting_managed_flags()
    {
        const nint canJoinAllSpaces = 1 << 0, moveToActiveSpace = 1 << 1;
        const nint managed = 1 << 2, transient = 1 << 3, stationary = 1 << 4;
        const nint participatesInCycle = 1 << 5, ignoresCycle = 1 << 6;
        const nint fullScreenPrimary = 1 << 7, fullScreenAuxiliary = 1 << 8, fullScreenNone = 1 << 9;
        const nint fullScreenAllowsTiling = 1 << 11, fullScreenDisallowsTiling = 1 << 12;
        const nint primary = 1 << 16, auxiliary = 1 << 17, canJoinAllApplications = 1 << 18;
        const nint unrelated = 1 << 22;
        var conflicting = moveToActiveSpace | managed | stationary | participatesInCycle | fullScreenPrimary |
                          fullScreenNone | fullScreenAllowsTiling | primary | auxiliary | unrelated;

        var behavior = NativeMethods.OverlayCollectionBehavior(conflicting, macOS13OrLater: true);

        Assert.Equal(0, behavior & (moveToActiveSpace | managed | stationary | participatesInCycle |
                                    fullScreenPrimary | fullScreenNone | fullScreenAllowsTiling | primary | auxiliary));
        Assert.Equal(canJoinAllSpaces | transient | ignoresCycle | fullScreenAuxiliary |
                     fullScreenDisallowsTiling | canJoinAllApplications,
            behavior & ~unrelated);
        Assert.NotEqual(0, behavior & unrelated);
    }

    [Fact]
    public void Pre_macos_13_overlay_omits_the_new_stage_manager_flag()
    {
        const nint canJoinAllApplications = 1 << 18;
        var behavior = NativeMethods.OverlayCollectionBehavior(canJoinAllApplications, macOS13OrLater: false);
        Assert.Equal(0, behavior & canJoinAllApplications);
    }

    [Fact]
    public void NativeFrameworksAndPermissionProbeLoadOnMacOS()
    {
        if (!OperatingSystem.IsMacOS())
        {
            Assert.Skip("Requires macOS frameworks.");
            return;
        }

        var platform = new MacOSPlatformServices();
        _ = platform.HasAccessibilityPermission;
        _ = platform.PrefersReducedMotion;
        _ = platform.ProcessNameAt(0, 0);
        Assert.Equal(4, platform.SystemDragThreshold);
    }
}
