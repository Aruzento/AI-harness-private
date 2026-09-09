using System.Reflection;

namespace MyAgent.Configuration;

public static class HarnessVersion
{
    public static string Current
    {
        get
        {
            Assembly assembly =
                typeof(HarnessVersion)
                    .Assembly;

            string? version =
                assembly
                    .GetCustomAttribute<
                        AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion;

            if (string.IsNullOrWhiteSpace(
                    version))
            {
                return assembly
                    .GetName()
                    .Version
                    ?.ToString()
                    ?? "unknown";
            }

            int metadataSeparator =
                version.IndexOf(
                    '+');

            if (metadataSeparator >= 0)
            {
                version =
                    version[
                        ..metadataSeparator];
            }

            return version;
        }
    }
}