using MyAgent.Agent;
using MyAgent.Llm;

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

Console.WriteLine(
    "AI Harness запущен.");

Console.WriteLine(
    "Введите /exit для выхода.");

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
        string result =
            await agent.RunAsync(input);

        Console.WriteLine();
        Console.WriteLine($"LLM: {result}");
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