namespace MyAgent.Diff;

public sealed class LineDiffLine
{
    public LineDiffKind Kind { get; }

    public string Text { get; }

    public int? OldLineNumber { get; }

    public int? NewLineNumber { get; }

    public LineDiffLine(
        LineDiffKind kind,
        string text,
        int? oldLineNumber,
        int? newLineNumber)
    {
        if (oldLineNumber is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(oldLineNumber));
        }

        if (newLineNumber is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(newLineNumber));
        }

        Kind =
            kind;

        Text =
            text
            ?? string.Empty;

        OldLineNumber =
            oldLineNumber;

        NewLineNumber =
            newLineNumber;
    }
}