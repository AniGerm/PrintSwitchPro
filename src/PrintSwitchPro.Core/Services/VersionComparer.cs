namespace PrintSwitchPro.Core.Services;

public static class VersionComparer
{
    /// <summary>True when <paramref name="remote"/> is strictly newer than <paramref name="local"/>.</summary>
    public static bool IsNewer(string remote, string local)
    {
        var a = Parse(remote);
        var b = Parse(local);
        var len = Math.Max(a.Length, b.Length);
        for (var i = 0; i < len; i++)
        {
            var x = i < a.Length ? a[i] : 0;
            var y = i < b.Length ? b[i] : 0;
            if (x > y)
            {
                return true;
            }

            if (x < y)
            {
                return false;
            }
        }

        return false;
    }

    public static string Normalize(string version)
        => version.Trim().TrimStart('v', 'V');

    private static int[] Parse(string version)
        => Normalize(version)
            .Split(['.', '+', '-'], StringSplitOptions.RemoveEmptyEntries)
            .Select(part => int.TryParse(part, out var n) ? n : 0)
            .ToArray();
}
