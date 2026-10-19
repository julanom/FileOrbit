// Real file-management actions used by FileOrbit's Actions wheel.

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;

namespace Filee.App.Services;

public enum FileActionKind
{
    AirDrop,
    Shelf,
    Copy,
    Move,
    CopyPath,
    Terminal,
}

public sealed class FileActionService(ShelfService shelf)
{
    private const string AirDropScript = """
        ObjC.import('AppKit');
        ObjC.import('Foundation');
        function run(argv) {
          const error = Ref();
          const text = $.NSString.stringWithContentsOfFileEncodingError(argv[0], $.NSUTF8StringEncoding, error).js;
          const paths = JSON.parse(text);
          const urls = paths.map(path => $.NSURL.fileURLWithPath(path));
          const service = $.NSSharingService.sharingServiceNamed('com.apple.share.AirDrop');
          if (!service) throw new Error('AirDrop is unavailable on this Mac.');
          service.performWithItems(urls);
        }
        """;

    public async Task ExecuteAsync(FileActionKind action, IReadOnlyList<string> paths, Window owner)
    {
        var existing = paths.Where(path => File.Exists(path) || Directory.Exists(path)).Distinct().ToArray();
        if (existing.Length == 0)
            throw new FileNotFoundException("The selected files no longer exist.");

        switch (action)
        {
            case FileActionKind.AirDrop:
                await Task.Run(() => AirDrop(existing));
                break;
            case FileActionKind.Shelf:
                shelf.Add(existing);
                shelf.Show();
                break;
            case FileActionKind.Copy:
            case FileActionKind.Move:
                var destination = await PickDestinationAsync(owner);
                if (destination is not null)
                    await TransferToFolderAsync(existing, destination, action == FileActionKind.Move);
                break;
            case FileActionKind.CopyPath:
                var clipboard = owner.Clipboard;
                if (clipboard is null)
                    throw new InvalidOperationException("The clipboard is unavailable.");
                await clipboard.SetTextAsync(string.Join(Environment.NewLine, existing));
                break;
            case FileActionKind.Terminal:
                await Task.Run(() => OpenInTerminal(existing));
                break;
        }
    }

    private static async Task<string?> PickDestinationAsync(Window owner)
    {
        var picked = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Choose a destination",
        });
        return picked.FirstOrDefault()?.TryGetLocalPath();
    }

    internal static async Task<IReadOnlyList<string>> TransferToFolderAsync(
        IReadOnlyList<string> sources, string destinationDirectory, bool move)
    {
        var destination = Path.GetFullPath(destinationDirectory);
        if (!Directory.Exists(destination))
            throw new DirectoryNotFoundException(destination);

        return await Task.Run<IReadOnlyList<string>>(() =>
        {
            var outputs = new List<string>();
            foreach (var sourcePath in sources)
            {
                var source = Path.GetFullPath(sourcePath);
                if (!File.Exists(source) && !Directory.Exists(source))
                    throw new FileNotFoundException("The selected item no longer exists.", source);
                if (Directory.Exists(source) && IsInside(destination, source))
                    throw new IOException($"A folder cannot be copied or moved inside itself: {source}");

                var target = AvailablePath(destination, Path.GetFileName(source));
                var partial = target + $".fileorbit-partial-{Guid.NewGuid():N}";
                try
                {
                    CopyItem(source, partial);
                    if (File.Exists(partial))
                        File.Move(partial, target);
                    else
                        Directory.Move(partial, target);

                    if (move)
                    {
                        if (File.Exists(source) || File.GetAttributes(source).HasFlag(FileAttributes.ReparsePoint))
                            File.Delete(source);
                        else
                            Directory.Delete(source, recursive: true);
                    }
                    outputs.Add(target);
                }
                catch
                {
                    DeletePartial(partial);
                    throw;
                }
            }
            return outputs;
        });
    }

    internal static string AvailablePath(string destinationDirectory, string name)
    {
        var candidate = Path.Combine(destinationDirectory, name);
        if (!File.Exists(candidate) && !Directory.Exists(candidate))
            return candidate;

        var extension = Path.GetExtension(name);
        var stem = extension.Length == 0 ? name : name[..^extension.Length];
        for (var number = 1; ; number++)
        {
            var suffix = number == 1 ? " copy" : $" copy {number}";
            candidate = Path.Combine(destinationDirectory, stem + suffix + extension);
            if (!File.Exists(candidate) && !Directory.Exists(candidate))
                return candidate;
        }
    }

    private static bool IsInside(string candidate, string parent)
    {
        var relative = Path.GetRelativePath(parent, candidate);
        return relative == "." || relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }

    private static void CopyItem(string source, string destination)
    {
        var attributes = File.GetAttributes(source);
        if (attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            var info = Directory.Exists(source) ? (FileSystemInfo)new DirectoryInfo(source) : new FileInfo(source);
            var target = info.LinkTarget ?? throw new IOException($"Could not read symbolic link: {source}");
            if (info is DirectoryInfo)
                Directory.CreateSymbolicLink(destination, target);
            else
                File.CreateSymbolicLink(destination, target);
            return;
        }

        if (File.Exists(source))
        {
            File.Copy(source, destination, overwrite: false);
            File.SetLastWriteTimeUtc(destination, File.GetLastWriteTimeUtc(source));
            return;
        }

        Directory.CreateDirectory(destination);
        foreach (var entry in Directory.EnumerateFileSystemEntries(source))
            CopyItem(entry, Path.Combine(destination, Path.GetFileName(entry)));
        Directory.SetLastWriteTimeUtc(destination, Directory.GetLastWriteTimeUtc(source));
    }

    private static void DeletePartial(string path)
    {
        try
        {
            if (File.Exists(path) || File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
                File.Delete(path);
            else if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FileNotFoundException)
        {
            // Best-effort cleanup of data created by this operation only.
        }
    }

    private static void AirDrop(IReadOnlyList<string> paths)
    {
        if (!OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException("AirDrop is only available on macOS.");

        var manifest = Path.Combine(Path.GetTempPath(), $"fileorbit-airdrop-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(manifest, JsonSerializer.Serialize(paths), new UTF8Encoding(false));
            Run("/usr/bin/osascript", ["-l", "JavaScript", "-e", AirDropScript, manifest]);
        }
        finally
        {
            try { File.Delete(manifest); }
            catch (IOException) { }
        }
    }

    private static void OpenInTerminal(IReadOnlyList<string> paths)
    {
        if (!OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException("Open in Terminal is currently available on macOS.");
        foreach (var directory in paths.Select(path => Directory.Exists(path) ? path : Path.GetDirectoryName(path))
                     .OfType<string>().Distinct(StringComparer.Ordinal))
            Run("/usr/bin/open", ["-a", "Terminal", directory]);
    }

    private static void Run(string executable, IReadOnlyList<string> arguments)
    {
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new IOException($"Could not start {executable}.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30_000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"{Path.GetFileName(executable)} did not respond.");
        }
        if (process.ExitCode != 0)
            throw new IOException(error.GetAwaiter().GetResult().Trim());
        _ = output.GetAwaiter().GetResult();
    }
}
