namespace MyAgent.Diff;

public static class LineDiffTextFormatter
{
    public static string Format(
        IReadOnlyList<LineDiffLine> diff,
        int contextLines = 2)
    {
        ArgumentNullException.ThrowIfNull(
            diff);

        if (contextLines < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(contextLines));
        }

        if (diff.Count == 0)
        {
            return "(нет изменений)";
        }

        bool hasChanges =
            diff.Any(
                line =>
                    line.Kind !=
                    LineDiffKind.Context);

        if (!hasChanges)
        {
            return "(нет изменений)";
        }

        var visible =
            new bool[diff.Count];

        for (int index = 0;
            index < diff.Count;
            index++)
        {
            if (diff[index].Kind ==
                LineDiffKind.Context)
            {
                continue;
            }

            int start =
                Math.Max(
                    0,
                    index - contextLines);

            int end =
                Math.Min(
                    diff.Count - 1,
                    index + contextLines);

            for (int visibleIndex = start;
                visibleIndex <= end;
                visibleIndex++)
            {
                visible[visibleIndex] =
                    true;
            }
        }

        var output =
            new List<string>();

        bool insideGroup =
            false;

        for (int index = 0;
            index < diff.Count;
            index++)
        {
            if (!visible[index])
            {
                insideGroup =
                    false;

                continue;
            }

            if (!insideGroup)
            {
                if (output.Count > 0)
                {
                    output.Add(
                        "...");
                }

                output.Add(
                    "@@");

                insideGroup =
                    true;
            }

            LineDiffLine line =
                diff[index];

            string prefix =
                line.Kind switch
                {
                    LineDiffKind.Removed =>
                        "- ",

                    LineDiffKind.Added =>
                        "+ ",

                    _ =>
                        "  "
                };

            output.Add(
                prefix
                + line.Text);
        }

        return string.Join(
            Environment.NewLine,
            output);
    }
}