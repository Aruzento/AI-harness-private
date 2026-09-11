namespace MyAgent.Guardrails;

public class AgentPolicy
{
    public int MaxSteps { get; }

    public int MaxToolCalls { get; }

    public AgentPolicy(
        int maxSteps,
        int maxToolCalls)
    {
        if (maxSteps <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxSteps));
        }

        if (maxToolCalls <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxToolCalls));
        }

        MaxSteps = maxSteps;
        MaxToolCalls = maxToolCalls;
    }

    public bool RequiresApproval(
        string toolName)
    {
        return toolName.Equals(
                "run_terminal",
                StringComparison.OrdinalIgnoreCase)
            ||
            toolName.Equals(
                "edit_file",
                StringComparison.OrdinalIgnoreCase)
            ||
            toolName.Equals(
                "write_file",
                StringComparison.OrdinalIgnoreCase);
    }
}