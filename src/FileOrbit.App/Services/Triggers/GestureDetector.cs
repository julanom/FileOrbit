// Pure state machine that turns low-level mouse/keyboard events into "open the donut" gestures.
// It knows nothing about SharpHook or Avalonia so it can be unit tested (see FileOrbit.App.Tests).
//
// Drag gesture:  button down with the exact modifiers  →  (armed)  →  moved past threshold  →  DragStarted
//                →  button up  →  DragEnded
// Hold gesture:  button down with modifiers  →  (armed)  →  HoldElapsed without moving  →  HoldTriggered
// Key chord:     key down with the exact modifiers  →  KeyChordTriggered at the last known cursor position

using FileOrbit.Core.Settings;

namespace FileOrbit.App.Services.Triggers;

/// <summary>Detects configured <see cref="TriggerGesture"/>s in a stream of input events. Thread-safe.</summary>
public sealed class GestureDetector
{
    private readonly object _lock = new();
    private IReadOnlyList<TriggerGesture> _gestures = [];
    private Armed? _armed;
    private bool _dragActive;
    private bool _lateRejected; // scope check failed for this drag; don't re-check on every move
    private TriggerMouseButton _heldButton;
    private int _buttonDownX, _buttonDownY;
    private bool _buttonMovedPastThreshold;
    private int _lastX, _lastY;

    private sealed record Armed(TriggerGesture Gesture, TriggerMouseButton Button, int X, int Y, long Token);

    /// <summary>
    /// Decides whether a gesture may start at a screen point (scope / excluded apps / own windows).
    /// Called on the hook thread; must be fast.
    /// </summary>
    public Func<TriggerGesture, int, int, bool> ScopeFilter { get; set; } = (_, _, _) => true;

    /// <summary>Fallback drag threshold (physical pixels) when a gesture uses 0 = "system default".</summary>
    public int SystemDragThreshold { get; set; } = 4;

    /// <summary>When true nothing is detected (tray "Pause").</summary>
    public bool Paused { get; set; }

    public event Action<TriggerGesture, int, int>? DragStarted;
    public event Action<int, int>? DragEnded;
    public event Action<TriggerGesture, int, int>? HoldTriggered;
    public event Action<TriggerGesture, int, int>? KeyChordTriggered;

    /// <summary>Raised when a hold gesture is armed; the owner must call <see cref="HoldElapsed"/> after the delay.</summary>
    public event Action<long, int>? HoldTimerRequested;

    public void Configure(IEnumerable<TriggerGesture> gestures)
    {
        lock (_lock)
        {
            _gestures = gestures.Where(g => g.Enabled).Select(g => g.Clone()).ToList();
            _armed = null;
        }
    }

    public void MouseDown(TriggerMouseButton button, int x, int y, ModifierKeys modifiers)
    {
        TriggerGesture? holdGesture = null;
        long token = 0;
        lock (_lock)
        {
            _lastX = x;
            _lastY = y;
            _armed = null;
            _lateRejected = false;
            _heldButton = button;
            _buttonDownX = x;
            _buttonDownY = y;
            _buttonMovedPastThreshold = false;
            if (Paused)
                return;

            var gesture = _gestures.FirstOrDefault(g =>
                g.Kind is TriggerKind.Drag or TriggerKind.Hold &&
                g.Button == button &&
                g.Modifiers == modifiers);
            if (gesture is null || !ScopeFilter(gesture, x, y))
                return;

            token = Environment.TickCount64 ^ (x * 7919L) ^ y;
            _armed = new Armed(gesture, button, x, y, token);
            if (gesture.Kind == TriggerKind.Hold)
                holdGesture = gesture;
        }

        if (holdGesture is not null)
            HoldTimerRequested?.Invoke(token, holdGesture.HoldMilliseconds);
    }

    /// <summary>Pointer moved (physical pixels). <paramref name="heldButton"/> is the button currently held, or None.</summary>
    public void MouseMove(int x, int y, ModifierKeys modifiers, TriggerMouseButton heldButton = TriggerMouseButton.None)
    {
        TriggerGesture? fire;
        lock (_lock)
        {
            _lastX = x;
            _lastY = y;
            if (heldButton != TriggerMouseButton.None)
            {
                _heldButton = heldButton;
                if (Math.Abs(x - _buttonDownX) > SystemDragThreshold || Math.Abs(y - _buttonDownY) > SystemDragThreshold)
                    _buttonMovedPastThreshold = true;
            }
            fire = _armed is null ? DetectLateModifier(x, y, modifiers, heldButton) : DetectDragPastThreshold(x, y);
            if (fire is not null)
                _dragActive = true;
        }

        if (fire is not null)
            DragStarted?.Invoke(fire, x, y);
    }

    /// <summary>
    /// An armed gesture moved: a drag gesture fires once the threshold is passed; a hold gesture is cancelled.
    /// Caller holds the lock.
    /// </summary>
    private TriggerGesture? DetectDragPastThreshold(int x, int y)
    {
        var armed = _armed!;
        var threshold = armed.Gesture.DragThreshold > 0 ? armed.Gesture.DragThreshold : SystemDragThreshold;
        if (Math.Abs(x - armed.X) <= threshold && Math.Abs(y - armed.Y) <= threshold)
            return null;

        _armed = null;
        return armed.Gesture.Kind == TriggerKind.Drag ? armed.Gesture : null;
    }

    /// <summary>
    /// The modifier was pressed after the drag had already started (common in Explorer: start dragging,
    /// then press Ctrl to copy). Treat it like a drag gesture that just passed the threshold. Caller holds the lock.
    /// </summary>
    private TriggerGesture? DetectLateModifier(int x, int y, ModifierKeys modifiers, TriggerMouseButton heldButton)
    {
        if (_dragActive || _lateRejected || Paused || heldButton == TriggerMouseButton.None || modifiers == ModifierKeys.None)
            return null;

        var late = _gestures.FirstOrDefault(g =>
            g.Kind == TriggerKind.Drag && g.Button == heldButton && g.Modifiers == modifiers);
        if (late is null)
            return null;
        if (ScopeFilter(late, x, y))
            return late;
        _lateRejected = true;
        return null;
    }

    public void MouseUp(TriggerMouseButton button, int x, int y)
    {
        bool ended;
        lock (_lock)
        {
            _lastX = x;
            _lastY = y;
            _armed = null;
            ended = _dragActive;
            _dragActive = false;
            _lateRejected = false;
            _heldButton = TriggerMouseButton.None;
            _buttonMovedPastThreshold = false;
        }
        if (ended)
            DragEnded?.Invoke(x, y);
    }

    /// <summary>Called when the hold timer for <paramref name="token"/> fires.</summary>
    public void HoldElapsed(long token)
    {
        TriggerGesture? fire = null;
        int x = 0, y = 0;
        lock (_lock)
        {
            if (_armed is { } armed && armed.Token == token && armed.Gesture.Kind == TriggerKind.Hold)
            {
                fire = armed.Gesture;
                (x, y) = (armed.X, armed.Y);
                _armed = null;
            }
        }
        if (fire is not null)
            HoldTriggered?.Invoke(fire, x, y);
    }

    /// <summary>A key went down. <paramref name="key"/> is the key name without the "Vc" prefix, e.g. "Space".</summary>
    public void KeyDown(string key, ModifierKeys modifiers)
    {
        TriggerGesture? chord;
        TriggerGesture? drag = null;
        int x, y;
        lock (_lock)
        {
            if (Paused)
                return;
            chord = _gestures.FirstOrDefault(g =>
                g.Kind == TriggerKind.KeyChord &&
                string.Equals(g.Key, key, StringComparison.OrdinalIgnoreCase) &&
                g.Modifiers == modifiers);
            (x, y) = (_lastX, _lastY);
            if (chord is not null && !ScopeFilter(chord, x, y))
                chord = null;

            // On macOS the intended gesture is: begin a normal Finder drag, then tap Shift/Command/Control.
            // A modifier press is itself enough to open the wheel; waiting for another mouse-move event made a tap
            // look broken when the user held the pointer still after pressing the key.
            if (chord is null && !_dragActive && !_lateRejected && _armed is null && _buttonMovedPastThreshold &&
                _heldButton != TriggerMouseButton.None && modifiers != ModifierKeys.None)
            {
                drag = DetectLateModifier(x, y, modifiers, _heldButton);
                if (drag is not null)
                    _dragActive = true;
            }
        }
        if (chord is not null)
            KeyChordTriggered?.Invoke(chord, x, y);
        if (drag is not null)
            DragStarted?.Invoke(drag, x, y);
    }
}
