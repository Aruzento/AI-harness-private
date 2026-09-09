using MyAgent.Llm;

namespace MyAgent.Agent;

public class Agent
{
    private readonly ILlmClient _llmClient;

    public Agent(ILlmClient llmClient)
    {
        _llmClient = llmClient;
    }

    public async Task<string> RunAsync(string task)
    {
        return await _llmClient.SendAsync(task);
    }
}