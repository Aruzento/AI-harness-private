using System.Text.Json;

namespace MyAgent.Configuration;

public sealed class LegacyLlmSettings
{
    private const string DefaultEndpoint =
        "https://api.groq.com/openai/v1/chat/completions";

    private const string DefaultModel =
        "openai/gpt-oss-20b";

    private const string DefaultApiKeyEnvironmentVariable =
        "GROQ_API_KEY";

    public string LlmEndpoint { get; }

    public string Model { get; }

    public string ApiKeyEnvironmentVariable { get; }

    private LegacyLlmSettings(
        string llmEndpoint,
        string model,
        string apiKeyEnvironmentVariable)
    {
        LlmEndpoint =
            llmEndpoint;

        Model =
            model;

        ApiKeyEnvironmentVariable =
            apiKeyEnvironmentVariable;
    }

    public static LegacyLlmSettings Load(
        string? settingsFilePath = null)
    {
        string filePath =
            string.IsNullOrWhiteSpace(
                settingsFilePath)
                ? HarnessOptions.SettingsFilePath
                : Path.GetFullPath(
                    settingsFilePath);

        SavedLegacyLlmSettings saved =
            LoadSaved(
                filePath);

        return new LegacyLlmSettings(
            llmEndpoint:
                ReadString(
                    "AI_HARNESS_LLM_ENDPOINT",
                    ReadSavedString(
                        saved.LlmEndpoint,
                        DefaultEndpoint)),

            model:
                ReadString(
                    "AI_HARNESS_MODEL",
                    ReadSavedString(
                        saved.Model,
                        DefaultModel)),

            apiKeyEnvironmentVariable:
                ReadString(
                    "AI_HARNESS_API_KEY_ENV",
                    ReadSavedString(
                        saved.ApiKeyEnvironmentVariable,
                        DefaultApiKeyEnvironmentVariable)));
    }

    private static SavedLegacyLlmSettings LoadSaved(
        string settingsFilePath)
    {
        if (!File.Exists(
                settingsFilePath))
        {
            return new SavedLegacyLlmSettings();
        }

        try
        {
            string json =
                File.ReadAllText(
                    settingsFilePath);

            return JsonSerializer.Deserialize<
                       SavedLegacyLlmSettings>(
                       json,
                       new JsonSerializerOptions
                       {
                           PropertyNameCaseInsensitive =
                               true
                       })
                   ?? new SavedLegacyLlmSettings();
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Invalid settings file: {settingsFilePath}",
                exception);
        }
    }

    private static string ReadString(
        string name,
        string defaultValue)
    {
        string? value =
            Environment.GetEnvironmentVariable(
                name);

        return string.IsNullOrWhiteSpace(
            value)
            ? defaultValue
            : value;
    }

    private static string ReadSavedString(
        string? value,
        string defaultValue)
    {
        return string.IsNullOrWhiteSpace(
            value)
            ? defaultValue
            : value;
    }

    private sealed class SavedLegacyLlmSettings
    {
        public string? LlmEndpoint
        {
            get;
            set;
        }

        public string? Model
        {
            get;
            set;
        }

        public string? ApiKeyEnvironmentVariable
        {
            get;
            set;
        }
    }
}