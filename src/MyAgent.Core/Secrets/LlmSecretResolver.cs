using MyAgent.Configuration;

namespace MyAgent.Secrets;

public sealed class LlmSecretResolver
    : ILlmSecretResolver
{
    private readonly ISecretStore _secretStore;

    public LlmSecretResolver(
        ISecretStore secretStore)
    {
        _secretStore =
            secretStore
            ?? throw new ArgumentNullException(
                nameof(secretStore));
    }

    public async Task<string> ResolveAsync(
        LlmProfile profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            profile);

        if (string.IsNullOrWhiteSpace(
                profile.SecretSource))
        {
            throw new InvalidOperationException(
                "LLM profile has no secret source.");
        }

        if (string.IsNullOrWhiteSpace(
                profile.SecretReference))
        {
            throw new InvalidOperationException(
                "LLM profile has no secret reference.");
        }

        if (string.Equals(
                profile.SecretSource,
                LlmSecretSources.EncryptedStore,
                StringComparison.OrdinalIgnoreCase))
        {
            string? value =
                await _secretStore.GetAsync(
                    profile.SecretReference,
                    cancellationToken);

            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new InvalidOperationException(
                    "Secret not found for LLM profile "
                    + $"'{profile.Name}'.");
            }

            return value;
        }

        if (string.Equals(
                profile.SecretSource,
                LlmSecretSources.Environment,
                StringComparison.OrdinalIgnoreCase))
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            string? value =
                Environment.GetEnvironmentVariable(
                    profile.SecretReference);

            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new InvalidOperationException(
                    "Environment variable "
                    + $"'{profile.SecretReference}' "
                    + "was not found for LLM profile "
                    + $"'{profile.Name}'.");
            }

            return value;
        }

        throw new NotSupportedException(
            "Unsupported LLM secret source: "
            + profile.SecretSource);
    }
}