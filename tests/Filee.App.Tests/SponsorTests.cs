using Filee.App.Services;
namespace Filee.App.Tests;
public class SponsorTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("ko")]
    [InlineData("ar")]
    public void FileOrbit_has_no_upstream_donation_links(string language) =>
        Assert.Empty(SponsorLinks.For(language));
}
