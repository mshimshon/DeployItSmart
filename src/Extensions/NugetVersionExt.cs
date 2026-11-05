using NuGet.Versioning;
using System.Text.RegularExpressions;

public static class NuGetVersionExt
{
    public static int GetPrereleaseNumberOrCrash(this NuGetVersion version)
    {
        if (!version.IsPrerelease)
        {
            throw new FormatException($"Version '{version}' is a stable release and does not contain pre-release numbers.");
        }
        int current = int.Parse(version.ReleaseLabels.ElementAt(1));
        return current;
    }

    public static NuGetVersion IncrementPrelease(this NuGetVersion version)
    {
        if (!version.IsPrerelease)
        {
            throw new FormatException($"Version '{version}' is a stable release and does not contain pre-release numbers.");
        }
        var releaseLabels = new[] { version.ReleaseLabels.ElementAt(0), (version.GetPrereleaseNumberOrCrash() + 1).ToString() };
        NuGetVersion nextVersion = new(version.Major, version.Minor, version.Patch, releaseLabels!, null);
        return nextVersion;
    }

}
