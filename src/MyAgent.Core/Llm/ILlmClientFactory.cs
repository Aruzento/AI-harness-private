using MyAgent.Configuration;

namespace MyAgent.Llm;

public interface ILlmClientFactory
{
    Task<ILlmClient> CreateAsync(
        LlmProfile profile,
        CancellationToken cancellationToken = default);
}