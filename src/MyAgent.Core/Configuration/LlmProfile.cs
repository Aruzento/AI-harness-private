namespace MyAgent.Configuration;

public sealed class LlmProfile
{
    public string Id { get; init; } =
        string.Empty;

    public string Name { get; init; } =
        string.Empty;

    public string ApiFormat { get; init; } =
        LlmApiFormats.OpenAiChatCompletions;

    public string Endpoint { get; init; } =
        string.Empty;

    public string Model { get; init; } =
        string.Empty;

    public string SecretSource { get; init; } =
        LlmSecretSources.EncryptedStore;

    public string SecretReference { get; init; } =
        string.Empty;
}

public static class LlmApiFormats
{
    public const string OpenAiChatCompletions =
        "openai-chat-completions";
}

public static class LlmSecretSources
{
    public const string EncryptedStore =
        "encrypted-store";

    public const string Environment =
        "environment";
}