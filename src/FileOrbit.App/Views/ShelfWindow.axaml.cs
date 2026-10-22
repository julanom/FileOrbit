using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FileOrbit.App.Views;

public sealed record ShelfItem(string Path)
{
    public string Name => System.IO.Path.GetFileName(Path);
}

public sealed partial class ShelfViewModel : ObservableObject
{
    public ObservableCollection<ShelfItem> Items { get; } = [];
    public bool IsEmpty => Items.Count == 0;
    public bool HasItems => Items.Count > 0;

    public void Changed()
    {
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasItems));
    }
}

public partial class ShelfWindow : Window
{
    private readonly ShelfViewModel _viewModel = new();

    public ShelfWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        Closing += (_, e) =>
        {
            e.Cancel = true;
            Hide();
        };
        DragDrop.AddDragOverHandler(DropArea, OnDragOver);
        DragDrop.AddDropHandler(DropArea, OnDrop);
    }

    public void Add(IEnumerable<string> paths)
    {
        foreach (var path in paths.Where(path => File.Exists(path) || Directory.Exists(path)))
        {
            if (_viewModel.Items.All(item => !string.Equals(item.Path, path, StringComparison.Ordinal)))
                _viewModel.Items.Add(new ShelfItem(path));
        }
        _viewModel.Changed();
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        Add((e.DataTransfer.TryGetFiles() ?? []).Select(item => item.TryGetLocalPath()).OfType<string>());
        e.Handled = true;
    }

    private async void OnItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: ShelfItem item } || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;
        IStorageItem? storage = File.Exists(item.Path)
            ? await StorageProvider.TryGetFileFromPathAsync(item.Path)
            : await StorageProvider.TryGetFolderFromPathAsync(item.Path);
        if (storage is null)
            return;

        using var transfer = new DataTransfer();
        transfer.Add(DataTransferItem.CreateFile(storage));
        await DragDrop.DoDragDropAsync(e, transfer, DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link);
    }

    private void OnRemove(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button { Tag: string path })
        {
            var item = _viewModel.Items.FirstOrDefault(value => value.Path == path);
            if (item is not null)
                _viewModel.Items.Remove(item);
            _viewModel.Changed();
        }
    }

    private void OnClear(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _viewModel.Items.Clear();
        _viewModel.Changed();
    }
}
