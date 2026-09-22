using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
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
using MyAgent.Chats;
using MyAgent.Projects;

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

    private readonly ProjectManager
        _projectManager;

    private readonly ChatManager
        _chatManager;

    private AgentProject? _activeProject;

    private AgentChat? _activeChat;

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

    var projectStore =
        new ProjectStore();

    _projectManager =
        new ProjectManager(
            projectStore);

    var chatStore =
        new ChatStore();

    _chatManager =
        new ChatManager(
            chatStore,
            projectStore);

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
            LegacyLlmSettings legacyLlmSettings =
                LegacyLlmSettings.Load();

            _llmCatalog =
                await _llmProfileBootstrapper
                    .EnsureInitializedAsync(
                        legacyLlmSettings);

            _activeProfile =
                GetActiveProfile(
                    _llmCatalog);

            await EnsureDesktopSessionAsync();

            AgentProject activeProject =
                _activeProject
                ?? throw new InvalidOperationException(
                    "Active project is unavailable.");

            AgentChat activeChat =
                _activeChat
                ?? throw new InvalidOperationException(
                    "Active chat is unavailable.");

            _agent =
                await CreateAgentAsync(
                    _options,
                    _activeProfile,
                    activeProject.WorkspacePath,
                    activeChat.Messages);

            LoadChatItems(
                activeChat);

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

    private async Task EnsureDesktopSessionAsync(
        CancellationToken cancellationToken = default)
    {
        ProjectCatalog projectCatalog =
            await _projectManager.LoadAsync(
                cancellationToken);

        AgentProject? activeProject =
            projectCatalog.Projects
                .FirstOrDefault(
                    project =>
                        string.Equals(
                            project.Id,
                            projectCatalog.ActiveProjectId,
                            StringComparison.Ordinal));

        if (activeProject is null
            && projectCatalog.Projects.Length > 0)
        {
            activeProject =
                projectCatalog.Projects[0];

            projectCatalog =
                await _projectManager.SetActiveAsync(
                    activeProject.Id,
                    cancellationToken);
        }

        if (activeProject is null)
        {
            string initialWorkspacePath =
                Path.GetFullPath(
                    _options.WorkspacePath);

            projectCatalog =
                await _projectManager.AddAsync(
                    GetInitialProjectName(
                        initialWorkspacePath),
                    initialWorkspacePath,
                    makeActive: true,
                    cancellationToken);

            activeProject =
                projectCatalog.Projects.Single(
                    project =>
                        string.Equals(
                            project.Id,
                            projectCatalog.ActiveProjectId,
                            StringComparison.Ordinal));
        }

        _activeProject =
            activeProject;

        ChatCatalog chatCatalog =
            await _chatManager.LoadAsync(
                cancellationToken);

        AgentChat? activeChat =
            chatCatalog.Chats
                .FirstOrDefault(
                    chat =>
                        string.Equals(
                            chat.Id,
                            chatCatalog.ActiveChatId,
                            StringComparison.Ordinal)
                        &&
                        string.Equals(
                            chat.ProjectId,
                            activeProject.Id,
                            StringComparison.Ordinal));

        if (activeChat is null)
        {
            IReadOnlyList<AgentChat> projectChats =
                await _chatManager.GetProjectChatsAsync(
                    activeProject.Id,
                    cancellationToken);

            activeChat =
                projectChats.FirstOrDefault();

            if (activeChat is not null)
            {
                await _chatManager.SetActiveAsync(
                    activeChat.Id,
                    cancellationToken);
            }
        }

        if (activeChat is null)
        {
            chatCatalog =
                await _chatManager.AddAsync(
                    activeProject.Id,
                    "Новый чат",
                    makeActive: true,
                    cancellationToken);

            activeChat =
                chatCatalog.Chats.Single(
                    chat =>
                        string.Equals(
                            chat.Id,
                            chatCatalog.ActiveChatId,
                            StringComparison.Ordinal));
        }

        _activeChat =
            activeChat;
    }

    private static string GetInitialProjectName(
        string workspacePath)
    {
        string fullPath =
            Path.GetFullPath(
                workspacePath);

        string trimmedPath =
            Path.TrimEndingDirectorySeparator(
                fullPath);

        string name =
            Path.GetFileName(
                trimmedPath);

        return string.IsNullOrWhiteSpace(name)
            ? fullPath
            : name;
    }

    private async Task<AgentCore> CreateAgentAsync(
        HarnessOptions options,
        LlmProfile profile,
        string workspacePath,
        IReadOnlyList<Message>? initialMessages = null,
        CancellationToken cancellationToken = default)
    {
        ILlmClient llmClient =
            await _llmClientFactory.CreateAsync(
                profile,
                cancellationToken);

        var workspace =
            new AgentWorkspace(
                workspacePath);

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
            _systemPrompt,
            initialMessages);
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

    private void LoadChatItems(
        AgentChat chat)
    {
        _items.Clear();

        foreach (Message message
                in chat.Messages)
        {
            if (message.Role ==
                MessageRole.User
                &&
                !string.IsNullOrWhiteSpace(
                    message.Content))
            {
                AddChatItem(
                    new UserMessageItem(
                        message.Content));

                continue;
            }

            if (message.Role ==
                MessageRole.Assistant
                &&
                message.ToolCalls.Count == 0
                &&
                !string.IsNullOrWhiteSpace(
                    message.Content))
            {
                AddChatItem(
                    new AssistantMessageItem(
                        message.Content));
            }
        }
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
        ToolApprovalRequest request,
        CancellationToken cancellationToken)
    {
        if (!Dispatcher.CheckAccess())
        {
            return await Dispatcher.Invoke(
                () =>
                    RequestToolApprovalAsync(
                        request,
                        cancellationToken));
        }

        var item =
            new ApprovalItem(
                request.ToolCall.Name,
                request.Preview.Text,
                request.Preview.FileChange
                    is not null);

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

            AgentProject activeProject =
                _activeProject
                ?? throw new InvalidOperationException(
                    "Active project is unavailable.");

            AgentChat activeChat =
                _activeChat
                ?? throw new InvalidOperationException(
                    "Active chat is unavailable.");

            AgentCore newAgent =
                await CreateAgentAsync(
                    newOptions,
                    newActiveProfile,
                    newOptions.WorkspacePath,
                    activeChat.Messages);

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

    private async Task PersistActiveChatAsync(
        AgentCore agent)
    {
        AgentChat activeChat =
            _activeChat
            ?? throw new InvalidOperationException(
                "Active chat is unavailable.");

        ChatCatalog updatedCatalog =
            await _chatManager.ReplaceMessagesAsync(
                activeChat.Id,
                agent.CreatePersistentHistorySnapshot(),
                CancellationToken.None);

        _activeChat =
            updatedCatalog.Chats.Single(
                chat =>
                    string.Equals(
                        chat.Id,
                        activeChat.Id,
                        StringComparison.Ordinal));
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

            await PersistActiveChatAsync(
                agent);
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

            AgentProject activeProject =
                _activeProject
                ?? throw new InvalidOperationException(
                    "Active project is unavailable.");

            AgentCore newAgent =
                await CreateAgentAsync(
                    _options,
                    activeProfile,
                    activeProject.WorkspacePath,
                    Array.Empty<Message>());

            ChatCatalog chatCatalog =
                await _chatManager.AddAsync(
                    activeProject.Id,
                    "Новый чат",
                    makeActive: true);

            AgentChat newChat =
                chatCatalog.Chats.Single(
                    chat =>
                        string.Equals(
                            chat.Id,
                            chatCatalog.ActiveChatId,
                            StringComparison.Ordinal));

            _activeProfile =
                activeProfile;

            _activeChat =
                newChat;

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