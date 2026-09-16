namespace MyAgent.Projects;

public sealed class ProjectCatalog
{
    public string? ActiveProjectId
    {
        get;
        init;
    }

    public AgentProject[] Projects
    {
        get;
        init;
    } =
        Array.Empty<AgentProject>();
}