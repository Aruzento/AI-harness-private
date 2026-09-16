using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MyAgent.Desktop;

public sealed class DiffTextBlock
    : Border
{
    private static readonly Brush
        AddedBackground =
            new SolidColorBrush(
                Color.FromRgb(
                    215,
                    245,
                    222));

    private static readonly Brush
        AddedForeground =
            new SolidColorBrush(
                Color.FromRgb(
                    20,
                    110,
                    50));

    private static readonly Brush
        AddedBorder =
            new SolidColorBrush(
                Color.FromRgb(
                    55,
                    165,
                    80));

    private static readonly Brush
        RemovedBackground =
            new SolidColorBrush(
                Color.FromRgb(
                    255,
                    222,
                    222));

    private static readonly Brush
        RemovedForeground =
            new SolidColorBrush(
                Color.FromRgb(
                    165,
                    30,
                    30));

    private static readonly Brush
        RemovedBorder =
            new SolidColorBrush(
                Color.FromRgb(
                    205,
                    65,
                    65));

    private static readonly Brush
        HunkBackground =
            new SolidColorBrush(
                Color.FromRgb(
                    218,
                    225,
                    235));

    private static readonly Brush
        HunkForeground =
            new SolidColorBrush(
                Color.FromRgb(
                    55,
                    75,
                    110));

    private static readonly Brush
        MutedForeground =
            new SolidColorBrush(
                Color.FromRgb(
                    110,
                    110,
                    110));

    private readonly StackPanel
        _linesPanel;

    public static readonly DependencyProperty
        TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(DiffTextBlock),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions
                        .AffectsMeasure,
                    OnVisualPropertyChanged));

    public static readonly DependencyProperty
        IsDiffProperty =
            DependencyProperty.Register(
                nameof(IsDiff),
                typeof(bool),
                typeof(DiffTextBlock),
                new FrameworkPropertyMetadata(
                    false,
                    FrameworkPropertyMetadataOptions
                        .AffectsRender,
                    OnVisualPropertyChanged));

    public string Text
    {
        get =>
            (string?)GetValue(
                TextProperty)
            ?? string.Empty;

        set =>
            SetValue(
                TextProperty,
                value);
    }

    public bool IsDiff
    {
        get =>
            (bool)GetValue(
                IsDiffProperty);

        set =>
            SetValue(
                IsDiffProperty,
                value);
    }

    public DiffTextBlock()
    {
        Padding =
            new Thickness(
                8);

        Background =
            new SolidColorBrush(
                Color.FromRgb(
                    238,
                    238,
                    238));

        CornerRadius =
            new CornerRadius(
                4);

        var scrollViewer =
            new ScrollViewer
            {
                VerticalScrollBarVisibility =
                    ScrollBarVisibility.Auto,

                HorizontalScrollBarVisibility =
                    ScrollBarVisibility.Auto,

                MaxHeight =
                    360,

                CanContentScroll =
                    false
            };

        _linesPanel =
            new StackPanel();

        scrollViewer.Content =
            _linesPanel;

        Child =
            scrollViewer;
    }

    private static void OnVisualPropertyChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs eventArgs)
    {
        if (dependencyObject
            is DiffTextBlock control)
        {
            control.Rebuild();
        }
    }

    private void Rebuild()
    {
        _linesPanel.Children.Clear();

        string normalized =
            Text
                .Replace(
                    "\r\n",
                    "\n")
                .Replace(
                    '\r',
                    '\n');

        string[] lines =
            normalized.Split(
                '\n');

        foreach (string line in lines)
        {
            var textBlock =
                new TextBlock
                {
                    Text =
                        line,

                    FontFamily =
                        new FontFamily(
                            "Consolas"),

                    FontSize =
                        12,

                    TextWrapping =
                        TextWrapping.NoWrap
                };

            var row =
                new Border
                {
                    Padding =
                        new Thickness(
                            5,
                            2,
                            5,
                            2),

                    HorizontalAlignment =
                        HorizontalAlignment.Stretch,

                    Child =
                        textBlock
                };

            if (IsDiff)
            {
                ApplyDiffStyle(
                    line,
                    row,
                    textBlock);
            }

            _linesPanel.Children.Add(
                row);
        }
    }

    private static void ApplyDiffStyle(
        string line,
        Border row,
        TextBlock textBlock)
    {
        if (line.StartsWith(
                "+ ",
                StringComparison.Ordinal))
        {
            row.Background =
                AddedBackground;

            row.BorderBrush =
                AddedBorder;

            row.BorderThickness =
                new Thickness(
                    4,
                    0,
                    0,
                    0);

            textBlock.Foreground =
                AddedForeground;

            return;
        }

        if (line.StartsWith(
                "- ",
                StringComparison.Ordinal))
        {
            row.Background =
                RemovedBackground;

            row.BorderBrush =
                RemovedBorder;

            row.BorderThickness =
                new Thickness(
                    4,
                    0,
                    0,
                    0);

            textBlock.Foreground =
                RemovedForeground;

            return;
        }

        if (line.StartsWith(
                "@@",
                StringComparison.Ordinal))
        {
            row.Background =
                HunkBackground;

            textBlock.Foreground =
                HunkForeground;

            textBlock.FontWeight =
                FontWeights.SemiBold;

            return;
        }

        if (string.Equals(
                line,
                "...",
                StringComparison.Ordinal))
        {
            textBlock.Foreground =
                MutedForeground;

            textBlock.FontStyle =
                FontStyles.Italic;
        }
    }
}