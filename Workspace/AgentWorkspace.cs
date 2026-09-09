namespace MyAgent.Workspace;

public class AgentWorkspace
{
    private readonly string _rootPath;
    private readonly string _rootPathWithSeparator;

    public string RootPath =>
        _rootPath;

    public AgentWorkspace(string rootPath)
    {
        _rootPath =
            Path.GetFullPath(rootPath);

        _rootPathWithSeparator =
            _rootPath.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        Directory.CreateDirectory(
            _rootPath);
    }

    public string ResolvePath(
        string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException(
                "Absolute paths are not allowed.");
        }

        string fullPath =
            Path.GetFullPath(
                Path.Combine(
                    _rootPath,
                    relativePath));

        StringComparison comparison =
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

        bool isRoot =
            string.Equals(
                fullPath,
                _rootPath,
                comparison);

        bool isInsideRoot =
            fullPath.StartsWith(
                _rootPathWithSeparator,
                comparison);

        if (!isRoot && !isInsideRoot)
        {
            throw new InvalidOperationException(
                "Path is outside workspace.");
        }

        return fullPath;
    }
}