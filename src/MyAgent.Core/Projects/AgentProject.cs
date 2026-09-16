namespace MyAgent.Projects;

public sealed class AgentProject
{
    public string Id { get; init; } =
        string.Empty;

    public string Name { get; init; } =
        string.Empty;

    public string WorkspacePath { get; init; } =
        string.Empty;

    public DateTimeOffset CreatedAtUtc
    {
        get;
        init;
    }

    public DateTimeOffset UpdatedAtUtc
    {
        get;
        init;
    }
}