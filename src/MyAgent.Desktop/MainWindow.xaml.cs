using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using MyAgent.Agent;
using MyAgent.Configuration;
using MyAgent.Guardrails;
using MyAgent.Llm;
using MyAgent.Messages;
using MyAgent.Tools;
using MyAgent.Workspace;

using AgentCore = MyAgent.Agent.Agent;

namespace MyAgent.Desktop;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<ChatItem>
        _items =
            new();

    private readonly HttpClient _httpClient;
    private readonly string _systemPrompt;

    private AgentCore _agent;
    private HarnessOptions _options;

    private bool _cancellationShownInline;

    private CancellationTokenSource?
        _runCancellation;

    public MainWindow()
    {
        InitializeComponent();

        Title =
            $"AI Harness v{HarnessVersion.Current}";

        ConversationItemsControl.ItemsSource =
            _items;

        _options =
            HarnessOptions.Load();

        string systemPromptPath =
            Path.Combine(
                "Prompts",
                "system.md");

        if (!File.Exists(systemPromptPath))
        {
            throw new InvalidOperationException(
                $"Не найден system prompt: {systemPromptPath}");
        }

        _systemPrompt =
            File.ReadAllText(
                systemPromptPath);

        _httpClient =
            new HttpClient();

        _agent =
            CreateAgent(
                _options);

        UpdateStatus();
    }

    private AgentCore CreateAgent(
        HarnessOptions options)
    {
        string? apiKey =
            Environment.GetEnvironmentVariable(
                options.ApiKeyEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Не найдена переменная окружения "
                + $"{options.ApiKeyEnvironmentVariable}.");
        }

        ILlmClient llmClient =
            new OpenAiCompatibleLlmClient(
                _httpClient,
                apiKey,
                options.LlmEndpoint,
                options.Model);

        var workspace =
            new AgentWorkspace(
                options.WorkspacePath);

        var toolRegistry =
            new ToolRegistry();

        toolRegistry.Register(
            new ListFilesTool(
                workspace));

        toolRegistry.Register(
            new ReadFileTool(
                workspace));

        toolRegistry.Register(
            new WriteFileTool(
                workspace));

        toolRegistry.Register(
            new TerminalTool(
                workspace,
                options.TerminalTimeoutSeconds));

        var policy =
            new AgentPolicy(
                options.MaxSteps,
                options.MaxToolCalls);

        IToolApproval toolApproval =
            new DesktopToolApproval(
                RequestToolApprovalAsync);

        IAgentObserver observer =
            new DesktopAgentObserver(
                AddActivity);

        return new AgentCore(
            llmClient,
            toolRegistry,
            policy,
            toolApproval,
            observer,
            _systemPrompt);
    }

    private void UpdateStatus()
    {
        StatusTextBlock.Text =
            _options.Model
            + " · "
            + _options.WorkspacePath;
    }

    private void AddActivity(
        string text)
    {
        AddChatItem(
            new ActivityItem(
                text));
    }

    private void AddChatItem(
        ChatItem item)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(
                () => AddChatItem(
                    item));

            return;
        }

        _items.Add(
            item);

        Dispatcher.BeginInvoke(
            new Action(
                () =>
                    ConversationScrollViewer
                        .ScrollToEnd()));
    }

    private async Task<bool> RequestToolApprovalAsync(
        ToolCall toolCall,
        CancellationToken cancellationToken)
    {
        if (!Dispatcher.CheckAccess())
        {
            return await Dispatcher.Invoke(
                () =>
                    RequestToolApprovalAsync(
                        toolCall,
                        cancellationToken));
        }

        var item =
            new ApprovalItem(
                toolCall.Name,
                DescribeApproval(
                    toolCall));

        AddChatItem(
            item);

        try
        {
            return await item.WaitAsync(
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            item.Cancel();

            _cancellationShownInline =
                true;

            throw;
        }
    }

    private static string DescribeApproval(
        ToolCall toolCall)
    {
        if (toolCall.Name.Equals(
                "run_terminal",
                StringComparison.OrdinalIgnoreCase)
            &&
            toolCall.Arguments.ValueKind ==
                JsonValueKind.Object
            &&
            toolCall.Arguments.TryGetProperty(
                "command",
                out JsonElement commandElement)
            &&
            commandElement.ValueKind ==
                JsonValueKind.String)
        {
            return commandElement.GetString()
                ?? string.Empty;
        }

        return toolCall.Arguments
            .GetRawText();
    }

    private void SettingsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_runCancellation is not null)
        {
            return;
        }

        var settingsWindow =
            new SettingsWindow(
                _options)
            {
                Owner =
                    this
            };

        bool? result =
            settingsWindow.ShowDialog();

        if (result != true
            ||
            settingsWindow.SelectedOptions
                is null)
        {
            return;
        }

        HarnessOptions newOptions =
            settingsWindow.SelectedOptions;

        try
        {
            AgentCore newAgent =
                CreateAgent(
                    newOptions);

            HarnessOptions.Save(
                newOptions);

            _options =
                newOptions;

            _agent =
                newAgent;

            _items.Clear();

            AddActivity(
                "✓ Настройки применены. "
                + "Начата новая сессия.");

            UpdateStatus();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "Не удалось применить настройки",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void SendButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_runCancellation is not null)
        {
            return;
        }

        string input =
            InputTextBox.Text;

        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        InputTextBox.Clear();

        _cancellationShownInline =
            false;

        AddChatItem(
            new UserMessageItem(
                input));

        _runCancellation =
            new CancellationTokenSource();

        CancellationToken cancellationToken =
            _runCancellation.Token;

        SendButton.Visibility =
            Visibility.Collapsed;

        StopButton.Visibility =
            Visibility.Visible;

        StopButton.IsEnabled =
            true;

        SettingsButton.IsEnabled =
            false;

        try
        {
            LlmResponse response =
                await _agent.RunAsync(
                    input,
                    cancellationToken);

            AddChatItem(
                new AssistantMessageItem(
                    response.Content
                    ?? string.Empty));
        }
        catch (OperationCanceledException)
        {
            if (!_cancellationShownInline)
            {
                AddChatItem(
                    new ActivityItem(
                        "■ Выполнение остановлено"));
            }
        }
        catch (Exception exception)
        {
            AddChatItem(
                new ActivityItem(
                    "✗ Ошибка: "
                    + exception.Message));
        }
        finally
        {
            _runCancellation?.Dispose();

            _runCancellation =
                null;

            StopButton.Visibility =
                Visibility.Collapsed;

            SendButton.Visibility =
                Visibility.Visible;

            StopButton.IsEnabled =
                true;

            SettingsButton.IsEnabled =
                true;

            InputTextBox.Focus();
        }
    }

    private void ApproveTool_Click(
        object sender,
        RoutedEventArgs e)
    {
        ResolveApproval(
            sender,
            approved: true);
    }

    private void DenyTool_Click(
        object sender,
        RoutedEventArgs e)
    {
        ResolveApproval(
            sender,
            approved: false);
    }

    private static void ResolveApproval(
        object sender,
        bool approved)
    {
        if (sender is not Button button)
        {
            return;
        }

        if (button.DataContext
            is not ApprovalItem item)
        {
            return;
        }

        item.Resolve(
            approved);
    }

    private void StopButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        StopButton.IsEnabled =
            false;

        _runCancellation?.Cancel();
    }

    protected override void OnClosed(
        EventArgs e)
    {
        CancellationTokenSource?
            cancellation =
                _runCancellation;

        _runCancellation =
            null;

        cancellation?.Cancel();
        cancellation?.Dispose();

        _httpClient.Dispose();

        base.OnClosed(e);
    }
}