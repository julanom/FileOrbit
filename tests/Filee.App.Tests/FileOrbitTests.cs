using Avalonia.Headless.XUnit;
using Filee.App.Services;
using Filee.App.ViewModels;
using Filee.App.Views;
using Filee.Core.Localization;
using Filee.Core.Settings;

namespace Filee.App.Tests;

public class FileOrbitTests
{
    [AvaloniaFact]
    public void Late_finder_selection_cannot_replace_a_native_drag()
    {
        TestServices.EnsureInitialized();
        var radial = AppHost.Get<RadialController>();
        var generation = radial.ShowForDrag(400, 400);
        var vm = AppHost.Get<RadialViewModel>();
        vm.Load(["/tmp/browser.png"], false);
        radial.NativeDragEntered();
        radial.SupplyDragFiles(generation, ["/tmp/old-finder-selection.jpg"]);
        Assert.Equal(["/tmp/browser.png"], vm.Files);
        radial.Close(false);
    }

    [AvaloniaFact]
    public void Duplicate_profile_entries_only_create_one_slice()
    {
        TestServices.EnsureInitialized();
        var store = AppHost.Get<UserDataStore>();
        var profile = store.Profiles.Single(p => p.Id == "images");
        var before = profile.PresetIds.ToList();
        try
        {
            profile.PresetIds.Add(profile.PresetIds[0]);
            var vm = AppHost.Get<RadialViewModel>();
            vm.Load(["/tmp/photo.png"], true, TriggerMenu.Convert);
            Assert.Equal(vm.Items.Count, vm.Items.Select(i => i.Id).Distinct().Count());
        }
        finally { profile.PresetIds = before; }
    }

    [AvaloniaTheory]
    [InlineData(ThemeMode.Light, "en")]
    [InlineData(ThemeMode.Dark, "en")]
    [InlineData(ThemeMode.Light, "ar")]
    public void Glass_workspace_renders(ThemeMode mode, string language)
    {
        TestServices.EnsureInitialized(language);
        var store = AppHost.Get<UserDataStore>();
        AppHost.Get<ThemeService>().Apply(new ThemeSettings { Mode = mode }, language);
        using var vm = new MainWindowViewModel(AppHost.Services, AppHost.Get<ILocalizer>(), AppHost.Get<UpdateService>());
        var window = new MainWindow { DataContext = vm };
        window.Show();
        RenderTests.Pump();
        RenderTests.Save(window, $"fileorbit-{mode}-{language}.png");
        Assert.Equal("FileOrbit", window.Title);
        window.HideToTray();
        AppHost.Get<ThemeService>().Apply(store.Settings.Theme, language);
    }
}
