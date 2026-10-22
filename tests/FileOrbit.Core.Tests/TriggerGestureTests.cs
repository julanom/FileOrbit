using FileOrbit.Core.Settings;

namespace FileOrbit.Core.Tests;

public sealed class TriggerGestureTests
{
    [Fact]
    public void Mac_defaults_map_shift_command_and_control_to_the_three_wheels()
    {
        if (!OperatingSystem.IsMacOS())
            return;

        var drags = TriggerGesture.Defaults().Where(gesture => gesture.Kind == TriggerKind.Drag).ToList();

        Assert.Contains(drags, gesture => gesture.Modifiers == ModifierKeys.Shift && gesture.Menu == TriggerMenu.Convert);
        Assert.Contains(drags, gesture => gesture.Modifiers == ModifierKeys.Meta && gesture.Menu == TriggerMenu.Tools);
        Assert.Contains(drags, gesture => gesture.Modifiers == ModifierKeys.Ctrl && gesture.Menu == TriggerMenu.Actions);
    }
}
