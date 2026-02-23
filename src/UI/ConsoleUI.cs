namespace DataIngest.UI;

/// <summary>
/// Handles all console UI interactions.
/// Follows Single Responsibility - only handles user interaction.
/// </summary>
public class ConsoleUI
{
    public void ShowHeader()
    {
        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║              DATA INGESTION PIPELINE - OLLAMA                ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
    }

    public void ShowSearchHeader()
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                     SEMANTIC SEARCH                          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
    }

    public void ShowError(string message) => Console.WriteLine($"  ❌ {message}");
    public void ShowSuccess(string message) => Console.WriteLine($"  ✅ {message}");
    public void ShowWarning(string message) => Console.WriteLine($"  ⚠️  {message}");
    public void ShowInfo(string message) => Console.WriteLine($"  {message}");
    public void ShowSeparator() => Console.WriteLine("──────────────────────────────────────────────────────────────────");
    public void NewLine() => Console.WriteLine();

    public bool AskYesNo(string prompt, bool defaultNo = true)
    {
        var defaultHint = defaultNo ? "(y/N)" : "(Y/n)";
        Console.Write($"  {prompt} {defaultHint}: ");
        var response = Console.ReadLine()?.Trim().ToLower();
        return response == "y" || response == "yes";
    }

    public string? AskInput(string prompt)
    {
        Console.Write($"  {prompt}: ");
        return Console.ReadLine();
    }

    public void ShowDirectoryInfo(DirectoryInfo dir, string pattern)
    {
        var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), dir.FullName);
        Console.WriteLine($"  📁 Directory: .{Path.DirectorySeparatorChar}{relativePath}");
        Console.WriteLine($"  📄 Files found: {dir.GetFiles(pattern).Length}");
        Console.WriteLine();
    }

    public void ShowProcessingResult(int current, int total, string fileName, bool success, string? error = null)
    {
        Console.WriteLine();
        Console.WriteLine($"  [{current}/{total}] {fileName}");
        if (success)
            Console.WriteLine($"       ✅ Successfully processed");
        else
            Console.WriteLine($"       ❌ Error: {error}");
    }

    public void ShowSearchResult(int rank, double score, string? summary, string content)
    {
        var barLength = (int)(score * 20);
        var scoreBar = new string('█', barLength) + new string('░', 20 - barLength);
        
        Console.WriteLine($"  ┌─ Result #{rank}");
        Console.WriteLine($"  │ Score: {score:F4} [{scoreBar}]");
        Console.WriteLine($"  │");
        
        if (!string.IsNullOrWhiteSpace(summary))
        {
            Console.WriteLine($"  │ 📝 Summary:");
            foreach (var line in summary.Split('\n').Take(3))
            {
                var trimmed = line.Length > 65 ? line[..62] + "..." : line;
                Console.WriteLine($"  │    {trimmed}");
            }
            Console.WriteLine($"  │");
        }
        
        Console.WriteLine($"  │ 📄 Content preview:");
        foreach (var line in content.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).Take(4))
        {
            var trimmed = line.Length > 65 ? line[..62] + "..." : line;
            Console.WriteLine($"  │    {trimmed}");
        }
        if (content.Split('\n').Length > 4)
            Console.WriteLine($"  │    ...");
        
        Console.WriteLine($"  └───────────────────────────────────────────────────────────");
        Console.WriteLine();
    }

    public void ShowGoodbye() => Console.WriteLine("  👋 Goodbye!");
    public void ShowFinished() => Console.WriteLine("  ✅ Pipeline finished.");
}
