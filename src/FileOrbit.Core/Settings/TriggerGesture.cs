// Describes what the user has to press to open the donut toolbar. Deliberately flexible:
// any modifier combination, any mouse button, optional extra key, drag / hold / key-only.

namespace FileOrbit.Core.Settings;

/// <summary>Modifier keys. Left/right variants are treated the same.</summary>
[Flags]
public enum ModifierKeys
{
    None = 0,
    Ctrl = 1,
    Alt = 2,
    Shift = 4,
    /// <summary>Windows key on Windows, Command on macOS.</summary>
    Meta = 8,
}

/// <summary>Mouse buttons a gesture can use.</summary>
public enum TriggerMouseButton
{
    None,
    Left,
    Right,
    Middle,
    X1,
    X2,
}

/// <summary>What physical action completes the gesture.</summary>
public enum TriggerKind
{
    /// <summary>Hold the modifiers and drag with the mouse button past the threshold. Used for file drags.</summary>
    Drag,
    /// <summary>Hold the modifiers and keep the mouse button pressed without moving for <see cref="TriggerGesture.HoldMilliseconds"/>.</summary>
    Hold,
    /// <summary>Press the modifiers + <see cref="TriggerGesture.Key"/> (no mouse). Uses the file manager selection.</summary>
    KeyChord,
}

/// <summary>Where the gesture is active.</summary>
public enum TriggerScope
{
    /// <summary>
    /// Only when the cursor is over a file manager (Explorer, Finder; on Windows also Files, Directory Opus and other
    /// file managers) or the desktop.
    /// </summary>
    FileManager,
    /// <summary>Everywhere, except excluded applications.</summary>
    Anywhere,
}

/// <summary>Which FileOrbit wheel a gesture opens.</summary>
public enum TriggerMenu
{
    /// <summary>Format conversion presets for the dragged files.</summary>
    Convert,
    /// <summary>Non-format-changing tools such as resize, rotate, split and merge.</summary>
    Tools,
    /// <summary>File-management actions such as AirDrop, shelf, copy and move.</summary>
    Actions,
}

/// <summary>A user-configurable gesture that opens the donut toolbar.</summary>
public sealed class TriggerGesture
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public bool Enabled { get; set; } = true;
    public TriggerKind Kind { get; set; } = TriggerKind.Drag;
    public ModifierKeys Modifiers { get; set; } = ModifierKeys.Ctrl;
    public TriggerMouseButton Button { get; set; } = TriggerMouseButton.Left;

    /// <summary>
    /// Extra non-modifier key (SharpHook <c>KeyCode</c> name without the "Vc" prefix, e.g. <c>"Space"</c>).
    /// Required for <see cref="TriggerKind.KeyChord"/>, optional otherwise.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>Distance in logical pixels the mouse must travel before a drag counts. 0 = system default.</summary>
    public int DragThreshold { get; set; }

    /// <summary>Press duration for <see cref="TriggerKind.Hold"/>.</summary>
    public int HoldMilliseconds { get; set; } = 450;

    public TriggerScope Scope { get; set; } = TriggerScope.FileManager;

    /// <summary>The wheel displayed when this gesture fires.</summary>
    public TriggerMenu Menu { get; set; } = TriggerMenu.Convert;

    public TriggerGesture Clone() => (TriggerGesture)MemberwiseClone();

    /// <summary>Default gestures for the current OS.</summary>
    public static List<TriggerGesture> Defaults()
    {
        // FileOrbit's macOS interaction is intentionally based on tapping a modifier after Finder's drag has begun.
        // GestureDetector's late-modifier path prevents Command/Control from changing Finder's initial drag semantics.
        if (OperatingSystem.IsMacOS())
        {
            return
            [
                new() { Kind = TriggerKind.Drag, Modifiers = ModifierKeys.Shift, Button = TriggerMouseButton.Left, Menu = TriggerMenu.Convert },
                new() { Kind = TriggerKind.Drag, Modifiers = ModifierKeys.Meta, Button = TriggerMouseButton.Left, Menu = TriggerMenu.Tools },
                new() { Kind = TriggerKind.Drag, Modifiers = ModifierKeys.Ctrl, Button = TriggerMouseButton.Left, Menu = TriggerMenu.Actions },
                new() { Kind = TriggerKind.KeyChord, Modifiers = ModifierKeys.Ctrl | ModifierKeys.Alt, Button = TriggerMouseButton.None, Key = "Space", Menu = TriggerMenu.Convert },
            ];
        }

        return
        [
            new() { Kind = TriggerKind.Drag, Modifiers = ModifierKeys.Ctrl, Button = TriggerMouseButton.Left },
            new() { Kind = TriggerKind.KeyChord, Modifiers = ModifierKeys.Ctrl | ModifierKeys.Alt, Button = TriggerMouseButton.None, Key = "Space" },
        ];
    }
}
