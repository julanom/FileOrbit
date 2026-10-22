namespace FileOrbit.App.Services;

// Kept as a compatibility type for old layouts; FileOrbit has no donation endpoints.
public sealed record SponsorLink(string Id, string Name, string Url, string Icon, string Color);

public static class SponsorLinks
{
    public static IReadOnlyList<SponsorLink> For(string language) => [];
}
