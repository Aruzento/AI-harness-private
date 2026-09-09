using System.Windows;
using MyAgent.Configuration;

namespace MyAgent.Desktop;

public partial class SettingsWindow
    : Window
{
    public HarnessOptions?
        SelectedOptions
    {
        get;
        private set;
    }

    public SettingsWindow(
        HarnessOptions options)
    {
        InitializeComponent();

        EndpointTextBox.Text =
            options.LlmEndpoint;

        ModelTextBox.Text =
            options.Model;

        ApiKeyEnvironmentTextBox.Text =
            options.ApiKeyEnvironmentVariable;

        WorkspaceTextBox.Text =
            options.WorkspacePath;

        MaxStepsTextBox.Text =
            options.MaxSteps.ToString();

        MaxToolCallsTextBox.Text =
            options.MaxToolCalls.ToString();

        TerminalTimeoutTextBox.Text =
            options.TerminalTimeoutSeconds
                .ToString();
    }

    private void SaveButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!TryReadPositiveInt(
                MaxStepsTextBox.Text,
                "Max steps",
                out int maxSteps))
        {
            return;
        }

        if (!TryReadPositiveInt(
                MaxToolCallsTextBox.Text,
                "Max tool calls",
                out int maxToolCalls))
        {
            return;
        }

        if (!TryReadPositiveInt(
                TerminalTimeoutTextBox.Text,
                "Terminal timeout",
                out int terminalTimeout))
        {
            return;
        }

        try
        {
            SelectedOptions =
                new HarnessOptions(
                    llmEndpoint:
                        EndpointTextBox.Text.Trim(),

                    model:
                        ModelTextBox.Text.Trim(),

                    apiKeyEnvironmentVariable:
                        ApiKeyEnvironmentTextBox
                            .Text
                            .Trim(),

                    workspacePath:
                        WorkspaceTextBox.Text.Trim(),

                    maxSteps:
                        maxSteps,

                    maxToolCalls:
                        maxToolCalls,

                    terminalTimeoutSeconds:
                        terminalTimeout);

            DialogResult =
                true;
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "Некорректные настройки",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult =
            false;
    }

    private bool TryReadPositiveInt(
        string text,
        string name,
        out int value)
    {
        if (int.TryParse(
                text,
                out value)
            &&
            value > 0)
        {
            return true;
        }

        MessageBox.Show(
            this,
            $"{name} должен быть положительным целым числом.",
            "Некорректное значение",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

        return false;
    }
}