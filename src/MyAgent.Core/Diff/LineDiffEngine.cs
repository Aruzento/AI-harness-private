namespace MyAgent.Diff;

public static class LineDiffEngine
{
    private const long MaxMatrixCells =
        4_000_000;

    public static IReadOnlyList<LineDiffLine>
        Create(
            string oldContent,
            string newContent)
    {
        ArgumentNullException.ThrowIfNull(
            oldContent);

        ArgumentNullException.ThrowIfNull(
            newContent);

        string[] oldLines =
            SplitLines(
                oldContent);

        string[] newLines =
            SplitLines(
                newContent);

        if (oldLines.SequenceEqual(
                newLines))
        {
            return BuildUnchanged(
                oldLines);
        }

        long matrixCells =
            (long)(oldLines.Length + 1)
            *
            (newLines.Length + 1);

        if (matrixCells >
            MaxMatrixCells)
        {
            return BuildPrefixSuffixDiff(
                oldLines,
                newLines);
        }

        return BuildLcsDiff(
            oldLines,
            newLines);
    }

    private static IReadOnlyList<LineDiffLine>
        BuildLcsDiff(
            string[] oldLines,
            string[] newLines)
    {
        int oldCount =
            oldLines.Length;

        int newCount =
            newLines.Length;

        var lcs =
            new int[
                oldCount + 1,
                newCount + 1];

        for (int oldIndex =
                oldCount - 1;
            oldIndex >= 0;
            oldIndex--)
        {
            for (int newIndex =
                    newCount - 1;
                newIndex >= 0;
                newIndex--)
            {
                if (string.Equals(
                        oldLines[oldIndex],
                        newLines[newIndex],
                        StringComparison.Ordinal))
                {
                    lcs[
                        oldIndex,
                        newIndex] =
                            lcs[
                                oldIndex + 1,
                                newIndex + 1]
                            + 1;
                }
                else
                {
                    lcs[
                        oldIndex,
                        newIndex] =
                            Math.Max(
                                lcs[
                                    oldIndex + 1,
                                    newIndex],
                                lcs[
                                    oldIndex,
                                    newIndex + 1]);
                }
            }
        }

        var result =
            new List<LineDiffLine>();

        int oldPosition = 0;
        int newPosition = 0;

        while (oldPosition <
                   oldCount
               &&
               newPosition <
                   newCount)
        {
            if (string.Equals(
                    oldLines[oldPosition],
                    newLines[newPosition],
                    StringComparison.Ordinal))
            {
                result.Add(
                    new LineDiffLine(
                        LineDiffKind.Context,
                        oldLines[oldPosition],
                        oldPosition + 1,
                        newPosition + 1));

                oldPosition++;
                newPosition++;

                continue;
            }

            if (lcs[
                    oldPosition + 1,
                    newPosition]
                >=
                lcs[
                    oldPosition,
                    newPosition + 1])
            {
                result.Add(
                    new LineDiffLine(
                        LineDiffKind.Removed,
                        oldLines[oldPosition],
                        oldPosition + 1,
                        null));

                oldPosition++;
            }
            else
            {
                result.Add(
                    new LineDiffLine(
                        LineDiffKind.Added,
                        newLines[newPosition],
                        null,
                        newPosition + 1));

                newPosition++;
            }
        }

        while (oldPosition <
               oldCount)
        {
            result.Add(
                new LineDiffLine(
                    LineDiffKind.Removed,
                    oldLines[oldPosition],
                    oldPosition + 1,
                    null));

            oldPosition++;
        }

        while (newPosition <
               newCount)
        {
            result.Add(
                new LineDiffLine(
                    LineDiffKind.Added,
                    newLines[newPosition],
                    null,
                    newPosition + 1));

            newPosition++;
        }

        return result;
    }

    private static IReadOnlyList<LineDiffLine>
        BuildPrefixSuffixDiff(
            string[] oldLines,
            string[] newLines)
    {
        var result =
            new List<LineDiffLine>();

        int commonPrefixLength = 0;

        int maximumPrefixLength =
            Math.Min(
                oldLines.Length,
                newLines.Length);

        while (commonPrefixLength <
                   maximumPrefixLength
               &&
               string.Equals(
                   oldLines[
                       commonPrefixLength],
                   newLines[
                       commonPrefixLength],
                   StringComparison.Ordinal))
        {
            result.Add(
                new LineDiffLine(
                    LineDiffKind.Context,
                    oldLines[
                        commonPrefixLength],
                    commonPrefixLength + 1,
                    commonPrefixLength + 1));

            commonPrefixLength++;
        }

        int oldSuffixIndex =
            oldLines.Length - 1;

        int newSuffixIndex =
            newLines.Length - 1;

        while (oldSuffixIndex >=
                   commonPrefixLength
               &&
               newSuffixIndex >=
                   commonPrefixLength
               &&
               string.Equals(
                   oldLines[
                       oldSuffixIndex],
                   newLines[
                       newSuffixIndex],
                   StringComparison.Ordinal))
        {
            oldSuffixIndex--;
            newSuffixIndex--;
        }

        for (int oldIndex =
                commonPrefixLength;
            oldIndex <=
                oldSuffixIndex;
            oldIndex++)
        {
            result.Add(
                new LineDiffLine(
                    LineDiffKind.Removed,
                    oldLines[
                        oldIndex],
                    oldIndex + 1,
                    null));
        }

        for (int newIndex =
                commonPrefixLength;
            newIndex <=
                newSuffixIndex;
            newIndex++)
        {
            result.Add(
                new LineDiffLine(
                    LineDiffKind.Added,
                    newLines[
                        newIndex],
                    null,
                    newIndex + 1));
        }

        int oldSuffixStart =
            oldSuffixIndex + 1;

        int newSuffixStart =
            newSuffixIndex + 1;

        int suffixLength =
            oldLines.Length
            - oldSuffixStart;

        for (int offset = 0;
            offset < suffixLength;
            offset++)
        {
            int oldIndex =
                oldSuffixStart
                + offset;

            int newIndex =
                newSuffixStart
                + offset;

            result.Add(
                new LineDiffLine(
                    LineDiffKind.Context,
                    oldLines[
                        oldIndex],
                    oldIndex + 1,
                    newIndex + 1));
        }

        return result;
    }

    private static IReadOnlyList<LineDiffLine>
        BuildUnchanged(
            string[] lines)
    {
        var result =
            new List<LineDiffLine>(
                lines.Length);

        for (int index = 0;
            index < lines.Length;
            index++)
        {
            result.Add(
                new LineDiffLine(
                    LineDiffKind.Context,
                    lines[index],
                    index + 1,
                    index + 1));
        }

        return result;
    }

    private static string[] SplitLines(
        string text)
    {
        if (text.Length == 0)
        {
            return Array.Empty<string>();
        }

        string normalized =
            text
                .Replace(
                    "\r\n",
                    "\n")
                .Replace(
                    '\r',
                    '\n');

        string[] lines =
            normalized.Split(
                '\n');

        if (normalized.EndsWith(
                '\n'))
        {
            return lines[
                ..^1];
        }

        return lines;
    }
}