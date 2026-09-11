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

    public async Task<LlmProfileCatalog>
    UpdateAsync(
        string profileId,
        string name,
        string endpoint,
        string model,
        string? newApiKey = null,
        string apiFormat =
            LlmApiFormats.OpenAiChatCompletions,
        CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(
            profileId))
    {
        throw new ArgumentException(
            "Profile ID cannot be empty.",
            nameof(profileId));
    }

    ValidateProfileInput(
        name,
        endpoint,
        model,
        apiFormat);

    if (newApiKey is not null
        &&
        string.IsNullOrWhiteSpace(
            newApiKey))
    {
        throw new ArgumentException(
            "New API key cannot be empty.",
            nameof(newApiKey));
    }

    LlmProfileCatalog current =
        await _profileStore.LoadAsync(
            cancellationToken);

    LlmProfile? existingProfile =
        current.Profiles
            .FirstOrDefault(
                profile =>
                    string.Equals(
                        profile.Id,
                        profileId,
                        StringComparison.Ordinal));

    if (existingProfile is null)
    {
        throw new InvalidOperationException(
            "LLM profile does not exist: "
            + profileId);
    }

    string secretSource =
        existingProfile.SecretSource;

    string secretReference =
        existingProfile.SecretReference;

    string? newSecretReference =
        null;

    bool newSecretCreated =
        false;

    if (newApiKey is not null)
    {
        newSecretReference =
            "secret-"
            + Guid.NewGuid()
                .ToString("N");

        await _secretStore.SetAsync(
            newSecretReference,
            newApiKey,
            cancellationToken);

        secretSource =
            LlmSecretSources.EncryptedStore;

        secretReference =
            newSecretReference;

        newSecretCreated =
            true;
    }

    var updatedProfile =
        new LlmProfile
        {
            Id =
                existingProfile.Id,

            Name =
                name.Trim(),

            ApiFormat =
                apiFormat.Trim(),

            Endpoint =
                endpoint.Trim(),

            Model =
                model.Trim(),

            SecretSource =
                secretSource,

            SecretReference =
                secretReference
        };

    LlmProfile[] updatedProfiles =
        current.Profiles
            .Select(
                profile =>
                    string.Equals(
                        profile.Id,
                        profileId,
                        StringComparison.Ordinal)
                        ? updatedProfile
                        : profile)
            .ToArray();

    var updatedCatalog =
        new LlmProfileCatalog
        {
            ActiveProfileId =
                current.ActiveProfileId,

            Profiles =
                updatedProfiles
        };

    try
    {
        await _profileStore.SaveAsync(
            updatedCatalog,
            cancellationToken);
    }
    catch
    {
        if (newSecretCreated
            &&
            newSecretReference is not null)
        {
            try
            {
                await _secretStore.DeleteAsync(
                    newSecretReference,
                    CancellationToken.None);
            }
            catch
            {
                // Preserve the original update failure.
            }
        }

        throw;
    }

    if (newSecretCreated
        &&
        string.Equals(
            existingProfile.SecretSource,
            LlmSecretSources.EncryptedStore,
            StringComparison.OrdinalIgnoreCase)
        &&
        !string.Equals(
            existingProfile.SecretReference,
            secretReference,
            StringComparison.Ordinal))
    {
        bool oldSecretStillUsed =
            updatedProfiles.Any(
                profile =>
                    string.Equals(
                        profile.SecretSource,
                        LlmSecretSources.EncryptedStore,
                        StringComparison.OrdinalIgnoreCase)
                    &&
                    string.Equals(
                        profile.SecretReference,
                        existingProfile.SecretReference,
                        StringComparison.Ordinal));

        if (!oldSecretStillUsed)
        {
            try
            {
                await _secretStore.DeleteAsync(
                    existingProfile.SecretReference,
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    "LLM profile was updated, "
                    + "but its previous encrypted secret "
                    + "could not be removed.",
                    exception);
            }
        }
    }

    return updatedCatalog;
}

    public async Task<LlmProfileCatalog>
        DeleteAsync(
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

        LlmProfile? profile =
            current.Profiles
                .FirstOrDefault(
                    candidate =>
                        string.Equals(
                            candidate.Id,
                            profileId,
                            StringComparison.Ordinal));

        if (profile is null)
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
            throw new InvalidOperationException(
                "Active LLM profile cannot be deleted. "
                + "Select another active profile first.");
        }

        LlmProfile[] remainingProfiles =
            current.Profiles
                .Where(
                    candidate =>
                        !string.Equals(
                            candidate.Id,
                            profileId,
                            StringComparison.Ordinal))
                .ToArray();

        var updated =
            new LlmProfileCatalog
            {
                ActiveProfileId =
                    current.ActiveProfileId,

                Profiles =
                    remainingProfiles
            };

        await _profileStore.SaveAsync(
            updated,
            cancellationToken);

        if (string.Equals(
                profile.SecretSource,
                LlmSecretSources.EncryptedStore,
                StringComparison.OrdinalIgnoreCase))
        {
            bool secretStillUsed =
                remainingProfiles.Any(
                    candidate =>
                        string.Equals(
                            candidate.SecretSource,
                            LlmSecretSources.EncryptedStore,
                            StringComparison.OrdinalIgnoreCase)
                        &&
                        string.Equals(
                            candidate.SecretReference,
                            profile.SecretReference,
                            StringComparison.Ordinal));

            if (!secretStillUsed)
            {
                try
                {
                    await _secretStore.DeleteAsync(
                        profile.SecretReference,
                        cancellationToken);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException(
                        "LLM profile was deleted, "
                        + "but its encrypted secret "
                        + "could not be removed.",
                        exception);
                }
            }
        }

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