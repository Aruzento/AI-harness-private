using System.Text.Json;

namespace MyAgent.Configuration;

public sealed class LlmProfileStore
{
    private const int CurrentSchemaVersion =
        1;

    private readonly string _filePath;

    private static readonly JsonSerializerOptions
        JsonOptions =
            new()
            {
                WriteIndented = true,

                PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase,

                PropertyNameCaseInsensitive =
                    true
            };

    public static string DefaultFilePath =>
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "MyAgent",
            "llm-profiles.json");

    public LlmProfileStore(
        string? filePath = null)
    {
        _filePath =
            string.IsNullOrWhiteSpace(filePath)
                ? DefaultFilePath
                : Path.GetFullPath(filePath);
    }

    public async Task<LlmProfileCatalog>
        LoadAsync(
            CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return new LlmProfileCatalog();
        }

        try
        {
            await using FileStream stream =
                new(
                    _filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 4096,
                    useAsync: true);

            ProfileDocument? document =
                await JsonSerializer.DeserializeAsync<
                    ProfileDocument>(
                    stream,
                    JsonOptions,
                    cancellationToken);

            if (document is null)
            {
                throw new InvalidOperationException(
                    $"Invalid LLM profile file: {_filePath}");
            }

            if (document.SchemaVersion !=
                CurrentSchemaVersion)
            {
                throw new InvalidOperationException(
                    "Unsupported LLM profile schema version "
                    + $"{document.SchemaVersion}. "
                    + $"Expected {CurrentSchemaVersion}.");
            }

            var catalog =
                new LlmProfileCatalog
                {
                    ActiveProfileId =
                        document.ActiveProfileId,

                    Profiles =
                        document.Profiles
                        ?? Array.Empty<LlmProfile>()
                };

            ValidateCatalog(
                catalog);

            return catalog;
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Invalid LLM profile file: {_filePath}",
                exception);
        }
    }

    public async Task SaveAsync(
        LlmProfileCatalog catalog,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            catalog);

        ValidateCatalog(
            catalog);

        string? directory =
            Path.GetDirectoryName(
                _filePath);

        if (!string.IsNullOrWhiteSpace(
                directory))
        {
            Directory.CreateDirectory(
                directory);
        }

        var document =
            new ProfileDocument
            {
                SchemaVersion =
                    CurrentSchemaVersion,

                ActiveProfileId =
                    catalog.ActiveProfileId,

                Profiles =
                    catalog.Profiles
            };

        string temporaryPath =
            _filePath
            + "."
            + Guid.NewGuid().ToString("N")
            + ".tmp";

        try
        {
            await using (
                FileStream stream =
                    new(
                        temporaryPath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        bufferSize: 4096,
                        useAsync: true))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    document,
                    JsonOptions,
                    cancellationToken);

                await stream.FlushAsync(
                    cancellationToken);
            }

            cancellationToken
                .ThrowIfCancellationRequested();

            File.Move(
                temporaryPath,
                _filePath,
                overwrite: true);
        }
        catch
        {
            if (File.Exists(
                    temporaryPath))
            {
                File.Delete(
                    temporaryPath);
            }

            throw;
        }
    }

    private static void ValidateCatalog(
        LlmProfileCatalog catalog)
    {
        var profileIds =
            new HashSet<string>(
                StringComparer.Ordinal);

        foreach (LlmProfile profile
                 in catalog.Profiles)
        {
            if (string.IsNullOrWhiteSpace(
                    profile.Id))
            {
                throw new InvalidOperationException(
                    "LLM profile has no ID.");
            }

            if (!profileIds.Add(
                    profile.Id))
            {
                throw new InvalidOperationException(
                    "Duplicate LLM profile ID: "
                    + profile.Id);
            }
        }

        if (catalog.ActiveProfileId is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(
                catalog.ActiveProfileId))
        {
            throw new InvalidOperationException(
                "Active LLM profile ID cannot be empty.");
        }

        if (!profileIds.Contains(
                catalog.ActiveProfileId))
        {
            throw new InvalidOperationException(
                "Active LLM profile does not exist: "
                + catalog.ActiveProfileId);
        }
    }

    private sealed class ProfileDocument
    {
        public int SchemaVersion
        {
            get;
            set;
        }

        public string? ActiveProfileId
        {
            get;
            set;
        }

        public LlmProfile[]? Profiles
        {
            get;
            set;
        }
    }
}