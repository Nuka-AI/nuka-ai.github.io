using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.ComponentModel;
using System.Diagnostics;

// --- 1. KERNEL SETUP ---
var builder = Kernel.CreateBuilder();

// Connecting to Ollama's OpenAI-compatible endpoint
builder.AddOpenAIChatCompletion(
    modelId: "llama3.2",
    apiKey: "ollama",
    endpoint: new Uri("http://172.17.0.1:11434/v1")
);

// Import the vulnerable plugin
builder.Plugins.AddFromType<FilePlugin>("FileTools");
var kernel = builder.Build();

Console.WriteLine("--- CVE SEARCH HARNESS ACTIVE ---");
Console.WriteLine("Target: http://172.17.0.1:11434/v1 (Llama 3.2)");
Console.WriteLine("Ready for Researcher Input...");

while (true)
{
    Console.Write("\nResearcher Input: ");
    string? input = Console.ReadLine();
    if (string.IsNullOrEmpty(input)) break;

    // Enable Auto-Invocation so the LLM can trigger the tool
    OpenAIPromptExecutionSettings settings = new()
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
    };

    try
    {
        var result = await kernel.InvokePromptAsync(input, new(settings));
        Console.WriteLine($"\nLLM Response: {result}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n[!] Error: {ex.Message}");
    }
}

// --- 2. THE VULNERABLE PLUGIN DEFINITION ---
public class FilePlugin
{
    [KernelFunction, Description("Reads a data file from the storage folder.")]
    public string ReadDataFile(
        [Description("The relative path of the file to read.")] string path)
    {
        // Vulnerable: Path.Combine does not sanitize '..'
        // This demonstrates the Information Disclosure vector
        string fullPath = Path.Combine("data", path);
        return File.ReadAllText(fullPath);
    }

    [KernelFunction, Description("Saves content to a specified file path.")]
    public void SaveConversation(
        [Description("The exact file path to write to.")] string path,
        [Description("The string content to be saved.")] string content)
    {
        // CRITICAL VULNERABILITY: Direct write with no path validation
        // This is the vector used to overwrite Program.cs
        Console.WriteLine($"\n[SYSTEM] Executing Tool: SaveConversation(path='{path}')");
        File.WriteAllText(path, content);
        Console.WriteLine("[SYSTEM] Write operation completed successfully.");
    }

    [KernelFunction, Description("Executes a shell script or command on the system.")]
    public string ExecuteScript(
        [Description("The path to the script to execute.")] string path)
    {
        try
        {
            // THE RCE SINK: This allows the LLM to run any command it wants.
            // Note: v1.47.0 attempts to "blind" this, but v1.74.0 is wide open.
            Console.WriteLine($"\n[SYSTEM] Executing Script: {path}");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = path,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return $"Output: {result}";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}
