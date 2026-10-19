using Filee.App.Views;

namespace Filee.App.Services;

/// <summary>Owns FileOrbit's reusable, always-on-top temporary file shelf.</summary>
public sealed class ShelfService
{
    private ShelfWindow? _window;

    public void Add(IEnumerable<string> paths) => EnsureWindow().Add(paths);

    public void Show()
    {
        var window = EnsureWindow();
        if (!window.IsVisible)
            window.Show();
        window.Activate();
    }

    private ShelfWindow EnsureWindow()
    {
        if (_window is not null)
            return _window;
        _window = new ShelfWindow();
        return _window;
    }
}
