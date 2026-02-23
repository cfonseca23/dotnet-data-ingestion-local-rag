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
    public string ChatModel { get; init; } = "llama3.2:latest";
    public string EmbeddingModel { get; init; } = "all-minilm";
    public string MarkItDownEndpoint { get; init; } = "http://localhost:3001/mcp";
    public int EmbeddingDimensions { get; init; } = 384;
    public int MaxTokensPerChunk { get; init; } = 2000;
    public int OverlapTokens { get; init; } = 0;
    public TimeSpan HttpTimeout { get; init; } = TimeSpan.FromMinutes(5);
    public string CollectionName { get; init; } = "data";
    public string SearchPattern { get; init; } = "*.md";
    public int TopResults { get; init; } = 5;
}
