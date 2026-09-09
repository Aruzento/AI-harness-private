using MyAgent.Messages;

namespace MyAgent.Llm;

public interface ILlmClient
{
    Task<string> SendAsync(
        IReadOnlyList<Message> messages);
}