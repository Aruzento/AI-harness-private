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
using System.Windows.Input;
using MyAgent.Secrets;

using AgentCore = MyAgent.Agent.Agent;

namespace MyAgent.Desktop;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<ChatItem>
        _items =
            new();

    private readonly HttpClient _httpClient;
    private readonly string _systemPrompt;

    private AgentCore? _agent;
    private HarnessOptions _options;

    private readonly LlmProfileBootstrapper
        _llmProfileBootstrapper;

    private readonly LlmProfileManager
        _llmProfileManager;

    private readonly ILlmClientFactory
        _llmClientFactory;

    private LlmProfileCatalog _llmCatalog =
        new();

    private LlmProfile? _activeProfile;

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

    var profileStore =
        new LlmProfileStore();

    ISecretStore secretStore =
        new DpapiSecretStore();

    var secretResolver =
        new LlmSecretResolver(
            secretStore);

    _llmProfileBootstrapper =
        new LlmProfileBootstrapper(
            profileStore,
            secretStore);

    _llmProfileManager =
        new LlmProfileManager(
            profileStore,
            secretStore);

    _llmClientFactory =
        new LlmClientFactory(
            _httpClient,
            secretResolver);

    SetReadyState(
        ready: false);

    StatusTextBlock.Text =
        "Инициализация...";

    Loaded +=
        MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        Loaded -=
            MainWindow_Loaded;

        try
        {
            _llmCatalog =
                await _llmProfileBootstrapper
                    .EnsureInitializedAsync(
                        _options);

            _activeProfile =
                GetActiveProfile(
                    _llmCatalog);

            _agent =
                await CreateAgentAsync(
                    _options,
                    _activeProfile);

            UpdateStatus();

            SetReadyState(
                ready: true);

            InputTextBox.Focus();
        }
        catch (Exception exception)
        {
            StatusTextBlock.Text =
                "Модель не настроена";

            InputTextBox.IsEnabled =
                false;

            SendButton.IsEnabled =
                false;

            NewChatButton.IsEnabled =
                false;

            SettingsButton.IsEnabled =
                true;

            MessageBox.Show(
                this,
                exception.Message
                + Environment.NewLine
                + Environment.NewLine
                + "Откройте настройки и добавьте "
                + "или выберите рабочую модель.",
                "Не удалось инициализировать AI Harness",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task<AgentCore> CreateAgentAsync(
        HarnessOptions options,
        LlmProfile profile,
        CancellationToken cancellationToken = default)
    {
        ILlmClient llmClient =
            await _llmClientFactory.CreateAsync(
                profile,
                cancellationToken);

        var workspace =
            new AgentWorkspace(
                options.WorkspacePath);

        var toolRegistry =
            new ToolRegistry();

        toolRegistry.Register(
            new ListFilesTool(
                workspace));

        toolRegistry.Register(
            new SearchFilesTool(
                workspace));

        toolRegistry.Register(
            new ReadFileTool(
                workspace));

        toolRegistry.Register(
            new EditFileTool(
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

    private static LlmProfile GetActiveProfile(
        LlmProfileCatalog catalog)
    {
        if (string.IsNullOrWhiteSpace(
                catalog.ActiveProfileId))
        {
            throw new InvalidOperationException(
                "Активная LLM-модель не выбрана.");
        }

        LlmProfile? profile =
            catalog.Profiles
                .FirstOrDefault(
                    candidate =>
                        string.Equals(
                            candidate.Id,
                            catalog.ActiveProfileId,
                            StringComparison.Ordinal));

        return profile
            ?? throw new InvalidOperationException(
                "Активный LLM-профиль не найден: "
                + catalog.ActiveProfileId);
    }

    private void SetReadyState(
        bool ready)
    {
        InputTextBox.IsEnabled =
            ready;

        SendButton.IsEnabled =
            ready;

        SettingsButton.IsEnabled =
            ready;

        NewChatButton.IsEnabled =
            ready;
    }

    private void UpdateStatus()
    {
        if (_activeProfile is null)
        {
            StatusTextBlock.Text =
                "Модель не выбрана";

            return;
        }

        StatusTextBlock.Text =
            _activeProfile.Name
            + " · "
            + _activeProfile.Model
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

    private async void SettingsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_runCancellation is not null)
        {
            return;
        }

        string? previousActiveProfileId =
            _llmCatalog.ActiveProfileId;

        LlmProfile? previousActiveProfile =
            _llmCatalog.Profiles
                .FirstOrDefault(
                    profile =>
                        string.Equals(
                            profile.Id,
                            previousActiveProfileId,
                            StringComparison.Ordinal));

        var settingsWindow =
            new SettingsWindow(
                _options,
                _llmProfileManager,
                _llmCatalog)
            {
                Owner =
                    this
            };

        bool? result =
            settingsWindow.ShowDialog();

        LlmProfileCatalog newCatalog =
            settingsWindow.ProfileCatalog;

        LlmProfile? newCatalogActiveProfile  =
            newCatalog.Profiles
                .FirstOrDefault(
                    profile =>
                        string.Equals(
                            profile.Id,
                            newCatalog.ActiveProfileId,
                            StringComparison.Ordinal));

        bool activeProfileChanged =
            !string.Equals(
                previousActiveProfileId,
                newCatalog.ActiveProfileId,
                StringComparison.Ordinal);

        bool activeProfileConfigurationChanged =
            !AreProfilesEquivalent(
                previousActiveProfile,
                newCatalogActiveProfile);

        bool generalSettingsChanged =
            result == true
            &&
            settingsWindow.SelectedOptions
                is not null;

        _llmCatalog =
            newCatalog;

        if (!activeProfileChanged
            &&
            !activeProfileConfigurationChanged
            &&
            !generalSettingsChanged)
        {
            return;
        }

        HarnessOptions newOptions =
            generalSettingsChanged
                ? settingsWindow.SelectedOptions!
                : _options;

        try
        {
            LlmProfile newActiveProfile =
                GetActiveProfile(
                    newCatalog);

            AgentCore newAgent =
                await CreateAgentAsync(
                    newOptions,
                    newActiveProfile);

            if (generalSettingsChanged)
            {
                HarnessOptions.Save(
                    newOptions);
            }

            _options =
                newOptions;

            _llmCatalog =
                newCatalog;

            _activeProfile =
                newActiveProfile;

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
            if (activeProfileChanged
                &&
                !string.IsNullOrWhiteSpace(
                    previousActiveProfileId))
            {
                try
                {
                    _llmCatalog =
                        await _llmProfileManager
                            .SetActiveAsync(
                                previousActiveProfileId);
                }
                catch
                {
                    // Preserve the original failure.
                }
            }

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
        await SendCurrentInputAsync();
    }

    private async void InputTextBox_PreviewKeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        if (Keyboard.Modifiers.HasFlag(
                ModifierKeys.Shift))
        {
            return;
        }

        e.Handled =
            true;

        await SendCurrentInputAsync();
    }

    private async Task SendCurrentInputAsync()
    {
        if (_runCancellation is not null)
        {
            return;
        }

        AgentCore? agent =
            _agent;

        if (agent is null)
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

        NewChatButton.IsEnabled =
            false;

        try
        {
            LlmResponse response =
                await agent.RunAsync(
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

            NewChatButton.IsEnabled =
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

    private async void NewChatButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_runCancellation is not null)
        {
            return;
        }

        try
        {
            LlmProfile activeProfile =
                GetActiveProfile(
                    _llmCatalog);

            AgentCore newAgent =
                await CreateAgentAsync(
                    _options,
                    activeProfile);

            _activeProfile =
                activeProfile;

            _agent =
                newAgent;

            _items.Clear();

            InputTextBox.Clear();

            AddActivity(
                "✓ Новый чат начат.");

            UpdateStatus();

            InputTextBox.Focus();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "Не удалось начать новый чат",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static bool AreProfilesEquivalent(
        LlmProfile? left,
        LlmProfile? right)
    {
        if (ReferenceEquals(
                left,
                right))
        {
            return true;
        }

        if (left is null
            ||
            right is null)
        {
            return false;
        }

        return string.Equals(
                left.Id,
                right.Id,
                StringComparison.Ordinal)
            &&
            string.Equals(
                left.Name,
                right.Name,
                StringComparison.Ordinal)
            &&
            string.Equals(
                left.ApiFormat,
                right.ApiFormat,
                StringComparison.Ordinal)
            &&
            string.Equals(
                left.Endpoint,
                right.Endpoint,
                StringComparison.Ordinal)
            &&
            string.Equals(
                left.Model,
                right.Model,
                StringComparison.Ordinal)
            &&
            string.Equals(
                left.SecretSource,
                right.SecretSource,
                StringComparison.Ordinal)
            &&
            string.Equals(
                left.SecretReference,
                right.SecretReference,
                StringComparison.Ordinal);
    }
}