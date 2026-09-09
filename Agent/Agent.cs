using MyAgent.Llm;
using MyAgent.Messages;
using MyAgent.Tools;

namespace MyAgent.Agent;

public class Agent
{
    private const int MaxSteps = 10;

    private readonly ILlmClient _llmClient;
    private readonly ToolRegistry _toolRegistry;
    private readonly ConversationHistory _history;

    public Agent(
        ILlmClient llmClient,
        ToolRegistry toolRegistry,
        string systemPrompt)
    {
        _llmClient = llmClient;
        _toolRegistry = toolRegistry;

        _history =
            new ConversationHistory();

        _history.AddSystem(
            systemPrompt);
    }

    public async Task<LlmResponse> RunAsync(
        string task)
    {
        _history.AddUser(task);

        for (int step = 1;
             step <= MaxSteps;
             step++)
        {
            Console.WriteLine(
                $"[Agent step: {step}]");

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
                Console.WriteLine(
                    $"[Tool call: {toolCall.Name}]");

                ToolResult toolResult =
                    await _toolRegistry.ExecuteAsync(
                        toolCall.Name,
                        toolCall.Arguments);

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
            $"Agent exceeded maximum number of steps: {MaxSteps}.");
    }
}