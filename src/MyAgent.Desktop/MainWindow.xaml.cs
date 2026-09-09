using System.IO;
using System.Net.Http;
using System.Windows;
using MyAgent.Agent;
using MyAgent.Configuration;
using MyAgent.Guardrails;
using MyAgent.Llm;
using MyAgent.Tools;
using MyAgent.Workspace;

using AgentCore = MyAgent.Agent.Agent;

namespace MyAgent.Desktop;

public partial class MainWindow : Window
{
    private readonly HttpClient _httpClient;
    private readonly AgentCore _agent;

    public MainWindow()
    {
        InitializeComponent();

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
            new DesktopAgentObserver();

        _agent =
            new AgentCore(
                llmClient,
                toolRegistry,
                policy,
                toolApproval,
                observer,
                systemPrompt);
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

        ConversationTextBox.AppendText(
            $"Вы:{Environment.NewLine}"
            + input
            + Environment.NewLine
            + Environment.NewLine);

        SendButton.IsEnabled =
            false;

        try
        {
            LlmResponse response =
                await _agent.RunAsync(
                    input);

            ConversationTextBox.AppendText(
                $"AI:{Environment.NewLine}"
                + response.Content
                + Environment.NewLine
                + Environment.NewLine);
        }
        catch (Exception exception)
        {
            ConversationTextBox.AppendText(
                $"Ошибка:{Environment.NewLine}"
                + exception.Message
                + Environment.NewLine
                + Environment.NewLine);
        }
        finally
        {
            SendButton.IsEnabled =
                true;

            ConversationTextBox.ScrollToEnd();

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