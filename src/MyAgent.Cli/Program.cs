using MyAgent.Agent;
using MyAgent.Cli;
using MyAgent.Configuration;
using MyAgent.Guardrails;
using MyAgent.Llm;
using MyAgent.Tools;
using MyAgent.Workspace;
using MyAgent.Secrets;

HarnessOptions options =
    HarnessOptions.Load();

string systemPromptPath =
    Path.Combine(
        "Prompts",
        "system.md");

if (!File.Exists(systemPromptPath))
{
    Console.WriteLine(
        $"Не найден system prompt: {systemPromptPath}");

    return;
}

string systemPrompt =
    await File.ReadAllTextAsync(
        systemPromptPath);

using var httpClient =
    new HttpClient();

if (!OperatingSystem.IsWindows())
{
    Console.WriteLine(
        "Текущее хранилище API keys использует Windows DPAPI.");

    return;
}

var profileStore =
    new LlmProfileStore();

ISecretStore secretStore =
    new DpapiSecretStore();

var secretResolver =
    new LlmSecretResolver(
        secretStore);

var profileBootstrapper =
    new LlmProfileBootstrapper(
        profileStore,
        secretStore);

ILlmClientFactory llmClientFactory =
    new LlmClientFactory(
        httpClient,
        secretResolver);

LegacyLlmSettings legacyLlmSettings =
    LegacyLlmSettings.Load();

LlmProfileCatalog profileCatalog =
    await profileBootstrapper
        .EnsureInitializedAsync(
            legacyLlmSettings);

LlmProfile? activeProfile =
    profileCatalog.Profiles
        .FirstOrDefault(
            profile =>
                string.Equals(
                    profile.Id,
                    profileCatalog.ActiveProfileId,
                    StringComparison.Ordinal));

if (activeProfile is null)
{
    Console.WriteLine(
        "Активная LLM-модель не настроена.");

    Console.WriteLine(
        "Откройте Desktop-приложение "
        + "и добавьте или выберите модель.");

    return;
}

ILlmClient llmClient =
    await llmClientFactory.CreateAsync(
        activeProfile);

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
        maxSteps: options.MaxSteps,
        maxToolCalls: options.MaxToolCalls);

IToolApproval toolApproval =
    new ConsoleToolApproval();

IAgentObserver observer =
    new ConsoleAgentObserver();

var agent =
    new Agent(
        llmClient,
        toolRegistry,
        policy,
        toolApproval,
        observer,
        systemPrompt);

Console.WriteLine(
    $"AI Harness v{HarnessVersion.Current} запущен.");

Console.WriteLine(
    "Команды:");

Console.WriteLine(
    "/exit — выход");

Console.WriteLine(
    "/status — конфигурация harness");

Console.WriteLine();

while (true)
{
    Console.Write("Вы: ");

    string? input =
        Console.ReadLine();

    if (input is null)
    {
        break;
    }

    if (input.Equals(
        "/exit",
        StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    if (input.Equals(
        "/status",
        StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine();

        Console.WriteLine(
            $"Profile: {activeProfile.Name}");

        Console.WriteLine(
            $"Model: {activeProfile.Model}");

        Console.WriteLine(
            $"LLM endpoint: {activeProfile.Endpoint}");

        Console.WriteLine(
            $"Workspace: {workspace.RootPath}");

        Console.WriteLine(
            $"Max steps: {options.MaxSteps}");

        Console.WriteLine(
            $"Max tool calls: {options.MaxToolCalls}");

        Console.WriteLine(
            "Terminal timeout: "
            + $"{options.TerminalTimeoutSeconds}s");
        
        Console.WriteLine(
            $"Version: {HarnessVersion.Current}");

        Console.WriteLine(
            $"Secret source: {activeProfile.SecretSource}");

        Console.WriteLine(
            $"Settings: {HarnessOptions.SettingsFilePath}");

        Console.WriteLine(
            "Tools: "
            + string.Join(
                ", ",
                toolRegistry.Tools
                    .Select(tool => tool.Name)));

        Console.WriteLine();

        continue;
    }

    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    try
    {
        LlmResponse result =
            await agent.RunAsync(input);

        Console.WriteLine();

        Console.WriteLine(
            $"[Model: {result.Model}]");

        Console.WriteLine(
            $"LLM: {result.Content ?? "<empty>"}");
        
        if (agent.LastRunState is not null)
        {
            Console.WriteLine();

            Console.WriteLine(
                "[State: "
                + $"steps={agent.LastRunState.StepCount}, "
                + $"toolCalls={agent.LastRunState.ToolCallCount}, "
                + $"denied={agent.LastRunState.DeniedToolCallCount}]");
        }

        Console.WriteLine();
    }
    catch (Exception exception)
    {
        Console.WriteLine();
        Console.WriteLine(
            $"Ошибка: {exception.Message}");
        Console.WriteLine();
    }
}