# Semantic Kernel

## What is Semantic Kernel?

Semantic Kernel is Microsoft's open-source SDK for integrating Large Language Models (LLMs) into .NET, Python, and Java applications. It provides a comprehensive framework for building AI-powered applications that can combine traditional programming with AI capabilities, enabling developers to create intelligent systems that leverage the best of both worlds.

The SDK is designed around the concept of an "AI orchestration layer" that sits between your application and various AI services. This abstraction allows you to swap AI providers, combine multiple models, and build complex AI workflows without being locked into a specific vendor or implementation.

## Core Concepts

### The Kernel

The Kernel is the central component that orchestrates AI operations. It manages:
- AI service connections (chat, embeddings, text-to-image, etc.)
- Plugin registration and invocation
- Memory and context management
- Dependency injection integration

```csharp
var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey)
    .AddOllamaTextEmbeddingGeneration("all-minilm", new Uri("http://localhost:11434"))
    .Build();
```

### Plugins

Plugins are collections of related functions that extend the kernel's capabilities. There are two types:

**Native Functions**: Regular C# methods decorated with attributes
```csharp
public class TimePlugin
{
    [KernelFunction, Description("Gets the current date and time")]
    public string GetCurrentTime() => DateTime.Now.ToString("F");
    
    [KernelFunction, Description("Gets the current date")]
    public string GetCurrentDate() => DateTime.Today.ToShortDateString();
}
```

**Semantic Functions**: Prompt templates that invoke AI models
```yaml
name: Summarize
template: |
  Summarize the following text in {{$style}} style:
  
  {{$input}}
  
  Summary:
```

### Semantic Functions with Prompty

Prompty is a new format for defining semantic functions:

```prompty
---
name: EmailGenerator
description: Generates professional emails
authors:
  - Microsoft
model:
  api: chat
  configuration:
    type: azure_openai
inputs:
  recipient:
    type: string
  subject:
    type: string
  tone:
    type: string
    default: professional
---
Write an email to {{recipient}} about {{subject}}.
Use a {{tone}} tone.
```

## Key Features

### 1. Multi-Model Support

Semantic Kernel supports numerous AI providers out of the box:

| Provider | Chat | Embeddings | Text-to-Image | Text-to-Speech |
|----------|------|------------|---------------|----------------|
| Azure OpenAI | ✓ | ✓ | ✓ | ✓ |
| OpenAI | ✓ | ✓ | ✓ | ✓ |
| Ollama | ✓ | ✓ | - | - |
| Google Gemini | ✓ | ✓ | - | - |
| Anthropic Claude | ✓ | - | - | - |
| Hugging Face | ✓ | ✓ | - | - |
| Mistral AI | ✓ | ✓ | - | - |

### 2. Automatic Function Calling

The kernel can automatically invoke functions based on user requests:

```csharp
kernel.ImportPluginFromType<TimePlugin>();
kernel.ImportPluginFromType<WeatherPlugin>();

var settings = new OpenAIPromptExecutionSettings
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
};

var result = await kernel.InvokePromptAsync(
    "What's the weather like today in Seattle?",
    new KernelArguments(settings)
);
```

### 3. Planners

Planners enable complex task orchestration by breaking down goals into steps:

```csharp
// Handlebars Planner for template-based plans
var planner = new HandlebarsPlanner();
var plan = await planner.CreatePlanAsync(kernel, "Create a marketing campaign for our new product");
var result = await plan.InvokeAsync(kernel);

// Function Calling Stepwise Planner
var stepwisePlanner = new FunctionCallingStepwisePlanner();
var result = await stepwisePlanner.ExecuteAsync(kernel, "Analyze sales data and create a report");
```

### 4. Memory and Vector Stores

Semantic Kernel provides a unified interface for vector databases:

```csharp
// Create a vector store collection
var vectorStore = new SqliteVectorStore(
    "Data Source=memory.db;Pooling=false",
    new() { EmbeddingGenerator = embeddingGenerator }
);

var collection = vectorStore.GetCollection<string, DocumentChunk>("documents");
await collection.CreateCollectionIfNotExistsAsync();

// Store a memory
await collection.UpsertAsync(new DocumentChunk
{
    Id = "doc-001",
    Content = "Semantic Kernel is an AI orchestration framework",
    Embedding = await embeddingGenerator.GenerateEmbeddingAsync("...")
});

// Semantic search
await foreach (var result in collection.SearchAsync("What is SK?", top: 5))
{
    Console.WriteLine($"{result.Score}: {result.Record.Content}");
}
```

### Supported Vector Stores

| Store | Type | Use Case |
|-------|------|----------|
| SQLite Vec | Local file | Development, small apps |
| PostgreSQL pgvector | Self-hosted | Production, SQL integration |
| Qdrant | Dedicated | High-performance search |
| Azure AI Search | Cloud | Enterprise, hybrid search |
| Pinecone | Cloud | Serverless vector DB |
| Weaviate | Self-hosted | GraphQL integration |
| Redis | In-memory | Low-latency caching |
| Milvus | Distributed | Large-scale deployments |

## Building a RAG Application

Retrieval-Augmented Generation (RAG) combines search with generation:

```csharp
public class RagService
{
    private readonly Kernel _kernel;
    private readonly VectorStoreCollection<string, DocumentChunk> _collection;
    
    public async Task<string> AskAsync(string question)
    {
        // 1. Retrieve relevant documents
        var context = new StringBuilder();
        await foreach (var result in _collection.SearchAsync(question, top: 3))
        {
            context.AppendLine(result.Record.Content);
        }
        
        // 2. Generate answer with context
        var prompt = $"""
            Answer the question based on the following context:
            
            Context:
            {context}
            
            Question: {question}
            
            Answer:
            """;
        
        var result = await _kernel.InvokePromptAsync(prompt);
        return result.ToString();
    }
}
```

## Filters and Observability

Semantic Kernel supports filters for logging, telemetry, and modification:

```csharp
public class LoggingFilter : IFunctionFilter
{
    private readonly ILogger _logger;
    
    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context, 
        Func<FunctionInvocationContext, Task> next)
    {
        _logger.LogInformation($"Invoking: {context.Function.Name}");
        var sw = Stopwatch.StartNew();
        
        await next(context);
        
        _logger.LogInformation($"Completed: {context.Function.Name} in {sw.ElapsedMilliseconds}ms");
    }
}

// Register filter
kernel.FunctionFilters.Add(new LoggingFilter(logger));
```

## Dependency Injection Integration

```csharp
// In Program.cs or Startup.cs
builder.Services.AddKernel()
    .AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey)
    .AddSqliteVectorStore("Data Source=vectors.db")
    .Plugins.AddFromType<TimePlugin>()
    .Plugins.AddFromType<WeatherPlugin>();

// Use in controllers/services
public class ChatController
{
    private readonly Kernel _kernel;
    
    public ChatController(Kernel kernel) => _kernel = kernel;
    
    public async Task<IActionResult> Chat([FromBody] string message)
    {
        var result = await _kernel.InvokePromptAsync(message);
        return Ok(result.ToString());
    }
}
```

## Advanced: Agents with Semantic Kernel

Semantic Kernel supports multi-agent scenarios:

```csharp
// Create agents with different roles
ChatCompletionAgent analyst = new()
{
    Name = "Analyst",
    Instructions = "You analyze data and provide insights",
    Kernel = kernel
};

ChatCompletionAgent writer = new()
{
    Name = "Writer",
    Instructions = "You write reports based on analysis",
    Kernel = kernel
};

// Create group chat
AgentGroupChat chat = new(analyst, writer)
{
    ExecutionSettings = new()
    {
        TerminationStrategy = new MaxTurnsTerminationStrategy(10)
    }
};

// Run conversation
await foreach (var message in chat.InvokeAsync("Analyze Q4 sales and write a report"))
{
    Console.WriteLine($"[{message.AuthorName}]: {message.Content}");
}
```

## Configuration Best Practices

```csharp
// Use configuration binding
var openAIConfig = builder.Configuration.GetSection("OpenAI").Get<OpenAIConfig>();

// Use secrets for API keys
builder.Services.AddKernel()
    .AddAzureOpenAIChatCompletion(
        deploymentName: openAIConfig.DeploymentName,
        endpoint: openAIConfig.Endpoint,
        apiKey: builder.Configuration["OpenAI:ApiKey"] // From user secrets
    );

// Configure retry policies
builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddStandardResilienceHandler(options =>
    {
        options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(5);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(10);
    });
});
```

## NuGet Packages

| Package | Description |
|---------|-------------|
| `Microsoft.SemanticKernel` | Core SDK |
| `Microsoft.SemanticKernel.Connectors.OpenAI` | OpenAI/Azure OpenAI |
| `Microsoft.SemanticKernel.Connectors.Ollama` | Ollama local models |
| `Microsoft.SemanticKernel.Connectors.SqliteVec` | SQLite vector store |
| `Microsoft.SemanticKernel.Connectors.Postgres` | PostgreSQL pgvector |
| `Microsoft.SemanticKernel.Connectors.Qdrant` | Qdrant vector DB |
| `Microsoft.SemanticKernel.Plugins.Core` | Built-in plugins |
| `Microsoft.SemanticKernel.Planners.Handlebars` | Handlebars planner |

## Resources

- [GitHub Repository](https://github.com/microsoft/semantic-kernel)
- [Official Documentation](https://learn.microsoft.com/semantic-kernel/)
- [Sample Applications](https://github.com/microsoft/semantic-kernel/tree/main/samples)
- [Discord Community](https://aka.ms/semantic-kernel/discord)
- [Blog](https://devblogs.microsoft.com/semantic-kernel/)
