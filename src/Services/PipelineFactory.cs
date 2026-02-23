using DataIngest.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DataIngestion;
using Microsoft.Extensions.DataIngestion.Chunkers;
using Microsoft.Extensions.Logging;
using Microsoft.ML.Tokenizers;
using Microsoft.SemanticKernel.Connectors.SqliteVec;
using OllamaSharp;

namespace DataIngest.Services;

/// <summary>
/// Factory for creating pipeline components.
/// Follows Dependency Inversion - abstracts component creation.
/// Follows Open/Closed - extend by creating new factory methods.
/// </summary>
public class PipelineFactory : IDisposable
{
    private readonly PipelineConfig _config;
    private readonly ILoggerFactory _loggerFactory;
    private readonly List<IDisposable> _disposables = [];

    public PipelineFactory(PipelineConfig config, ILoggerFactory loggerFactory)
    {
        _config = config;
        _loggerFactory = loggerFactory;
    }

    public IChatClient CreateChatClient()
    {
        var httpClient = new HttpClient 
        { 
            BaseAddress = new Uri(_config.OllamaEndpoint), 
            Timeout = _config.HttpTimeout 
        };
        _disposables.Add(httpClient);
        return new OllamaApiClient(httpClient, _config.ChatModel);
    }

    public IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator()
    {
        var httpClient = new HttpClient 
        { 
            BaseAddress = new Uri(_config.OllamaEndpoint), 
            Timeout = _config.HttpTimeout 
        };
        _disposables.Add(httpClient);
        return new OllamaApiClient(httpClient, _config.EmbeddingModel);
    }

    public IngestionDocumentReader CreateDocumentReader()
    {
        return new MarkItDownMcpReader(new Uri(_config.MarkItDownEndpoint));
    }

    public IngestionChunker<string> CreateChunker(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        var chunkerOptions = new IngestionChunkerOptions(TiktokenTokenizer.CreateForModel("gpt-4"))
        {
            MaxTokensPerChunk = _config.MaxTokensPerChunk,
            OverlapTokens = _config.OverlapTokens
        };
        return new SemanticSimilarityChunker(embeddingGenerator, chunkerOptions);
    }

    public SqliteVectorStore CreateVectorStore(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        var store = new SqliteVectorStore(
            $"Data Source={_config.DatabasePath};Pooling=false",
            new() { EmbeddingGenerator = embeddingGenerator });
        _disposables.Add(store);
        return store;
    }

    public VectorStoreWriter<string> CreateWriter(SqliteVectorStore vectorStore)
    {
        var writer = new VectorStoreWriter<string>(
            vectorStore, 
            dimensionCount: _config.EmbeddingDimensions, 
            new VectorStoreWriterOptions { CollectionName = _config.CollectionName });
        _disposables.Add(writer);
        return writer;
    }

    public IngestionPipeline<string> CreatePipeline(
        IngestionDocumentReader reader,
        IngestionChunker<string> chunker,
        VectorStoreWriter<string> writer,
        IChatClient chatClient)
    {
        var enricherOptions = new EnricherOptions(chatClient) 
        { 
            LoggerFactory = _loggerFactory,
            BatchSize = 1 // Ollama returns 1 summary regardless of batch size — process one chunk at a time
        };
        
        var pipeline = new IngestionPipeline<string>(reader, chunker, writer, loggerFactory: _loggerFactory)
        {
            DocumentProcessors = { new ImageAlternativeTextEnricher(enricherOptions) },
            ChunkProcessors = { new SummaryEnricher(enricherOptions) }
        };
        _disposables.Add(pipeline);
        return pipeline;
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        _disposables.Clear();
        GC.SuppressFinalize(this);
    }
}
