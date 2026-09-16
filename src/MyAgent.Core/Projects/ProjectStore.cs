using System.Text.Json;

namespace MyAgent.Projects;

public sealed class ProjectStore
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
                Environment.SpecialFolder
                    .LocalApplicationData),
            "MyAgent",
            "projects.json");

    public ProjectStore(
        string? filePath = null)
    {
        _filePath =
            string.IsNullOrWhiteSpace(
                filePath)
                ? DefaultFilePath
                : Path.GetFullPath(
                    filePath);
    }

    public async Task<ProjectCatalog>
        LoadAsync(
            CancellationToken cancellationToken = default)
    {
        if (!File.Exists(
                _filePath))
        {
            return new ProjectCatalog();
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

            ProjectDocument? document =
                await JsonSerializer
                    .DeserializeAsync<
                        ProjectDocument>(
                        stream,
                        JsonOptions,
                        cancellationToken);

            if (document is null)
            {
                throw new InvalidOperationException(
                    $"Invalid project file: {_filePath}");
            }

            if (document.SchemaVersion !=
                CurrentSchemaVersion)
            {
                throw new InvalidOperationException(
                    "Unsupported project schema version "
                    + $"{document.SchemaVersion}. "
                    + $"Expected {CurrentSchemaVersion}.");
            }

            var catalog =
                new ProjectCatalog
                {
                    ActiveProjectId =
                        document.ActiveProjectId,

                    Projects =
                        document.Projects
                        ?? Array.Empty<AgentProject>()
                };

            ValidateCatalog(
                catalog);

            return catalog;
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Invalid project file: {_filePath}",
                exception);
        }
    }

    public async Task SaveAsync(
        ProjectCatalog catalog,
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
            new ProjectDocument
            {
                SchemaVersion =
                    CurrentSchemaVersion,

                ActiveProjectId =
                    catalog.ActiveProjectId,

                Projects =
                    catalog.Projects
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
                await JsonSerializer
                    .SerializeAsync(
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
        ProjectCatalog catalog)
    {
        var projectIds =
            new HashSet<string>(
                StringComparer.Ordinal);

        foreach (AgentProject project
                 in catalog.Projects)
        {
            if (string.IsNullOrWhiteSpace(
                    project.Id))
            {
                throw new InvalidOperationException(
                    "Project has no ID.");
            }

            if (!projectIds.Add(
                    project.Id))
            {
                throw new InvalidOperationException(
                    "Duplicate project ID: "
                    + project.Id);
            }

            if (string.IsNullOrWhiteSpace(
                    project.Name))
            {
                throw new InvalidOperationException(
                    "Project has no name: "
                    + project.Id);
            }

            if (string.IsNullOrWhiteSpace(
                    project.WorkspacePath))
            {
                throw new InvalidOperationException(
                    "Project has no workspace path: "
                    + project.Id);
            }

            if (!Path.IsPathFullyQualified(
                    project.WorkspacePath))
            {
                throw new InvalidOperationException(
                    "Project workspace path "
                    + "must be absolute: "
                    + project.Id);
            }

            if (project.CreatedAtUtc ==
                default)
            {
                throw new InvalidOperationException(
                    "Project has no creation time: "
                    + project.Id);
            }

            if (project.UpdatedAtUtc ==
                default)
            {
                throw new InvalidOperationException(
                    "Project has no update time: "
                    + project.Id);
            }

            if (project.UpdatedAtUtc <
                project.CreatedAtUtc)
            {
                throw new InvalidOperationException(
                    "Project update time is earlier "
                    + "than creation time: "
                    + project.Id);
            }
        }

        if (catalog.ActiveProjectId
            is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(
                catalog.ActiveProjectId))
        {
            throw new InvalidOperationException(
                "Active project ID cannot be empty.");
        }

        if (!projectIds.Contains(
                catalog.ActiveProjectId))
        {
            throw new InvalidOperationException(
                "Active project does not exist: "
                + catalog.ActiveProjectId);
        }
    }

    private sealed class ProjectDocument
    {
        public int SchemaVersion
        {
            get;
            set;
        }

        public string? ActiveProjectId
        {
            get;
            set;
        }

        public AgentProject[]? Projects
        {
            get;
            set;
        }
    }
}