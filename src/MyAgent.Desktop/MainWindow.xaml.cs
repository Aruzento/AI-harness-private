using System.IO;
using System.Net.Http;
using System.Windows;
using MyAgent.Agent;
using MyAgent.Configuration;
using MyAgent.Guardrails;
using MyAgent.Llm;
using MyAgent.Tools;
using MyAgent.Workspace;
using System.Collections.ObjectModel;

using AgentCore = MyAgent.Agent.Agent;

namespace MyAgent.Desktop;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<ChatItem>
        _items =
            new();

    private readonly HttpClient _httpClient;
    private readonly AgentCore _agent;

    public MainWindow()
    {
        InitializeComponent();

        ConversationItemsControl.ItemsSource =
            _items;

        string? apiKey =
            Environment.GetEnvironmentVariable(
                "GROQ_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Не найдена переменная окружения GROQ_API_KEY.");
        }

        HarnessOptions options =
            HarnessOptions.FromEnvironment();

        string systemPromptPath =
            Path.Combine(
                "Prompts",
                "system.md");

        if (!File.Exists(systemPromptPath))
        {
            throw new InvalidOperationException(
                $"Не найден system prompt: {systemPromptPath}");
        }

        string systemPrompt =
            File.ReadAllText(
                systemPromptPath);

        _httpClient =
            new HttpClient();

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
            new DesktopToolApproval();

        IAgentObserver observer =
            new DesktopAgentObserver(
                AddActivity);

        _agent =
            new AgentCore(
                llmClient,
                toolRegistry,
                policy,
                toolApproval,
                observer,
                systemPrompt);
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

    private async void SendButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        string input =
            InputTextBox.Text;

        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        InputTextBox.Clear();

        AddChatItem(
            new UserMessageItem(
                input));

        SendButton.IsEnabled =
            false;

        try
        {
            LlmResponse response =
                await _agent.RunAsync(
                    input);

            AddChatItem(
                new AssistantMessageItem(
                    response.Content
                    ?? string.Empty));
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
            SendButton.IsEnabled =
                true;

            InputTextBox.Focus();
        }
    }

    protected override void OnClosed(
        EventArgs e)
    {
        _httpClient.Dispose();

        base.OnClosed(e);
    }
}