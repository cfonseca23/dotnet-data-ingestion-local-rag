# Microsoft Extensions DataIngestion

## What is DataIngestion?

Microsoft Extensions DataIngestion is a powerful library designed for processing, chunking, and enriching documents before storing them in vector databases for semantic search and retrieval-augmented generation (RAG) applications. It provides a modular, pipeline-based architecture that allows developers to build sophisticated document processing workflows with minimal code.

The library is designed to work seamlessly with the broader .NET AI ecosystem, including Semantic Kernel, Microsoft.Extensions.AI, and various vector store implementations. It follows the same design principles as other Microsoft.Extensions libraries, making it familiar and easy to integrate into existing .NET applications.

## Core Architecture

The DataIngestion library is built around a pipeline concept where documents flow through a series of processing stages:

```
┌──────────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│   Document   │───▶│   Reader     │───▶│   Chunker    │───▶│  Enrichers   │
│   Source     │    │              │    │              │    │              │
└──────────────┘    └──────────────┘    └──────────────┘    └──────────────┘
                                                                    │
                                                                    ▼
                                                           ┌──────────────┐
                                                           │   Writer     │
                                                           │ (VectorStore)│
                                                           └──────────────┘
```

## Main Components

### Document Readers

Readers are responsible for extracting content from various document formats:

| Reader | Description | Formats |
|--------|-------------|---------|
| `MarkdownReader` | Native Markdown parser | .md |
| `MarkItDownMcpReader` | MCP-based document converter | .md, .pdf, .docx, .pptx, .xlsx |
| `HtmlReader` | HTML content extractor | .html, .htm |
| `TextReader` | Plain text reader | .txt |

### Chunkers

Chunkers divide documents into smaller, semantically meaningful pieces:

| Chunker | Description | Use Case |
|---------|-------------|----------|
| `SentenceChunker` | Splits by sentences | Simple documents |
| `ParagraphChunker` | Splits by paragraphs | Well-structured text |
| `SemanticSimilarityChunker` | Uses embeddings for semantic boundaries | Best quality |
| `TokenChunker` | Fixed token-based splitting | Precise token control |
| `RecursiveCharacterChunker` | Hierarchical splitting | Large documents |

### Enrichers

Enrichers add metadata and additional information to chunks:

| Enricher | Description | Output |
|----------|-------------|--------|
| `SummaryEnricher` | AI-generated summaries | `summary` field |
| `KeywordEnricher` | Extracted keywords | `keywords` field |
| `ImageAlternativeTextEnricher` | Alt text for images | `image_alt` field |
| `EntityEnricher` | Named entity extraction | `entities` field |
| `MetadataEnricher` | Custom metadata | User-defined fields |

### Writers

Writers persist processed chunks to storage systems:

| Writer | Description | Backend |
|--------|-------------|---------|
| `VectorStoreWriter` | Semantic Kernel vector stores | SQLite, PostgreSQL, Qdrant, etc. |
| `JsonFileWriter` | JSON file output | Local filesystem |
| `AzureAISearchWriter` | Azure AI Search index | Azure cloud |

## Complete Pipeline Example

```csharp
using Microsoft.Extensions.DataIngestion;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.SqliteVec;
using Microsoft.ML.Tokenizers;

// Create AI clients
IChatClient chatClient = new OllamaApiClient(
    new Uri("http://localhost:11434"), 
    "llama3.2"
);
IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = 
    new OllamaApiClient(new Uri("http://localhost:11434"), "all-minilm")
        .AsEmbeddingGenerator();

// Configure document reader (MarkItDown MCP for universal format support)
var reader = new MarkItDownMcpReader(new McpHttpClientTransport("http://localhost:3001"));

// Configure semantic chunker with GPT-4 tokenizer
var tokenizer = TiktokenTokenizer.CreateForModel("gpt-4");
var chunkerOptions = new IngestionChunkerOptions(tokenizer)
{
    MaxTokensPerChunk = 2000,
    OverlapTokens = 200,  // 10% overlap for context preservation
    MinTokensPerChunk = 100  // Avoid tiny chunks
};
var chunker = new SemanticSimilarityChunker<string>(embeddingGenerator, chunkerOptions);

// Configure enrichers
var enricherOptions = new EnricherOptions(chatClient);
var summaryEnricher = new SummaryEnricher(enricherOptions);
var imageEnricher = new ImageAlternativeTextEnricher(enricherOptions);

// Configure vector store writer
var vectorStore = new SqliteVectorStore(
    "Data Source=documents.db;Pooling=false",
    new() { EmbeddingGenerator = embeddingGenerator }
);
var writer = new VectorStoreWriter<string>(vectorStore);

// Build the pipeline
var pipeline = new IngestionPipelineBuilder<string>()
    .UseDocumentReader(reader)
    .UseChunker(chunker)
    .UseChunkProcessor(summaryEnricher)
    .UseChunkProcessor(imageEnricher)
    .UseWriter(writer)
    .Build();

// Process documents
var dataDir = new DirectoryInfo("./documents");
await foreach (var result in pipeline.ProcessAsync(dataDir, "*.md"))
{
    if (result.Succeeded)
        Console.WriteLine($"✓ Processed: {result.DocumentId}");
    else
        Console.WriteLine($"✗ Failed: {result.DocumentId} - {result.Exception?.Message}");
}
```

## Semantic Similarity Chunker Deep Dive

The `SemanticSimilarityChunker` is the most sophisticated chunking strategy. It uses embeddings to identify semantic boundaries in text, ensuring that each chunk contains coherent, related content.

### How It Works

1. **Sentence Splitting**: The text is first split into sentences
2. **Embedding Generation**: Each sentence is converted to an embedding vector
3. **Similarity Calculation**: Cosine similarity is computed between adjacent sentences
4. **Boundary Detection**: Low similarity scores indicate semantic boundaries
5. **Chunk Formation**: Sentences are grouped into chunks respecting token limits

### Configuration

```csharp
var chunkerOptions = new IngestionChunkerOptions(tokenizer)
{
    MaxTokensPerChunk = 2000,      // Maximum tokens per chunk
    OverlapTokens = 200,           // Overlap for context continuity
    MinTokensPerChunk = 100,       // Minimum chunk size
    SimilarityThreshold = 0.7f     // Boundary detection threshold
};

var chunker = new SemanticSimilarityChunker<string>(
    embeddingGenerator, 
    chunkerOptions
);
```

## Summary Enricher Configuration

The `SummaryEnricher` generates AI summaries for each chunk, which can be used for:
- Quick previews in search results
- Additional context for RAG queries
- Document overview generation

```csharp
var enricherOptions = new EnricherOptions(chatClient)
{
    MaxSummaryLength = 200,  // Maximum summary tokens
    Temperature = 0.3f,      // Lower for more consistent summaries
    SystemPrompt = "Generate a concise summary of the following text."
};

var summaryEnricher = new SummaryEnricher(enricherOptions);
```

## Chunk Data Structure

Each processed chunk contains:

```csharp
public record IngestionChunk
{
    public string DocumentId { get; init; }      // Source document identifier
    public string ChunkId { get; init; }         // Unique chunk identifier
    public string Content { get; init; }         // Chunk text content
    public int TokenCount { get; init; }         // Number of tokens
    public int ChunkIndex { get; init; }         // Position in document
    public Dictionary<string, object> Metadata { get; init; }  // Enrichment data
}
```

## Error Handling and Retry

The pipeline includes built-in retry logic for transient failures:

```csharp
var pipeline = new IngestionPipelineBuilder<string>()
    .UseDocumentReader(reader)
    .UseChunker(chunker)
    .UseWriter(writer)
    .ConfigureRetry(options =>
    {
        options.MaxRetries = 3;
        options.InitialDelay = TimeSpan.FromSeconds(1);
        options.BackoffMultiplier = 2.0;
    })
    .Build();
```

## Performance Considerations

| Factor | Recommendation |
|--------|----------------|
| Chunk Size | 1000-2000 tokens for balance |
| Overlap | 10-20% for context preservation |
| Batch Size | Process 10-50 documents per batch |
| Parallelism | Use async streams for I/O efficiency |
| Caching | Cache embeddings for repeated processing |

## NuGet Packages

| Package | Description |
|---------|-------------|
| `Microsoft.Extensions.DataIngestion` | Core pipeline framework |
| `Microsoft.Extensions.DataIngestion.Markdig` | Markdown reader |
| `Microsoft.Extensions.DataIngestion.MarkItDown` | MCP document converter |
| `Microsoft.Extensions.DataIngestion.Html` | HTML reader |

## Resources

- [Official Documentation](https://learn.microsoft.com/dotnet/ai/data-ingestion)
- [GitHub Repository](https://github.com/microsoft/dotnet-extensions)
- [Vector Store Integration Guide](https://learn.microsoft.com/semantic-kernel/concepts/vector-store)
