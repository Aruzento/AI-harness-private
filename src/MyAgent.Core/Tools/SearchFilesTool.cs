using System.Text.Json;
using MyAgent.Workspace;

namespace MyAgent.Tools;

public class SearchFilesTool : ITool
{
    private const int MaxResults =
        50;

    private const long MaxFileSize =
        1_000_000;

    private readonly AgentWorkspace _workspace;

    public string Name =>
        "search_files";

    public string Description =>
        "Ищет текст во всех подходящих файлах "
        + "внутри workspace и возвращает путь, "
        + "номер строки и найденную строку.";

    public JsonElement ParametersSchema =>
        JsonSerializer.SerializeToElement(
            new
            {
                type = "object",

                properties = new
                {
                    query = new
                    {
                        type = "string",

                        description =
                            "Текст для поиска."
                    },

                    path = new
                    {
                        type = "string",

                        description =
                            "Папка относительно workspace. "
                            + "По умолчанию корень workspace."
                    }
                },

                required = new[]
                {
                    "query"
                }
            });

    public SearchFilesTool(
        AgentWorkspace workspace)
    {
        _workspace =
            workspace;
    }

    public async Task<ToolResult> ExecuteAsync(
        JsonElement arguments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!arguments.TryGetProperty(
                    "query",
                    out JsonElement queryElement)
                ||
                queryElement.ValueKind !=
                    JsonValueKind.String)
            {
                return ToolResult.Fail(
                    "Missing parameter: query.");
            }

            string query =
                queryElement.GetString()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(query))
            {
                return ToolResult.Fail(
                    "query cannot be empty.");
            }

            string path =
                ".";

            if (arguments.TryGetProperty(
                    "path",
                    out JsonElement pathElement)
                &&
                pathElement.ValueKind ==
                    JsonValueKind.String)
            {
                path =
                    pathElement.GetString()
                    ?? ".";
            }

            string rootPath =
                _workspace.ResolvePath(
                    path);

            if (!Directory.Exists(rootPath))
            {
                return ToolResult.Fail(
                    $"Directory not found: {path}");
            }

            var enumerationOptions =
                new EnumerationOptions
                {
                    RecurseSubdirectories =
                        true,

                    IgnoreInaccessible =
                        true,

                    AttributesToSkip =
                        FileAttributes.ReparsePoint
                };

            var results =
                new List<string>();

            foreach (string filePath
                     in Directory.EnumerateFiles(
                         rootPath,
                         "*",
                         enumerationOptions))
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                string relativePath =
                    Path.GetRelativePath(
                        _workspace.RootPath,
                        filePath);

                if (ShouldSkip(
                        relativePath))
                {
                    continue;
                }

                var fileInfo =
                    new FileInfo(
                        filePath);

                if (fileInfo.Length >
                    MaxFileSize)
                {
                    continue;
                }

                string content =
                    await File.ReadAllTextAsync(
                        filePath,
                        cancellationToken);

                if (content.Contains(
                        '\0'))
                {
                    continue;
                }

                string normalized =
                    content
                        .Replace(
                            "\r\n",
                            "\n")
                        .Replace(
                            '\r',
                            '\n');

                string[] lines =
                    normalized.Split(
                        '\n');

                for (int lineIndex = 0;
                     lineIndex < lines.Length;
                     lineIndex++)
                {
                    if (lines[lineIndex]
                        .IndexOf(
                            query,
                            StringComparison
                                .OrdinalIgnoreCase)
                        < 0)
                    {
                        continue;
                    }

                    results.Add(
                        relativePath
                        + ":"
                        + (lineIndex + 1)
                        + ": "
                        + lines[lineIndex]
                            .Trim());

                    if (results.Count >=
                        MaxResults)
                    {
                        return ToolResult.Ok(
                            string.Join(
                                Environment.NewLine,
                                results)
                            + Environment.NewLine
                            + $"(limited to {MaxResults} results)");
                    }
                }
            }

            if (results.Count == 0)
            {
                return ToolResult.Ok(
                    "(no matches)");
            }

            return ToolResult.Ok(
                string.Join(
                    Environment.NewLine,
                    results));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return ToolResult.Fail(
                exception.Message);
        }
    }

    private static bool ShouldSkip(
        string relativePath)
    {
        string normalized =
            relativePath.Replace(
                '\\',
                '/');

        string[] parts =
            normalized.Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries);

        return parts.Any(
            part =>
                part.Equals(
                    ".git",
                    StringComparison.OrdinalIgnoreCase)
                ||
                part.Equals(
                    "bin",
                    StringComparison.OrdinalIgnoreCase)
                ||
                part.Equals(
                    "obj",
                    StringComparison.OrdinalIgnoreCase)
                ||
                part.Equals(
                    ".vs",
                    StringComparison.OrdinalIgnoreCase));
    }
}