namespace MyAgent.Llm;

public interface ILlmClient
{
    Task<string> SendAsync(string prompt);
}