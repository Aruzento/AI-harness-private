using MyAgent.Llm;
using MyAgent.Messages;

namespace MyAgent.Agent;

public class Agent
{
    private readonly ILlmClient _llmClient;
    private readonly ConversationHistory _history;

    public Agent(
        ILlmClient llmClient,
        string systemPrompt)
    {
        _llmClient = llmClient;

        _history = new ConversationHistory();
        _history.AddSystem(systemPrompt);
    }

    public async Task<string> RunAsync(string task)
    {
        _history.AddUser(task);

        string answer =
            await _llmClient.SendAsync(
                _history.Messages);

        _history.AddAssistant(answer);

        return answer;
    }
}