using MyAgent.Secrets;

namespace MyAgent.Configuration;

public sealed class LlmProfileBootstrapper
{
    private readonly LlmProfileStore _profileStore;
    private readonly ISecretStore _secretStore;

    public LlmProfileBootstrapper(
        LlmProfileStore profileStore,
        ISecretStore secretStore)
    {
        _profileStore =
            profileStore
            ?? throw new ArgumentNullException(
                nameof(profileStore));

        _secretStore =
            secretStore
            ?? throw new ArgumentNullException(
                nameof(secretStore));
    }

    public async Task<LlmProfileCatalog> EnsureInitializedAsync(
        LegacyLlmSettings legacyOptions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            legacyOptions);

        LlmProfileCatalog existingCatalog =
            await _profileStore.LoadAsync(
                cancellationToken);

        if (existingCatalog.Profiles.Length > 0)
        {
            return existingCatalog;
        }

        string profileId =
            "profile-"
            + Guid.NewGuid().ToString("N");

        string? legacyApiKey =
            Environment.GetEnvironmentVariable(
                legacyOptions.ApiKeyEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(
                legacyApiKey))
        {
            return existingCatalog;
        }

        string secretReference =
            "secret-"
            + Guid.NewGuid().ToString("N");

        await _secretStore.SetAsync(
            secretReference,
            legacyApiKey,
            cancellationToken);

        bool encryptedSecretCreated =
            true;

        string secretSource =
            LlmSecretSources.EncryptedStore;

        var profile =
            new LlmProfile
            {
                Id =
                    profileId,

                Name =
                    CreateLegacyProfileName(
                        legacyOptions),

                ApiFormat =
                    LlmApiFormats.OpenAiChatCompletions,

                Endpoint =
                    legacyOptions.LlmEndpoint,

                Model =
                    legacyOptions.Model,

                SecretSource =
                    secretSource,

                SecretReference =
                    secretReference
            };

        var catalog =
            new LlmProfileCatalog
            {
                ActiveProfileId =
                    profile.Id,

                Profiles =
                    new[]
                    {
                        profile
                    }
            };

        try
        {
            await _profileStore.SaveAsync(
                catalog,
                cancellationToken);

            return catalog;
        }
        catch
        {
            if (encryptedSecretCreated)
            {
                try
                {
                    await _secretStore.DeleteAsync(
                        secretReference,
                        CancellationToken.None);
                }
                catch
                {
                    // Preserve the original migration failure.
                }
            }

            throw;
        }
    }

    private static string CreateLegacyProfileName(
        LegacyLlmSettings options)
    {
        if (options.LlmEndpoint.Contains(
                "groq.com",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Groq · "
                + options.Model;
        }

        return "Imported · "
            + options.Model;
    }
}