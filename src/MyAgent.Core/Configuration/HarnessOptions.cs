namespace MyAgent.Configuration;

public class HarnessOptions
{
    public string Model { get; }

    public string WorkspacePath { get; }

    public int MaxSteps { get; }

    public int MaxToolCalls { get; }

    public int TerminalTimeoutSeconds { get; }

    public string LlmEndpoint { get; }

    public HarnessOptions(
        string llmEndpoint,
        string model,
        string workspacePath,
        int maxSteps,
        int maxToolCalls,
        int terminalTimeoutSeconds)
    {
        LlmEndpoint = llmEndpoint;
        Model = model;
        WorkspacePath = workspacePath;
        MaxSteps = maxSteps;
        MaxToolCalls = maxToolCalls;
        TerminalTimeoutSeconds =
            terminalTimeoutSeconds;
    }

    public static HarnessOptions FromEnvironment()
    {
        return new HarnessOptions(
            llmEndpoint: ReadString(
                "AI_HARNESS_LLM_ENDPOINT",
                "https://api.groq.com/openai/v1/chat/completions"),

            model: ReadString(
                "AI_HARNESS_MODEL",
                "openai/gpt-oss-20b"),

            workspacePath: ReadString(
                "AI_HARNESS_WORKDIR",
                "Workdir"),

            maxSteps: ReadPositiveInt(
                "AI_HARNESS_MAX_STEPS",
                10),

            maxToolCalls: ReadPositiveInt(
                "AI_HARNESS_MAX_TOOL_CALLS",
                20),

            terminalTimeoutSeconds:
                ReadPositiveInt(
                    "AI_HARNESS_TERMINAL_TIMEOUT_SECONDS",
                    15));
    }

    private static string ReadString(
        string name,
        string defaultValue)
    {
        string? value =
            Environment.GetEnvironmentVariable(
                name);

        return string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : value;
    }

    private static int ReadPositiveInt(
        string name,
        int defaultValue)
    {
        string? value =
            Environment.GetEnvironmentVariable(
                name);

        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (!int.TryParse(
                value,
                out int parsed)
            ||
            parsed <= 0)
        {
            throw new InvalidOperationException(
                $"Environment variable {name} "
                + "must be a positive integer.");
        }

        return parsed;
    }
}