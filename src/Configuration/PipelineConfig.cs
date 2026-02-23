namespace DataIngest.Configuration;

/// <summary>
/// Configuration record for the data ingestion pipeline.
/// Follows Single Responsibility - only holds configuration values.
/// </summary>
public record PipelineConfig
{
    public string DatabasePath { get; init; } = "vectors.db";
    public string DataPath { get; init; } = "./data";
    public string OllamaEndpoint { get; init; } = "http://localhost:11434";
    public string ChatModel { get; init; } = "qwen3:1.7b";
    public string EmbeddingModel { get; init; } = "nomic-embed-text";
    public string MarkItDownEndpoint { get; init; } = "http://localhost:3001/mcp";
    public int EmbeddingDimensions { get; init; } = 768;
    public int MaxTokensPerChunk { get; init; } = 2000;
    public int OverlapTokens { get; init; } = 200;
    public TimeSpan HttpTimeout { get; init; } = TimeSpan.FromMinutes(5);
    public string CollectionName { get; init; } = "data";
    public string SearchPattern { get; init; } = "*.md";
    public int TopResults { get; init; } = 5;
}
