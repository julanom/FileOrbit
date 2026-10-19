using Avalonia.Headless.XUnit;
using Filee.App.Services;
using Filee.App.ViewModels;
using Filee.Core.Settings;

namespace Filee.App.Tests;

public sealed class RadialMenuModeTests
{
    [AvaloniaFact]
    public void Actions_wheel_exposes_real_actions_instead_of_conversion_presets()
    {
        TestServices.EnsureInitialized("en");
        var viewModel = AppHost.Get<RadialViewModel>();

        viewModel.Load(["/tmp/photo.jpg", "/tmp/folder"], clickMode: true, TriggerMenu.Actions);

        Assert.Equal(TriggerMenu.Actions, viewModel.Menu);
        Assert.Empty(viewModel.Presets);
        Assert.Contains(FileActionKind.Shelf, viewModel.Actions);
        Assert.Contains(FileActionKind.Copy, viewModel.Actions);
        Assert.Contains(FileActionKind.Move, viewModel.Actions);
        Assert.Contains(FileActionKind.CopyPath, viewModel.Actions);
        Assert.Contains(FileActionKind.Terminal, viewModel.Actions);
        Assert.Equal(viewModel.Actions.Count, viewModel.Items.Count);
    }
}
