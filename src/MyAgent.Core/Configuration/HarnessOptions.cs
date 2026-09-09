using System.Text.Json;

namespace MyAgent.Configuration;

public class HarnessOptions
{
    private const string DefaultEndpoint =
        "https://api.groq.com/openai/v1/chat/completions";

    private const string DefaultModel =
        "openai/gpt-oss-20b";

    private const string DefaultApiKeyEnvironmentVariable =
        "GROQ_API_KEY";

    private const string DefaultWorkspacePath =
        "Workdir";

    private const int DefaultMaxSteps =
        10;

    private const int DefaultMaxToolCalls =
        20;

    private const int DefaultTerminalTimeoutSeconds =
        15;

    public string LlmEndpoint { get; }

    public string Model { get; }

    public string ApiKeyEnvironmentVariable { get; }

    public string WorkspacePath { get; }

    public int MaxSteps { get; }

    public int MaxToolCalls { get; }

    public int TerminalTimeoutSeconds { get; }

    public static string SettingsFilePath =>
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "MyAgent",
            "settings.json");

    public HarnessOptions(
        string llmEndpoint,
        string model,
        string apiKeyEnvironmentVariable,
        string workspacePath,
        int maxSteps,
        int maxToolCalls,
        int terminalTimeoutSeconds)
    {
        if (string.IsNullOrWhiteSpace(llmEndpoint))
        {
            throw new ArgumentException(
                "LLM endpoint cannot be empty.",
                nameof(llmEndpoint));
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException(
                "Model cannot be empty.",
                nameof(model));
        }

        if (string.IsNullOrWhiteSpace(
                apiKeyEnvironmentVariable))
        {
            throw new ArgumentException(
                "API key environment variable cannot be empty.",
                nameof(apiKeyEnvironmentVariable));
        }

        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            throw new ArgumentException(
                "Workspace path cannot be empty.",
                nameof(workspacePath));
        }

        if (maxSteps <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxSteps));
        }

        if (maxToolCalls <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxToolCalls));
        }

        if (terminalTimeoutSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(terminalTimeoutSeconds));
        }

        LlmEndpoint =
            llmEndpoint;

        Model =
            model;

        ApiKeyEnvironmentVariable =
            apiKeyEnvironmentVariable;

        WorkspacePath =
            workspacePath;

        MaxSteps =
            maxSteps;

        MaxToolCalls =
            maxToolCalls;

        TerminalTimeoutSeconds =
            terminalTimeoutSeconds;
    }

    public static HarnessOptions Load()
    {
        HarnessOptions saved =
            LoadSaved();

        return new HarnessOptions(
            llmEndpoint:
                ReadString(
                    "AI_HARNESS_LLM_ENDPOINT",
                    saved.LlmEndpoint),

            model:
                ReadString(
                    "AI_HARNESS_MODEL",
                    saved.Model),

            apiKeyEnvironmentVariable:
                ReadString(
                    "AI_HARNESS_API_KEY_ENV",
                    saved.ApiKeyEnvironmentVariable),

            workspacePath:
                ReadString(
                    "AI_HARNESS_WORKDIR",
                    saved.WorkspacePath),

            maxSteps:
                ReadPositiveInt(
                    "AI_HARNESS_MAX_STEPS",
                    saved.MaxSteps),

            maxToolCalls:
                ReadPositiveInt(
                    "AI_HARNESS_MAX_TOOL_CALLS",
                    saved.MaxToolCalls),

            terminalTimeoutSeconds:
                ReadPositiveInt(
                    "AI_HARNESS_TERMINAL_TIMEOUT_SECONDS",
                    saved.TerminalTimeoutSeconds));
    }

    public static void Save(
        HarnessOptions options)
    {
        string? directory =
            Path.GetDirectoryName(
                SettingsFilePath);

        if (!string.IsNullOrWhiteSpace(
                directory))
        {
            Directory.CreateDirectory(
                directory);
        }

        var saved =
            new SavedHarnessOptions
            {
                LlmEndpoint =
                    options.LlmEndpoint,

                Model =
                    options.Model,

                ApiKeyEnvironmentVariable =
                    options.ApiKeyEnvironmentVariable,

                WorkspacePath =
                    options.WorkspacePath,

                MaxSteps =
                    options.MaxSteps,

                MaxToolCalls =
                    options.MaxToolCalls,

                TerminalTimeoutSeconds =
                    options.TerminalTimeoutSeconds
            };

        string json =
            JsonSerializer.Serialize(
                saved,
                new JsonSerializerOptions
                {
                    WriteIndented =
                        true
                });

        File.WriteAllText(
            SettingsFilePath,
            json);
    }

    private static HarnessOptions LoadSaved()
    {
        if (!File.Exists(
                SettingsFilePath))
        {
            return CreateDefaults();
        }

        try
        {
            string json =
                File.ReadAllText(
                    SettingsFilePath);

            SavedHarnessOptions? saved =
                JsonSerializer.Deserialize<
                    SavedHarnessOptions>(
                    json);

            if (saved is null)
            {
                return CreateDefaults();
            }

            return new HarnessOptions(
                llmEndpoint:
                    ReadSavedString(
                        saved.LlmEndpoint,
                        DefaultEndpoint),

                model:
                    ReadSavedString(
                        saved.Model,
                        DefaultModel),

                apiKeyEnvironmentVariable:
                    ReadSavedString(
                        saved.ApiKeyEnvironmentVariable,
                        DefaultApiKeyEnvironmentVariable),

                workspacePath:
                    ReadSavedString(
                        saved.WorkspacePath,
                        DefaultWorkspacePath),

                maxSteps:
                    ReadSavedPositiveInt(
                        saved.MaxSteps,
                        DefaultMaxSteps),

                maxToolCalls:
                    ReadSavedPositiveInt(
                        saved.MaxToolCalls,
                        DefaultMaxToolCalls),

                terminalTimeoutSeconds:
                    ReadSavedPositiveInt(
                        saved.TerminalTimeoutSeconds,
                        DefaultTerminalTimeoutSeconds));
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Invalid settings file: {SettingsFilePath}",
                exception);
        }
    }

    private static HarnessOptions CreateDefaults()
    {
        return new HarnessOptions(
            DefaultEndpoint,
            DefaultModel,
            DefaultApiKeyEnvironmentVariable,
            DefaultWorkspacePath,
            DefaultMaxSteps,
            DefaultMaxToolCalls,
            DefaultTerminalTimeoutSeconds);
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

    private static string ReadSavedString(
        string? value,
        string defaultValue)
    {
        return string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : value;
    }

    private static int ReadSavedPositiveInt(
        int? value,
        int defaultValue)
    {
        return value is > 0
            ? value.Value
            : defaultValue;
    }

    private sealed class SavedHarnessOptions
    {
        public string? LlmEndpoint { get; set; }

        public string? Model { get; set; }

        public string? ApiKeyEnvironmentVariable
        {
            get;
            set;
        }

        public string? WorkspacePath { get; set; }

        public int? MaxSteps { get; set; }

        public int? MaxToolCalls { get; set; }

        public int? TerminalTimeoutSeconds
        {
            get;
            set;
        }
    }
}