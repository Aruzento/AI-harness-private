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

    private readonly IAgentObserver _observer;

    public AgentRunState? LastRunState
    {
        get;
        private set;
    }

    public Message[]
        CreatePersistentHistorySnapshot()
    {
        return _history
            .CreatePersistentSnapshot();
    }

    public Agent(
        ILlmClient llmClient,
        ToolRegistry toolRegistry,
        AgentPolicy policy,
        IToolApproval toolApproval,
        IAgentObserver observer,
        string systemPrompt,
        IReadOnlyList<Message>? initialMessages = null)
    {
        _llmClient = llmClient;
        _toolRegistry = toolRegistry;
        _policy = policy;
        _toolApproval = toolApproval;
        _observer = observer;

        _history =
            new ConversationHistory();

        _history.AddSystem(
            systemPrompt);

        if (initialMessages is not null)
        {
            _history.AddPersistentMessages(
                initialMessages);
        }
    }

    public async Task<LlmResponse> RunAsync(
        string task,
        CancellationToken cancellationToken = default)
    {
        var state =
            new AgentRunState();

        cancellationToken
            .ThrowIfCancellationRequested();

        LastRunState =
            state;

        _history.AddUser(task);

        while (state.StepCount <
               _policy.MaxSteps)
        {
            state.BeginStep();

            _observer.OnStepStarted(
                state.StepCount);

            LlmResponse response =
                await _llmClient.SendAsync(
                    _history.Messages,
                    _toolRegistry.Tools,
                    cancellationToken);

            if (!response.HasToolCalls)
            {
                if (string.IsNullOrWhiteSpace(
                        response.Content))
                {
                    _observer.OnEmptyResponseRetry();

                    continue;
                }

                _history.AddAssistant(
                    response.Content);

                return response;
            }

            _history.AddAssistant(
                response.Content,
                response.ToolCalls);

            for (int toolIndex = 0;
                toolIndex < response.ToolCalls.Count;
                toolIndex++)
            {
                ToolCall toolCall =
                    response.ToolCalls[
                        toolIndex];

                try
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    state.RegisterToolCall();

                    if (state.ToolCallCount >
                        _policy.MaxToolCalls)
                    {
                        throw new InvalidOperationException(
                            "Agent exceeded maximum "
                            + "number of tool calls: "
                            + $"{_policy.MaxToolCalls}.");
                    }

                    _observer.OnToolCall(
                        toolCall);

                    ToolResult toolResult;

                    if (_policy.RequiresApproval(
                            toolCall.Name))
                    {
                        ToolApprovalPreview approvalPreview =
                            await _toolRegistry
                                .CreateApprovalPreviewAsync(
                                    toolCall.Name,
                                    toolCall.Arguments,
                                    cancellationToken);

                        var approvalRequest =
                            new ToolApprovalRequest(
                                toolCall,
                                approvalPreview);

                        bool approved =
                            await _toolApproval.ApproveAsync(
                                approvalRequest,
                                cancellationToken);

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
                                await _toolRegistry
                                    .ExecuteApprovedAsync(
                                        toolCall.Name,
                                        toolCall.Arguments,
                                        approvalPreview,
                                        cancellationToken);
                        }
                    }
                    else
                    {
                        toolResult =
                            await _toolRegistry.ExecuteAsync(
                                toolCall.Name,
                                toolCall.Arguments,
                                cancellationToken);
                    }

                    string toolContent =
                        toolResult.Success
                            ? toolResult.Content
                            : $"ERROR: {toolResult.Error}";

                    _observer.OnToolResult(
                        toolCall,
                        toolResult);

                    _history.AddTool(
                        toolCall.Id,
                        toolContent);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    _history.AddTool(
                        toolCall.Id,
                        "ERROR: Tool execution cancelled by user.");

                    for (int remainingIndex =
                            toolIndex + 1;
                        remainingIndex <
                            response.ToolCalls.Count;
                        remainingIndex++)
                    {
                        ToolCall remainingCall =
                            response.ToolCalls[
                                remainingIndex];

                        _history.AddTool(
                            remainingCall.Id,
                            "ERROR: Tool execution cancelled before execution.");
                    }

                    throw;
                }
            }
        }

        throw new InvalidOperationException(
            "Agent exceeded maximum "
            + "number of steps: "
            + $"{_policy.MaxSteps}.");
    }
}