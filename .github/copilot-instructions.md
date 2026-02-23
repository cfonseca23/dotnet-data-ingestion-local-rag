# Project Guidelines

## Code Style

- **C# 14 / .NET 10** — file-scoped namespaces, top-level statements in `Program.cs`, nullable enabled, implicit usings enabled.
- Records for immutable config (`PipelineConfig`). Collection expressions (`[]`), target-typed `new()`, `using var`, `await foreach`.
- Private fields prefixed with `_` (`_config`, `_disposables`). PascalCase for public members.
- XML `<summary>` doc comments on all classes, noting the SOLID principle applied.
- Namespace convention: `DataIngest.{Subfolder}` — direct folder-to-namespace mapping.
- See [src/Services/PipelineFactory.cs](src/Services/PipelineFactory.cs) and [src/Configuration/PipelineConfig.cs](src/Configuration/PipelineConfig.cs) as reference patterns.

## Architecture

```
.md files → MarkItDown MCP (Docker:3001) → SemanticSimilarityChunker → Ollama Enrichers → SQLite-vec → Semantic Search
```

Four source files with clear separation:

| File | Responsibility |
|------|---------------|
| `src/Program.cs` | Composition root — wires config, factory, pipeline, search loop |
| `src/Configuration/PipelineConfig.cs` | Immutable record with all settings (endpoints, models, dimensions) |
| `src/Services/PipelineFactory.cs` | Factory creating pipeline components + `IDisposable` ownership tracking |
| `src/UI/ConsoleUI.cs` | Console presentation — formatting, emojis, ASCII frames |

Key patterns:
- **Factory owns lifecycle**: `PipelineFactory` tracks all created `IDisposable` objects in `_disposables`.
- **Fresh ingestion each run**: `vectors.db` is deleted on every execution — no incremental ingestion.
- **Config is hardcoded**: No `appsettings.json` — all defaults live in `PipelineConfig` record.

## Build and Test

```bash
# Prerequisites — pull Ollama models and start services
ollama pull qwen3:1.7b
ollama pull nomic-embed-text
ollama serve

# MarkItDown MCP Server (Docker required)
docker run -p 3001:3001 mcp/markitdown --http --host 0.0.0.0 --port 3001

# Build and run
dotnet build
dotnet run
```

No test project exists yet. AOT is explicitly disabled (OllamaSharp uses reflection).

## Project Conventions

- Data files (`data/*.md`) are copied to output via `CopyToOutputDirectory: PreserveNewest` in `.csproj`.
- Separate `HttpClient` instances for chat and embeddings (both target Ollama at `localhost:11434`).
- Embeddings use `nomic-embed-text` (768 dimensions); chat uses `qwen3:1.7b`.
- SQLite connection string uses `Pooling=false` to avoid file locks.
- Console UI uses emojis extensively (✅❌⚠️📁📄🔍👋🗑️) and ASCII box-drawing characters.
- Paths shown to users must be **relative** (never absolute) — see `ConsoleUI.ShowDirectoryInfo`.
- Preview NuGet packages in use: `Microsoft.Extensions.DataIngestion` and `SemanticKernel.Connectors.SqliteVec`.

## Integration Points

| Service | Endpoint | Notes |
|---------|----------|-------|
| Ollama | `http://localhost:11434` | Local LLM + embeddings, no auth required |
| MarkItDown MCP | `http://localhost:3001/mcp` | Docker container `mcp/markitdown` |
| SQLite + sqlite-vec | `vectors.db` (local file) | Serverless vector store |

## Security

- **100% local processing** — no cloud APIs, no credentials or API keys.
- User search input passes directly to `SearchAsync` — sanitize if exposing as a service.
- `HttpTimeout` set to 5 minutes to handle slow local models.
