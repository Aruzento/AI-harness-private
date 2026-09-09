using MyAgent.Agent;
using MyAgent.Llm;
using System.Text.Json;
using MyAgent.Tools;

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

var agent =
    new Agent(
        llmClient,
        systemPrompt);

var toolRegistry =
    new ToolRegistry();

toolRegistry.Register(
    new EchoTool());

Console.WriteLine(
    "AI Harness запущен.");

Console.WriteLine(
    "Команды:");

Console.WriteLine(
    "/echo <текст> — тестовый tool");

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

    const string echoPrefix =
    "/echo ";

if (input.StartsWith(
        echoPrefix,
        StringComparison.OrdinalIgnoreCase))
{
    string text =
        input[echoPrefix.Length..];

    JsonElement arguments =
        JsonSerializer.SerializeToElement(
            new
            {
                text
            });

    ToolResult toolResult =
        await toolRegistry.ExecuteAsync(
            "echo",
            arguments);

    Console.WriteLine();

    if (toolResult.Success)
    {
        Console.WriteLine(
            $"Tool echo: {toolResult.Content}");
    }
    else
    {
        Console.WriteLine(
            $"Ошибка tool: {toolResult.Error}");
    }

    Console.WriteLine();

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
            $"LLM: {result.Content}");

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