using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Net;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.DependencyInjection;

// #########################################################
// # JDP SECURITY RESEARCH SERIES: PROJECT NUKA-AI         #
// # TARGET: SEMANTIC KERNEL V1.48.0 (STRICT BINDER TEST)  #
// # STATUS: LIKE-FOR-LIKE AUDIT FROM V1.47.0              #
// #########################################################

var builder = Kernel.CreateBuilder();
builder.Plugins.AddFromType<FilePlugin>("FileTools");
builder.Services.AddSingleton<IFunctionInvocationFilter, PathSanitizationFilter>();
var kernel = builder.Build();

Console.WriteLine("=== NUKA-AI SECURITY AUDIT: V1.48.0 LIKE-FOR-LIKE ===");

// This call worked in 1.47.0 but will trigger a KernelException in 1.48.0
Console.WriteLine("\n[BYPASS 1] Attempting Base64 Encoding (Legacy Syntax)...");
try {
    await kernel.InvokeAsync("FileTools", "SaveConversation", new() {
        { "path", Convert.ToBase64String(Encoding.UTF8.GetBytes("Program.cs")) },
        { "content", "BYPASS 1: SUCCESS\n" }
    });
} catch (Exception ex) {
    Console.WriteLine($"\n[!] CRASH DETECTED IN V1.48.0:");
    Console.WriteLine($"Type: {ex.GetType().Name}");
    Console.WriteLine($"Message: {ex.Message}");
}

// --- THE DEFENSE ---
public class PathSanitizationFilter : IFunctionInvocationFilter {
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next) {
        foreach (var arg in context.Arguments) {
            if (arg.Value is string str && (str.Contains("..") || str.Contains("/")))
                throw new UnauthorizedAccessException("Blocked!");
        }
        await next(context);
    }
}

// --- THE VULNERABLE SINK ---
public class FilePlugin {
    [KernelFunction]
    public void SaveConversation(object path, string content) {
        string stringPath = path?.ToString() ?? "default.txt";
        if (stringPath.Contains("%")) stringPath = WebUtility.UrlDecode(stringPath);
        File.AppendAllText(stringPath, content);
EOF }
vboxuser@Ubuntu-Server:~/sk-lab-additional$
vboxuser@Ubuntu-Server:~/sk-lab-additional$
vboxuser@Ubuntu-Server:~/sk-lab-additional$
vboxuser@Ubuntu-Server:~/sk-lab-additional$ cat Program.cs
using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Net;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.DependencyInjection;

// #########################################################
// # JDP SECURITY RESEARCH SERIES: PROJECT NUKA-AI         #
// # TARGET: SEMANTIC KERNEL V1.48.0 (STRICT BINDER TEST)  #
// # STATUS: LIKE-FOR-LIKE AUDIT FROM V1.47.0              #
// #########################################################

var builder = Kernel.CreateBuilder();
builder.Plugins.AddFromType<FilePlugin>("FileTools");
builder.Services.AddSingleton<IFunctionInvocationFilter, PathSanitizationFilter>();
var kernel = builder.Build();

Console.WriteLine("=== NUKA-AI SECURITY AUDIT: V1.48.0 LIKE-FOR-LIKE ===");

// This call worked in 1.47.0 but will trigger a KernelException in 1.48.0
Console.WriteLine("\n[BYPASS 1] Attempting Base64 Encoding (Legacy Syntax)...");
try {
    await kernel.InvokeAsync("FileTools", "SaveConversation", new() {
        { "path", Convert.ToBase64String(Encoding.UTF8.GetBytes("Program.cs")) },
        { "content", "BYPASS 1: SUCCESS\n" }
    });
} catch (Exception ex) {
    Console.WriteLine($"\n[!] CRASH DETECTED IN V1.48.0:");
    Console.WriteLine($"Type: {ex.GetType().Name}");
    Console.WriteLine($"Message: {ex.Message}");
}

// --- THE DEFENSE ---
public class PathSanitizationFilter : IFunctionInvocationFilter {
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next) {
        foreach (var arg in context.Arguments) {
            if (arg.Value is string str && (str.Contains("..") || str.Contains("/")))
                throw new UnauthorizedAccessException("Blocked!");
        }
        await next(context);
    }
}

// --- THE VULNERABLE SINK ---
public class FilePlugin {
    [KernelFunction]
    public void SaveConversation(object path, string content) {
        string stringPath = path?.ToString() ?? "default.txt";
        if (stringPath.Contains("%")) stringPath = WebUtility.UrlDecode(stringPath);
        File.AppendAllText(stringPath, content);
    }
}
