namespace MyAgent.Llm;

public class LlmResponse
{
    public string Model { get; }

    public string Content { get; }

    public LlmResponse(
        string model,
        string content)
    {
        Model = model;
        Content = content;
    }
}