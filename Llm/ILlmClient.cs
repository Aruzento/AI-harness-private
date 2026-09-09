using MyAgent.Messages;

namespace MyAgent.Llm;

public interface ILlmClient
{
    Task<LlmResponse> SendAsync(
        IReadOnlyList<Message> messages);
}