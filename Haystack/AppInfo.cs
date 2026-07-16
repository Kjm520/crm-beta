using System.Reflection;

namespace Haystack;

/// <summary>
/// Version facts about the running build. The build number is the repo's commit
/// count, baked into the assembly at build time by the EmbedBuildNumber target
/// in Haystack.csproj — embedded rather than read from a file beside the exe,
/// because the published app is a single self-contained executable with
/// deliberately no sibling files.
/// </summary>
public static class AppInfo
{
    /// <summary>The product version (csproj AssemblyVersion), e.g. "0.1.0".</summary>
    public static string Version { get; } =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

    /// <summary>Commit count at build time; "dev" when built without git.</summary>
    public static string Build { get; } =
        Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "BuildNumber")?.Value is { Length: > 0 } count && count != "0"
                ? count
                : "dev";
}
