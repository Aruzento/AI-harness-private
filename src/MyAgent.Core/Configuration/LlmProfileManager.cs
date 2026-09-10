using MyAgent.Secrets;

namespace MyAgent.Configuration;

public sealed class LlmProfileManager
{
    private readonly LlmProfileStore _profileStore;
    private readonly ISecretStore _secretStore;

    public LlmProfileManager(
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

    public Task<LlmProfileCatalog> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        return _profileStore.LoadAsync(
            cancellationToken);
    }

    public async Task<LlmProfileCatalog>
        AddEncryptedAsync(
            string name,
            string endpoint,
            string model,
            string apiKey,
            bool makeActive = false,
            string apiFormat =
                LlmApiFormats.OpenAiChatCompletions,
            CancellationToken cancellationToken = default)
    {
        ValidateProfileInput(
            name,
            endpoint,
            model,
            apiFormat);

        if (string.IsNullOrWhiteSpace(
                apiKey))
        {
            throw new ArgumentException(
                "API key cannot be empty.",
                nameof(apiKey));
        }

        LlmProfileCatalog current =
            await _profileStore.LoadAsync(
                cancellationToken);

        string profileId =
            "profile-"
            + Guid.NewGuid()
                .ToString("N");

        string secretReference =
            "secret-"
            + Guid.NewGuid()
                .ToString("N");

        await _secretStore.SetAsync(
            secretReference,
            apiKey,
            cancellationToken);

        bool secretCreated =
            true;

        try
        {
            var profile =
                new LlmProfile
                {
                    Id =
                        profileId,

                    Name =
                        name.Trim(),

                    ApiFormat =
                        apiFormat.Trim(),

                    Endpoint =
                        endpoint.Trim(),

                    Model =
                        model.Trim(),

                    SecretSource =
                        LlmSecretSources.EncryptedStore,

                    SecretReference =
                        secretReference
                };

            LlmProfile[] profiles =
                current.Profiles
                    .Append(profile)
                    .ToArray();

            string? activeProfileId =
                makeActive
                || current.ActiveProfileId is null
                    ? profile.Id
                    : current.ActiveProfileId;

            var updated =
                new LlmProfileCatalog
                {
                    ActiveProfileId =
                        activeProfileId,

                    Profiles =
                        profiles
                };

            await _profileStore.SaveAsync(
                updated,
                cancellationToken);

            secretCreated =
                false;

            return updated;
        }
        finally
        {
            if (secretCreated)
            {
                try
                {
                    await _secretStore.DeleteAsync(
                        secretReference,
                        CancellationToken.None);
                }
                catch
                {
                    // Preserve the original failure.
                }
            }
        }
    }

    public async Task<LlmProfileCatalog>
        SetActiveAsync(
            string profileId,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
                profileId))
        {
            throw new ArgumentException(
                "Profile ID cannot be empty.",
                nameof(profileId));
        }

        LlmProfileCatalog current =
            await _profileStore.LoadAsync(
                cancellationToken);

        bool exists =
            current.Profiles.Any(
                profile =>
                    string.Equals(
                        profile.Id,
                        profileId,
                        StringComparison.Ordinal));

        if (!exists)
        {
            throw new InvalidOperationException(
                "LLM profile does not exist: "
                + profileId);
        }

        if (string.Equals(
                current.ActiveProfileId,
                profileId,
                StringComparison.Ordinal))
        {
            return current;
        }

        var updated =
            new LlmProfileCatalog
            {
                ActiveProfileId =
                    profileId,

                Profiles =
                    current.Profiles
            };

        await _profileStore.SaveAsync(
            updated,
            cancellationToken);

        return updated;
    }

    private static void ValidateProfileInput(
        string name,
        string endpoint,
        string model,
        string apiFormat)
    {
        if (string.IsNullOrWhiteSpace(
                name))
        {
            throw new ArgumentException(
                "Profile name cannot be empty.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(
                endpoint))
        {
            throw new ArgumentException(
                "Endpoint cannot be empty.",
                nameof(endpoint));
        }

        if (!Uri.TryCreate(
                endpoint.Trim(),
                UriKind.Absolute,
                out Uri? endpointUri)
            ||
            (endpointUri.Scheme !=
                Uri.UriSchemeHttps
             &&
             endpointUri.Scheme !=
                Uri.UriSchemeHttp))
        {
            throw new ArgumentException(
                "Endpoint must be a valid HTTP or HTTPS URL.",
                nameof(endpoint));
        }

        if (string.IsNullOrWhiteSpace(
                model))
        {
            throw new ArgumentException(
                "Model cannot be empty.",
                nameof(model));
        }

        if (string.IsNullOrWhiteSpace(
                apiFormat))
        {
            throw new ArgumentException(
                "API format cannot be empty.",
                nameof(apiFormat));
        }
    }
}