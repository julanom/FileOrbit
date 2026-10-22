using Avalonia.Headless.XUnit;
using FileOrbit.App.Services;
using FileOrbit.App.ViewModels;
using FileOrbit.Core.Settings;

namespace FileOrbit.App.Tests;

public sealed class RadialMenuModeTests
{
    [AvaloniaFact]
    public void Conversion_back_face_and_actions_remain_independent()
    {
        TestServices.EnsureInitialized("en");
        var vm = AppHost.Get<RadialViewModel>();
        vm.Load(["/tmp/photo.jpg"], true, TriggerMenu.Convert);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.True(vm.ToggleFace());
        Assert.Contains(vm.Items, item => item.Id == "tool-crop");
        vm.Load(["/tmp/photo.jpg"], true, TriggerMenu.Actions);
        Assert.False(vm.CanFlip);
        Assert.False(vm.ToggleFace());
        Assert.Null(vm.PresetAt(0, -1));
        Assert.NotNull(vm.ActionAt(0));
        vm.Load(["/tmp/photo.jpg"], true, TriggerMenu.Tools);
        Assert.Contains(vm.Items, item => item.Id == "tool-crop");
        Assert.Empty(vm.Actions);
        vm.Load(["/tmp/photo.jpg"], true, TriggerMenu.Convert);
    }

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
        if (OperatingSystem.IsMacOS())
            Assert.Contains(FileActionKind.Terminal, viewModel.Actions);
        else
            Assert.DoesNotContain(FileActionKind.Terminal, viewModel.Actions);
        Assert.Equal(viewModel.Actions.Count, viewModel.Items.Count);
    }

    [AvaloniaFact]
    public void Tools_wheel_includes_the_new_pdf_compression_and_slides_tools()
    {
        TestServices.EnsureInitialized("en");
        var viewModel = AppHost.Get<RadialViewModel>();

        viewModel.Load(["/tmp/report.pdf"], clickMode: true, TriggerMenu.Tools);

        Assert.Equal(TriggerMenu.Tools, viewModel.Menu);
        Assert.Contains(viewModel.Items, item => item.Id == "compress-pdf");
        Assert.Contains(viewModel.Items, item => item.Id == "pdf-slides");
    }
}
