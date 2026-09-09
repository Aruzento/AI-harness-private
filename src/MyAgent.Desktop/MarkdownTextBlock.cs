using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace MyAgent.Desktop;

public class MarkdownTextBlock
    : TextBlock
{
    public static readonly DependencyProperty
        MarkdownProperty =
            DependencyProperty.Register(
                nameof(Markdown),
                typeof(string),
                typeof(MarkdownTextBlock),
                new PropertyMetadata(
                    string.Empty,
                    OnMarkdownChanged));

    public string Markdown
    {
        get =>
            (string)GetValue(
                MarkdownProperty);

        set =>
            SetValue(
                MarkdownProperty,
                value);
    }

    private static void OnMarkdownChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject
            is MarkdownTextBlock textBlock)
        {
            textBlock.RenderMarkdown();
        }
    }

    private void RenderMarkdown()
    {
        Inlines.Clear();

        string markdown =
            Markdown
            ?? string.Empty;

        string normalized =
            markdown
                .Replace(
                    "\r\n",
                    "\n")
                .Replace(
                    '\r',
                    '\n');

        string[] lines =
            normalized.Split(
                '\n');

        bool inCodeBlock =
            false;

        for (int index = 0;
             index < lines.Length;
             index++)
        {
            string line =
                lines[index];

            string trimmed =
                line.TrimStart();

            if (trimmed.StartsWith(
                    "```",
                    StringComparison.Ordinal))
            {
                inCodeBlock =
                    !inCodeBlock;

                continue;
            }

            if (inCodeBlock)
            {
                AddCodeLine(
                    line);
            }
            else
            {
                AddMarkdownLine(
                    line);
            }

            if (index <
                lines.Length - 1)
            {
                Inlines.Add(
                    new LineBreak());
            }
        }
    }

    private void AddMarkdownLine(
        string line)
    {
        string trimmed =
            line.TrimStart();

        if (trimmed.StartsWith(
                "- ",
                StringComparison.Ordinal)
            ||
            trimmed.StartsWith(
                "* ",
                StringComparison.Ordinal))
        {
            Inlines.Add(
                new Run(
                    "• "));

            AddInlineMarkdown(
                trimmed[2..]);

            return;
        }

        int headingLevel =
            GetHeadingLevel(
                trimmed);

        if (headingLevel > 0)
        {
            string headingText =
                trimmed[
                    (headingLevel + 1)..];

            var run =
                new Run(
                    headingText)
                {
                    FontWeight =
                        FontWeights.SemiBold,

                    FontSize =
                        FontSize
                        + Math.Max(
                            1,
                            5 - headingLevel)
                };

            Inlines.Add(
                run);

            return;
        }

        AddInlineMarkdown(
            line);
    }

    private void AddInlineMarkdown(
        string text)
    {
        int position =
            0;

        while (position <
               text.Length)
        {
            int boldStart =
                text.IndexOf(
                    "**",
                    position,
                    StringComparison.Ordinal);

            int codeStart =
                text.IndexOf(
                    '`',
                    position);

            int markerStart =
                FindFirstMarker(
                    boldStart,
                    codeStart);

            if (markerStart < 0)
            {
                Inlines.Add(
                    new Run(
                        text[position..]));

                break;
            }

            if (markerStart >
                position)
            {
                Inlines.Add(
                    new Run(
                        text[
                            position
                            ..markerStart]));
            }

            if (markerStart ==
                boldStart)
            {
                int boldEnd =
                    text.IndexOf(
                        "**",
                        boldStart + 2,
                        StringComparison.Ordinal);

                if (boldEnd < 0)
                {
                    Inlines.Add(
                        new Run(
                            text[boldStart..]));

                    break;
                }

                string boldText =
                    text[
                        (boldStart + 2)
                        ..boldEnd];

                Inlines.Add(
                    new Run(
                        boldText)
                    {
                        FontWeight =
                            FontWeights.Bold
                    });

                position =
                    boldEnd + 2;

                continue;
            }

            int codeEnd =
                text.IndexOf(
                    '`',
                    codeStart + 1);

            if (codeEnd < 0)
            {
                Inlines.Add(
                    new Run(
                        text[codeStart..]));

                break;
            }

            string codeText =
                text[
                    (codeStart + 1)
                    ..codeEnd];

            Inlines.Add(
                CreateCodeRun(
                    codeText));

            position =
                codeEnd + 1;
        }
    }

    private void AddCodeLine(
        string text)
    {
        Inlines.Add(
            CreateCodeRun(
                text));
    }

    private static Run CreateCodeRun(
        string text)
    {
        return new Run(
            text)
        {
            FontFamily =
                new FontFamily(
                    "Consolas"),

            Background =
                new SolidColorBrush(
                    Color.FromRgb(
                        242,
                        242,
                        242))
        };
    }

    private static int GetHeadingLevel(
        string text)
    {
        int level =
            0;

        while (level <
               text.Length
               &&
               level < 6
               &&
               text[level] == '#')
        {
            level++;
        }

        if (level == 0
            ||
            level >= text.Length
            ||
            text[level] != ' ')
        {
            return 0;
        }

        return level;
    }

    private static int FindFirstMarker(
        int first,
        int second)
    {
        if (first < 0)
        {
            return second;
        }

        if (second < 0)
        {
            return first;
        }

        return Math.Min(
            first,
            second);
    }
}