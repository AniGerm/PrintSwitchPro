using PrintSwitchPro.Core.Services;

namespace PrintSwitchPro.Core.Tests;

public class VersionComparerTests
{
    [Theory]
    [InlineData("3.0.1", "3.0.0", true)]
    [InlineData("v3.1.0", "3.0.9", true)]
    [InlineData("3.0.0", "3.0.0", false)]
    [InlineData("2.9.9", "3.0.0", false)]
    [InlineData("3.0.0", "v3.0.0", false)]
    public void IsNewer_Works(string remote, string local, bool expected)
        => Assert.Equal(expected, VersionComparer.IsNewer(remote, local));
}
