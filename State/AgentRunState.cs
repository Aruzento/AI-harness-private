namespace MyAgent.State;

public class AgentRunState
{
    public int StepCount { get; private set; }

    public int ToolCallCount { get; private set; }

    public int DeniedToolCallCount { get; private set; }

    public void BeginStep()
    {
        StepCount++;
    }

    public void RegisterToolCall()
    {
        ToolCallCount++;
    }

    public void RegisterDeniedToolCall()
    {
        DeniedToolCallCount++;
    }
}