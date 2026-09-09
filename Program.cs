using MyAgent.Agent;
using MyAgent.Llm;

Console.WriteLine("Введите задачу:");

string? task = Console.ReadLine();

if (string.IsNullOrWhiteSpace(task))
{
    Console.WriteLine("Задача не введена.");
    return;
}

string? apiKey =
    Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine(
        "Не найдена переменная окружения OPENROUTER_API_KEY.");

    return;
}

using var httpClient = new HttpClient();

ILlmClient llmClient =
    new OpenRouterLlmClient(httpClient, apiKey);

var agent =
    new Agent(llmClient);

try
{
    string result =
        await agent.RunAsync(task);

    Console.WriteLine();
    Console.WriteLine("Ответ LLM:");
    Console.WriteLine(result);
}
catch (Exception exception)
{
    Console.WriteLine();
    Console.WriteLine("Ошибка:");
    Console.WriteLine(exception.Message);
}