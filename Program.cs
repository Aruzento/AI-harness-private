using MyAgent.Agent;
using MyAgent.Llm;
using MyAgent.Tools;
using MyAgent.Workspace;
using MyAgent.Guardrails;

string? apiKey =
    Environment.GetEnvironmentVariable(
        "OPENROUTER_API_KEY");

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine(
        "Не найдена переменная окружения OPENROUTER_API_KEY.");

    return;
}

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
    new OpenRouterLlmClient(
        httpClient,
        apiKey);

var workspace =
    new AgentWorkspace(
        "Workdir");

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
        workspace));

var policy =
    new AgentPolicy(
        maxSteps: 10,
        maxToolCalls: 20);

IToolApproval toolApproval =
    new ConsoleToolApproval();

var agent =
    new Agent(
        llmClient,
        toolRegistry,
        policy,
        toolApproval,
        systemPrompt);

Console.WriteLine(
    "AI Harness запущен.");

Console.WriteLine(
    "Команды:");

Console.WriteLine(
    "/exit — выход");

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