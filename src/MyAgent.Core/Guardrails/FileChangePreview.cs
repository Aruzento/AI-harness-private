namespace MyAgent.Guardrails;

public sealed class FileChangePreview
{
    public string Path { get; }

    public string? OldContent { get; }

    public string NewContent { get; }

    public bool IsNewFile =>
        OldContent is null;

    public FileChangePreview(
        string path,
        string? oldContent,
        string newContent)
    {
        if (string.IsNullOrWhiteSpace(
                path))
        {
            throw new ArgumentException(
                "File path cannot be empty.",
                nameof(path));
        }

        ArgumentNullException.ThrowIfNull(
            newContent);

        Path =
            path;

        OldContent =
            oldContent;

        NewContent =
            newContent;
    }
}