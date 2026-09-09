using MyAgent.Guardrails;
using MyAgent.Llm;
using MyAgent.Messages;
using MyAgent.State;
using MyAgent.Tools;

namespace MyAgent.Agent;

public class Agent
{
    private readonly ILlmClient _llmClient;
    private readonly ToolRegistry _toolRegistry;
    private readonly AgentPolicy _policy;
    private readonly IToolApproval _toolApproval;
    private readonly ConversationHistory _history;

    public AgentRunState? LastRunState
    {
        get;
        private set;
    }

    public Agent(
        ILlmClient llmClient,
        ToolRegistry toolRegistry,
        AgentPolicy policy,
        IToolApproval toolApproval,
        string systemPrompt)
    {
        _llmClient = llmClient;
        _toolRegistry = toolRegistry;
        _policy = policy;
        _toolApproval = toolApproval;

        _history =
            new ConversationHistory();

        _history.AddSystem(
            systemPrompt);
    }

    public async Task<LlmResponse> RunAsync(
        string task)
    {
        var state =
            new AgentRunState();

        LastRunState =
            state;

        _history.AddUser(task);

        while (state.StepCount <
               _policy.MaxSteps)
        {
            state.BeginStep();

            Console.WriteLine(
                $"[Agent step: {state.StepCount}]");

            LlmResponse response =
                await _llmClient.SendAsync(
                    _history.Messages,
                    _toolRegistry.Tools);

            if (!response.HasToolCalls)
            {
                _history.AddAssistant(
                    response.Content);

                return response;
            }

            _history.AddAssistant(
                response.Content,
                response.ToolCalls);

            foreach (ToolCall toolCall
                     in response.ToolCalls)
            {
                state.RegisterToolCall();

                if (state.ToolCallCount >
                    _policy.MaxToolCalls)
                {
                    throw new InvalidOperationException(
                        "Agent exceeded maximum "
                        + "number of tool calls: "
                        + $"{_policy.MaxToolCalls}.");
                }

                Console.WriteLine(
                    $"[Tool call: {toolCall.Name}]");

                ToolResult toolResult;

                if (_policy.RequiresApproval(
                        toolCall.Name))
                {
                    bool approved =
                        await _toolApproval.ApproveAsync(
                            toolCall);

                    if (!approved)
                    {
                        state.RegisterDeniedToolCall();

                        toolResult =
                            ToolResult.Fail(
                                "Tool execution denied by user.");
                    }
                    else
                    {
                        toolResult =
                            await _toolRegistry.ExecuteAsync(
                                toolCall.Name,
                                toolCall.Arguments);
                    }
                }
                else
                {
                    toolResult =
                        await _toolRegistry.ExecuteAsync(
                            toolCall.Name,
                            toolCall.Arguments);
                }

                string toolContent =
                    toolResult.Success
                        ? toolResult.Content
                        : $"ERROR: {toolResult.Error}";

                Console.WriteLine(
                    $"[Tool result: {toolContent}]");

                _history.AddTool(
                    toolCall.Id,
                    toolContent);
            }
        }

        throw new InvalidOperationException(
            "Agent exceeded maximum "
            + "number of steps: "
            + $"{_policy.MaxSteps}.");
    }
}