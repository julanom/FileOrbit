using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace FileOrbit.Platform.MacOS;

/// <summary>Opens macOS's native AirDrop sharing service from FileOrbit's own AppKit process.</summary>
[SupportedOSPlatform("macos")]
public static class AirDrop
{
    private const string ObjC = "/usr/lib/libobjc.A.dylib";
    private static readonly nint AppKit = NativeLibrary.Load("/System/Library/Frameworks/AppKit.framework/AppKit");

    public static bool IsAvailable
    {
        get
        {
            _ = AppKit;
            using var pool = new AutoreleasePool();
            return Service() != 0;
        }
    }

    public static void Send(IReadOnlyList<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        if (paths.Count == 0)
            throw new ArgumentException("At least one file is required.", nameof(paths));

        _ = AppKit;
        using var pool = new AutoreleasePool();
        var items = Send(objc_getClass("NSMutableArray"), sel_registerName("arrayWithCapacity:"), paths.Count);
        foreach (var path in paths)
        {
            var value = NSString(Path.GetFullPath(path));
            var url = Send(objc_getClass("NSURL"), sel_registerName("fileURLWithPath:"), value);
            SendVoid(items, sel_registerName("addObject:"), url);
        }

        var service = Service();
        if (service == 0)
            throw new PlatformNotSupportedException("AirDrop is unavailable on this Mac.");

        var application = Send(objc_getClass("NSApplication"), sel_registerName("sharedApplication"));
        SendVoid(application, sel_registerName("activateIgnoringOtherApps:"), 1);
        SendVoid(service, sel_registerName("performWithItems:"), items);
    }

    private static nint Service() =>
        Send(objc_getClass("NSSharingService"), sel_registerName("sharingServiceNamed:"),
            NSString("com.apple.share.AirDrop.send"));

    private static nint NSString(string value)
    {
        var utf8 = Marshal.StringToCoTaskMemUTF8(value);
        try { return Send(objc_getClass("NSString"), sel_registerName("stringWithUTF8String:"), utf8); }
        finally { Marshal.FreeCoTaskMem(utf8); }
    }

    private readonly struct AutoreleasePool : IDisposable
    {
        private readonly nint _pool = Send(Send(objc_getClass("NSAutoreleasePool"), sel_registerName("alloc")), sel_registerName("init"));
        public AutoreleasePool() { }
        public void Dispose() => SendVoid(_pool, sel_registerName("drain"));
    }

    [DllImport(ObjC)]
    private static extern nint objc_getClass([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjC)]
    private static extern nint sel_registerName([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern nint Send(nint receiver, nint selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern nint Send(nint receiver, nint selector, nint argument);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void SendVoid(nint receiver, nint selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void SendVoid(nint receiver, nint selector, nint argument);
}
