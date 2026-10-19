using Filee.App.Services;

namespace Filee.App.Tests;

public sealed class FileActionServiceTests
{
    [Fact]
    public async Task Copy_is_recursive_unicode_safe_and_collision_safe()
    {
        using var temp = new ActionTempDirectory();
        var source = Directory.CreateDirectory(Path.Combine(temp.Path, "source folder 🪐"));
        Directory.CreateDirectory(Path.Combine(source.FullName, "nested"));
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "nested", "résumé 你好.txt"), "orbit", TestContext.Current.CancellationToken);
        var destination = Directory.CreateDirectory(Path.Combine(temp.Path, "destination"));

        var first = await FileActionService.TransferToFolderAsync([source.FullName], destination.FullName, move: false);
        var second = await FileActionService.TransferToFolderAsync([source.FullName], destination.FullName, move: false);

        Assert.Equal("orbit", await File.ReadAllTextAsync(Path.Combine(first.Single(), "nested", "résumé 你好.txt"), TestContext.Current.CancellationToken));
        Assert.EndsWith("source folder 🪐 copy", second.Single());
        Assert.True(Directory.Exists(source.FullName));
    }

    [Fact]
    public async Task Move_removes_the_source_only_after_the_copy_is_complete()
    {
        using var temp = new ActionTempDirectory();
        var source = Path.Combine(temp.Path, "move me.txt");
        await File.WriteAllTextAsync(source, "safe", TestContext.Current.CancellationToken);
        var destination = Directory.CreateDirectory(Path.Combine(temp.Path, "destination"));

        var outputs = await FileActionService.TransferToFolderAsync([source], destination.FullName, move: true);

        Assert.False(File.Exists(source));
        Assert.Equal("safe", await File.ReadAllTextAsync(outputs.Single(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_folder_cannot_be_copied_into_itself()
    {
        using var temp = new ActionTempDirectory();
        var source = Directory.CreateDirectory(Path.Combine(temp.Path, "folder"));
        var inside = Directory.CreateDirectory(Path.Combine(source.FullName, "inside"));

        await Assert.ThrowsAsync<IOException>(() =>
            FileActionService.TransferToFolderAsync([source.FullName], inside.FullName, move: false));
    }

    private sealed class ActionTempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fileorbit-actions", Guid.NewGuid().ToString("N"));

        public ActionTempDirectory() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); }
            catch (IOException) { }
        }
    }
}
