using MyAgent.Messages;
using MyAgent.Tools;

namespace MyAgent.Llm;

public interface ILlmClient
{
    Task<LlmResponse> SendAsync(
        IReadOnlyList<Message> messages,
        IReadOnlyCollection<ITool> tools);
}