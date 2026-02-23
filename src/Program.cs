using DataIngest.Configuration;
using DataIngest.Services;
using DataIngest.UI;
using Microsoft.Extensions.Logging;

// Configuration
var config = new PipelineConfig();
var ui = new ConsoleUI();

using var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole());
using var factory = new PipelineFactory(config, loggerFactory);

// Validate data directory
var dataDir = new DirectoryInfo(config.DataPath);
if (!dataDir.Exists)
{
    ui.ShowError($"Directory does not exist '{dataDir.FullName}'");
    ui.ShowInfo("Create the 'data' folder with .md files or adjust the path.");
    return;
}

ui.ShowHeader();

// Delete existing database to ensure fresh ingestion
if (File.Exists(config.DatabasePath))
{
    File.Delete(config.DatabasePath);
    ui.ShowInfo("🗑️  Previous database deleted. Starting fresh ingestion...");
}

ui.NewLine();

// Create pipeline components
var chatClient = factory.CreateChatClient();
var embeddingGenerator = factory.CreateEmbeddingGenerator();
var reader = factory.CreateDocumentReader();
var chunker = factory.CreateChunker(embeddingGenerator);
var vectorStore = factory.CreateVectorStore(embeddingGenerator);
var writer = factory.CreateWriter(vectorStore);
var pipeline = factory.CreatePipeline(reader, chunker, writer, chatClient);

// Ingestion phase
ui.ShowDirectoryInfo(dataDir, config.SearchPattern);
ui.ShowSeparator();

bool anySuccess = false;
int processed = 0;
var files = dataDir.GetFiles(config.SearchPattern);

await foreach (var result in pipeline.ProcessAsync(dataDir, searchPattern: config.SearchPattern))
{
    processed++;
    ui.ShowProcessingResult(
        processed, 
        files.Length, 
        Path.GetFileName(result.DocumentId), 
        result.Succeeded, 
        result.Exception?.Message);
    
    if (result.Succeeded) anySuccess = true;
}

ui.NewLine();
ui.ShowSeparator();

if (!anySuccess)
{
    ui.NewLine();
    ui.ShowWarning("No documents were successfully processed.");
    ui.NewLine();
    return;
}

ui.NewLine();
ui.ShowSuccess("Ingestion completed. Starting interactive search...");
ui.NewLine();

// Search phase
var collection = writer.VectorStoreCollection;
ui.ShowSearchHeader();

while (true)
{
    var searchValue = ui.AskInput("🔍 Your query (or 'exit' to quit)");
    
    if (string.IsNullOrEmpty(searchValue) || searchValue.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        ui.NewLine();
        ui.ShowGoodbye();
        ui.NewLine();
        break;
    }

    ui.NewLine();
    ui.ShowInfo("Searching...");
    ui.NewLine();
    
    var results = new List<(double Score, string Content, string Summary)>();
    
    await foreach (var result in collection.SearchAsync(searchValue, top: config.TopResults))
    {
        var content = result.Record.TryGetValue("content", out var c) ? c?.ToString() ?? "" : "";
        var summary = result.Record.TryGetValue("summary", out var s) ? s?.ToString() ?? "" : "";
        results.Add((result.Score ?? 0, content, summary));
    }
    
    if (results.Count == 0)
    {
        ui.ShowWarning("No results found.");
        ui.NewLine();
        continue;
    }
    
    ui.ShowInfo($"Found {results.Count} result(s):");
    ui.NewLine();
    
    int rank = 1;
    foreach (var (score, content, summary) in results)
    {
        ui.ShowSearchResult(rank++, score, summary, content);
    }
}

ui.ShowFinished();
