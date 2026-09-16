namespace MyAgent.Projects;

public sealed class ProjectManager
{
    private readonly ProjectStore
        _projectStore;

    public ProjectManager(
        ProjectStore projectStore)
    {
        _projectStore =
            projectStore
            ?? throw new ArgumentNullException(
                nameof(projectStore));
    }

    public Task<ProjectCatalog> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        return _projectStore.LoadAsync(
            cancellationToken);
    }

    public async Task<ProjectCatalog>
        AddAsync(
            string name,
            string workspacePath,
            bool makeActive = true,
            CancellationToken cancellationToken = default)
    {
        string normalizedName =
            ValidateName(
                name);

        string normalizedWorkspacePath =
            ValidateExistingWorkspacePath(
                workspacePath);

        ProjectCatalog current =
            await _projectStore.LoadAsync(
                cancellationToken);

        DateTimeOffset now =
            DateTimeOffset.UtcNow;

        var project =
            new AgentProject
            {
                Id =
                    "project-"
                    + Guid.NewGuid()
                        .ToString("N"),

                Name =
                    normalizedName,

                WorkspacePath =
                    normalizedWorkspacePath,

                CreatedAtUtc =
                    now,

                UpdatedAtUtc =
                    now
            };

        AgentProject[] projects =
            current.Projects
                .Append(
                    project)
                .ToArray();

        string? activeProjectId =
            makeActive
            || current.ActiveProjectId is null
                ? project.Id
                : current.ActiveProjectId;

        var updated =
            new ProjectCatalog
            {
                ActiveProjectId =
                    activeProjectId,

                Projects =
                    projects
            };

        await _projectStore.SaveAsync(
            updated,
            cancellationToken);

        return updated;
    }

    public async Task<ProjectCatalog>
        SetActiveAsync(
            string projectId,
            CancellationToken cancellationToken = default)
    {
        ValidateProjectId(
            projectId);

        ProjectCatalog current =
            await _projectStore.LoadAsync(
                cancellationToken);

        EnsureProjectExists(
            current,
            projectId);

        if (string.Equals(
                current.ActiveProjectId,
                projectId,
                StringComparison.Ordinal))
        {
            return current;
        }

        var updated =
            new ProjectCatalog
            {
                ActiveProjectId =
                    projectId,

                Projects =
                    current.Projects
            };

        await _projectStore.SaveAsync(
            updated,
            cancellationToken);

        return updated;
    }

    public async Task<ProjectCatalog>
        RenameAsync(
            string projectId,
            string newName,
            CancellationToken cancellationToken = default)
    {
        ValidateProjectId(
            projectId);

        string normalizedName =
            ValidateName(
                newName);

        ProjectCatalog current =
            await _projectStore.LoadAsync(
                cancellationToken);

        AgentProject existing =
            GetProject(
                current,
                projectId);

        if (string.Equals(
                existing.Name,
                normalizedName,
                StringComparison.Ordinal))
        {
            return current;
        }

        var updatedProject =
            new AgentProject
            {
                Id =
                    existing.Id,

                Name =
                    normalizedName,

                WorkspacePath =
                    existing.WorkspacePath,

                CreatedAtUtc =
                    existing.CreatedAtUtc,

                UpdatedAtUtc =
                    DateTimeOffset.UtcNow
            };

        ProjectCatalog updated =
            ReplaceProject(
                current,
                updatedProject);

        await _projectStore.SaveAsync(
            updated,
            cancellationToken);

        return updated;
    }

    public async Task<ProjectCatalog>
        RelinkWorkspaceAsync(
            string projectId,
            string workspacePath,
            CancellationToken cancellationToken = default)
    {
        ValidateProjectId(
            projectId);

        string normalizedWorkspacePath =
            ValidateExistingWorkspacePath(
                workspacePath);

        ProjectCatalog current =
            await _projectStore.LoadAsync(
                cancellationToken);

        AgentProject existing =
            GetProject(
                current,
                projectId);

        if (string.Equals(
                existing.WorkspacePath,
                normalizedWorkspacePath,
                StringComparison.OrdinalIgnoreCase))
        {
            return current;
        }

        var updatedProject =
            new AgentProject
            {
                Id =
                    existing.Id,

                Name =
                    existing.Name,

                WorkspacePath =
                    normalizedWorkspacePath,

                CreatedAtUtc =
                    existing.CreatedAtUtc,

                UpdatedAtUtc =
                    DateTimeOffset.UtcNow
            };

        ProjectCatalog updated =
            ReplaceProject(
                current,
                updatedProject);

        await _projectStore.SaveAsync(
            updated,
            cancellationToken);

        return updated;
    }

    public async Task<ProjectCatalog>
        RemoveAsync(
            string projectId,
            CancellationToken cancellationToken = default)
    {
        ValidateProjectId(
            projectId);

        ProjectCatalog current =
            await _projectStore.LoadAsync(
                cancellationToken);

        AgentProject existing =
            GetProject(
                current,
                projectId);

        AgentProject[] remainingProjects =
            current.Projects
                .Where(
                    project =>
                        !string.Equals(
                            project.Id,
                            existing.Id,
                            StringComparison.Ordinal))
                .ToArray();

        string? activeProjectId =
            current.ActiveProjectId;

        if (string.Equals(
                activeProjectId,
                projectId,
                StringComparison.Ordinal))
        {
            activeProjectId =
                remainingProjects
                    .FirstOrDefault()
                    ?.Id;
        }

        var updated =
            new ProjectCatalog
            {
                ActiveProjectId =
                    activeProjectId,

                Projects =
                    remainingProjects
            };

        await _projectStore.SaveAsync(
            updated,
            cancellationToken);

        return updated;
    }

    private static ProjectCatalog
        ReplaceProject(
            ProjectCatalog current,
            AgentProject updatedProject)
    {
        return new ProjectCatalog
        {
            ActiveProjectId =
                current.ActiveProjectId,

            Projects =
                current.Projects
                    .Select(
                        project =>
                            string.Equals(
                                project.Id,
                                updatedProject.Id,
                                StringComparison.Ordinal)
                                ? updatedProject
                                : project)
                    .ToArray()
        };
    }

    private static AgentProject GetProject(
        ProjectCatalog catalog,
        string projectId)
    {
        AgentProject? project =
            catalog.Projects
                .FirstOrDefault(
                    candidate =>
                        string.Equals(
                            candidate.Id,
                            projectId,
                            StringComparison.Ordinal));

        if (project is null)
        {
            throw new InvalidOperationException(
                "Project does not exist: "
                + projectId);
        }

        return project;
    }

    private static void EnsureProjectExists(
        ProjectCatalog catalog,
        string projectId)
    {
        _ =
            GetProject(
                catalog,
                projectId);
    }

    private static void ValidateProjectId(
        string projectId)
    {
        if (string.IsNullOrWhiteSpace(
                projectId))
        {
            throw new ArgumentException(
                "Project ID cannot be empty.",
                nameof(projectId));
        }
    }

    private static string ValidateName(
        string name)
    {
        if (string.IsNullOrWhiteSpace(
                name))
        {
            throw new ArgumentException(
                "Project name cannot be empty.",
                nameof(name));
        }

        return name.Trim();
    }

    private static string
        ValidateExistingWorkspacePath(
            string workspacePath)
    {
        if (string.IsNullOrWhiteSpace(
                workspacePath))
        {
            throw new ArgumentException(
                "Workspace path cannot be empty.",
                nameof(workspacePath));
        }

        string trimmedPath =
            workspacePath.Trim();

        if (!Path.IsPathFullyQualified(
                trimmedPath))
        {
            throw new ArgumentException(
                "Workspace path must be absolute.",
                nameof(workspacePath));
        }

        string fullPath =
            Path.GetFullPath(
                trimmedPath);

        if (!Directory.Exists(
                fullPath))
        {
            throw new DirectoryNotFoundException(
                "Workspace directory does not exist: "
                + fullPath);
        }

        return fullPath;
    }
}