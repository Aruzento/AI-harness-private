using MyAgent.Agent;
using MyAgent.Llm;
using MyAgent.Tools;
using MyAgent.Workspace;
using MyAgent.Guardrails;
using MyAgent.Cli;
using MyAgent.Configuration;

string? apiKey =
    Environment.GetEnvironmentVariable(
        "GROQ_API_KEY");

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine(
        "Не найдена переменная окружения GROQ_API_KEY.");

    return;
}

HarnessOptions options =
    HarnessOptions.FromEnvironment();

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

ILlmClient llmClient =
    new OpenAiCompatibleLlmClient(
        httpClient,
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
    "AI Harness v0.1.1 запущен.");

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
            $"Model: {options.Model}");
        
        Console.WriteLine(
            $"LLM endpoint: {options.LlmEndpoint}");

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