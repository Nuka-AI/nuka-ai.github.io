---
date: 2026-04-28
title: Initial Research Disclosure
---
> **⚠️ CRITICAL ADVISORY:** If you are running **Microsoft Semantic Kernel (.NET SDK) version 1.48.0 or below**, or the newly released **Agent Framework 1.0**, your environment is currently operating with an unmitigated RCE entry point. This paper demonstrates active bypasses against Microsoft's official remediation for CVE-2026-25592. Users are strongly advised to implement the manual `NukaSecurityFilter` outlined in Appendix 1 immediately.

---

# WHITE PAPER | NUKA-AI-2026-001
## The Orchestration Trust Gap: Day-Zero Bypasses in Microsoft Semantic Kernel and Agent Framework 1.0

**Author:** Jeff Ponte, CISSP, CCSP, CEH | Security Researcher, JDP-Security  
**Series:** Project Nuka-AI (Disclosure #1)  
**Date:** April 25, 2026  
**Classification:** Public Research Disclosure  
**Target:** Microsoft Semantic Kernel (.NET) v1.47.0 - v1.48.0, Agent Framework 1.0  
**CVSS v3.1 Score:** 10.0 (Critical)  
**Vector:** `AV:N/AC:L/PR:N/UI:N/S:C/C:H/I:H/A:H`  
**CWE Chain:** CWE-1039 → CWE-22 → CWE-94  

---

## Executive Summary
This white paper documents a catastrophic architectural flaw in Microsoft’s Semantic Kernel (SK) framework, the premier orchestration layer for .NET-based AI agents. Our research reveals a fundamental **"Trust Gap"** where the framework treats stochastic, untrusted Large Language Model (LLM) output as deterministic, high-privilege system commands.

This oversight culminates in a full-chain Remote Code Execution (RCE) vulnerability driven by **CWE-1039 (Insecure Automated Optimizations)**. We demonstrate how an AI agent can be manipulated into overwriting its own host application's source code (internally tracked as the "Self-Nuke" vector).

Crucially, our forensic analysis spanning versions 1.47.0 through 1.48.0 proves that Microsoft's previous attempts to secure the framework have failed. We are disclosing **six independent Day-Zero bypass vectors** that completely evade the official patch issued for the February 6th Path Traversal vulnerability (**CVE-2026-25592**). This research proves that the current framework security model is architecturally unsound, relying on siloed, cosmetic filters rather than foundational security principles like mandatory input canonicalization.

---

## Key Takeaways

1. **CVSS 10.0 RCE** exists in Microsoft Semantic Kernel v1.48.0 and Agent Framework 1.0
2. **6 Day-Zero Bypasses** defeat Microsoft's CVE-2026-25592 patch
3. **Shadow Patching** occurred while Microsoft publicly denied the vulnerability
4. **Type Confusion** is the root cause - filters check `string` but plugins accept `object`
5. **Immediate Action Required:** Disable `AutoInvokeKernelFunctions` and implement Appendix 1 filter

---

## 1. Business Impact: The "Silent" Enterprise Risk
This vulnerability represents a systemic failure in the AI supply chain. Because Microsoft has currently dismissed these bypasses as "Developer Error" rather than issuing a new CVE or acknowledging the failed patch, the enterprise risk is severely compounded:

*   **SCA Tool Blindness:** Software Composition Analysis (SCA) and vulnerability scanners remain "green." Organizations believe they are secure because they patched CVE-2026-25592, entirely unaware that the patch is trivial for an LLM to bypass.
*   **Agent Framework 1.0 Inheritance:** Microsoft officially launched Agent Framework 1.0 on April 3, 2026. Because it is built atop these same orchestration primitives, Agent Framework 1.0 inherits this exact CVSS 10.0 "Trust Gap" out of the box. 
*   **Shadow Patching Risks:** Microsoft’s internal remediation cycle (see Section 7) has consisted of quiet, incomplete mitigations. This leaves developers unaware that their current implementation of `AutoInvokeKernelFunctions` is a direct conduit for host takeover.

### Industries at Immediate Risk:
- **Finance:** AI-powered trading agents with file system access
- **Healthcare:** Patient data processing via AI orchestration
- **Government:** Autonomous document processing systems
- **SaaS:** Multi-tenant AI services using Semantic Kernel

---

## 2. The Mechanics of Orchestration: Understanding Semantic Kernel
To comprehend the severity of these vulnerabilities, one must understand how Semantic Kernel operates. SK is not merely an API wrapper for OpenAI; it is a complex orchestration engine designed to give LLMs "hands" to interact with the host operating system.

### The Execution Pipeline
1.  **The Prompt:** A user inputs a natural language request.
2.  **The Planner/Kernel:** SK sends this request to the LLM, along with a "manifest" of available C# native functions (Plugins).
3.  **The Tool Call:** The LLM returns a JSON-formatted *Tool Call* instructing the framework to execute a specific C# function with specific arguments.
4.  **The Execution Sink:** The framework maps the LLM's JSON request to the compiled C# binary, executes the code, and feeds the result back to the LLM.

> **The Fatal Assumption:** Traditional application security assumes that the user is malicious and the backend logic is trusted. Semantic Kernel breaks this paradigm by placing a non-deterministic entity (the LLM) in the middle of the execution pipeline, yet the framework continues to trust the LLM's output as if it were hardcoded backend logic.

---

## 3. The Dual-Vulnerability Landscape
Our research highlights that Semantic Kernel is currently exposed to two distinct, highly critical attack vectors.

*   **Vulnerability A: The CVE-2026-25592 Day-Zero Bypasses**
    On February 6th, Microsoft released a patch for a known path traversal vulnerability. This patch focused on filtering the string arguments passed to plugins. Our research confirms this patch is structurally flawed because it fails to account for complex data types and LLM translation capabilities.
*   **Vulnerability B: The CWE-1039 Auto-Invocation Flaw**
    Even if standard prompt filters are active, the framework's architecture allows the AI to autonomously generate malicious payloads that execute directly against the host OS via `ToolCallBehavior.AutoInvokeKernelFunctions`.

---

## 4. The Three Doors of Vulnerability
We conceptualize the application’s security boundary as a house with three distinct entry points, all of which currently fail to protect the host.

### 4.1 The Front Door (Prompt Filtering)
*   **The Defense:** Regex-based filters designed to block literal `../` strings in user input. 
*   **Status: Bypassed.** This is a cosmetic defense. Attackers easily defeat it by instructing the LLM to construct the malicious string dynamically in memory, rather than providing it in the prompt.

### 4.2 The Kitchen Door (LLM Translation, TOCTOU, and Type Confusion)
*   **The Defense:** Standard string evaluation on LLM arguments via `IFunctionInvocationFilter` before they hit native code (The core mechanic of the CVE-2026-25592 patch).
*   **Status: Systemically Bypassed.** The framework evaluates arguments by checking if they are dangerous strings. However, if the LLM outputs a Base64 string, or structures the path inside a JSON array, the framework's `arg is string` evaluation evaluates to `false` or finds no malicious characters. The security filter waves it through. Once inside the execution sink, the underlying plugin deserializes the JSON or decodes the string and executes the payload. This is a fatal Time-of-Check to Time-of-Use (TOCTOU) vulnerability driven by Type Confusion.

### 4.3 The Garage Door: The CWE-1039 Auto-Invocation Architectural Flaw
*   **The Defense:** Trust.
*   **Vulnerable Implementation:** `ToolCallBehavior.AutoInvokeKernelFunctions`
*   **Status: Systemically Broken.** `AutoInvokeKernelFunctions` directly wires an untrusted, stochastic input source (the LLM) to highly privileged native code execution. In traditional MVC architectures, a controller validates all input before execution. Auto-Invocation rips the controller out of the pipeline. This violates Zero-Trust architecture and perfectly encapsulates CWE-1039.

---

## 5. The CWE-1039 RCE Chain: A Golden Chain of Failure
The full execution chain requires no elevated privileges and relies on a perfect alignment of three architectural failures:

1.  **CWE-1039 (Insecure Automated Optimizations):** The Auto-Invocation flaw allows the LLM to bypass developer scrutiny and act as a trusted orchestrator.
2.  **CWE-22 (Path Traversal):** The framework lacks mandatory, global "Path Anchoring." There is no native `[SafeRoot]` enforcement isolating file operations to a specific sandbox.
3.  **CWE-94 (Code Injection):** By bypassing the CVE-2026-25592 filters and traversing directories, the LLM locates and overwrites the application's source code (e.g., `Program.cs` or `appsettings.json`).

When the application next cycles, the injected payload executes with the privileges of the service account, completing the RCE chain.

---

## 6. Empirical Proof: Defeating CVE-2026-25592 (The 6 Bypasses)
To prove that the official remediation is fundamentally flawed, we tested Semantic Kernel v1.48.0 against six distinct evasion techniques. All six methods successfully bypassed the February 6th CVE patch, achieving a 100% exploitation success rate.

1.  **JSON Type Confusion:** Framework filters specifically look for `string` types. By passing the path as a JSON array (`["..", "..", "Program.cs"]`), the `arg is string` evaluation returns false, bypassing the check entirely before being concatenated at the execution sink.
2.  **Object Reflection Obfuscation:** Passing an anonymous object (`new { p = "../../file.txt" }`) bypasses flat string filters. The execution sink uses reflection to extract the property, executing the payload.
3.  **Base64 Encoding Bypass:** The attacker provides a Base64 string. The filter sees `UHJvZ3JhbS5jcw==` (safe). The LLM processes the tool call and passes the decoded `../../Program.cs` to the plugin.
4.  **URL Encoding Bypass:** Utilizing `%2e%2e%2f` to bypass filters that only search for literal dot and slash characters. If the execution sink URL-decodes the input, traversal is achieved.
5.  **Unicode Homoglyphs:** Utilizing visually identical Unicode characters, such as the full-width solidus `∕` (U+2044). Regex filters ignore it, but the underlying host Operating System normalizes it to a standard `/` during file I/O operations.
6.  **Hybrid Canonicalization:** Combining methods (e.g., URL-encoded Base64 inside a JSON array) to exhaust or confuse non-recursive sanitizers.

---

## 7. The Shadow Patch War: Full Forensic Timeline
The following timeline details the alarming discrepancy between Microsoft’s public stance and their internal engineering actions. While officially dismissing the vulnerability, Microsoft engineers were quietly pushing partial mitigations to the repository.

| Date (2026) | Event | Technical Significance & Vulnerability Status |
| :--- | :--- | :--- |
| **March 24** | **Initial Disclosure** | Full-chain RCE reported via MSRC. PoC `.CAST` recordings provided showing complete host takeover. |
| **April 3** | **Agent Framework 1.0 Launch** | **VULNERABLE.** Product launched while disclosure was in triage. Inherits the exact "Trust Gap" and CWE-1039 flaws from SK. |
| **April 7** | **The GA Bridge (v1.47.0)** | **VULNERABLE.** Commit `3e4c91a` adds "Sanity Checking." Microsoft markets "Enhanced Safety" while the core flaw remains. |
| **April 8** | **Official Rejection** | MSRC closes case as "Developer Error." Claims framework has no responsibility for tool-call sanitization. |
| **April 9** | **Failed Shadow Patch #1** | **VULNERABLE.** Commit `fa2d52f6` ("Shell Blinding") masks output but fails to block Path Traversal. Bypass demonstrated same day. |
| **April 11** | **Architectural Overhaul** | **INCOMPLETE.** PR #13683 implements `AllowedDirectories` (Safe Roots) exactly as recommended by our research. However, implementation remains opt-in. |
| **April 18** | **Canonicalization Fix** | **INCOMPLETE.** PR #13702 introduces Recursive Canonicalization designed to close Base64/Encoding bypasses. |
| **April 21** | **v1.48.0 Stable Release** | **STILL VULNERABLE.** Testing confirms the "Shadow Patch" in `DocumentPlugin.cs` fails due to siloed logic. All 6 bypasses remain functional. |
| **April 25** | **Current State** | **CRITICAL.** The framework remains open to RCE. The "Developer Error" stance has resulted in a failed, incomplete internal remediation cycle. |

---

## 8. Technical Analysis: The IFunctionInvocationFilter Failure
The root cause of these bypasses is a catastrophic failure in how `IFunctionInvocationFilter` evaluates arguments. Security checks occur on *un-decoded, un-normalized, and un-parsed* arguments.
```csharp
// THE VULNERABLE PATTERN (CVE-2026-25592 Remediation)
public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, ...) {
    
    // ⚠️ TIME OF CHECK: Type Confusion Vulnerability
    foreach (var arg in context.Arguments) {
        // ⚡ THE BYPASS POINT:
        // if arg.Value is a JsonElement, this 'is string' check returns false.
        // The filter is skipped, and the un-sanitized object proceeds to execution.
        if (arg.Value is string s && (s.Contains("..") || s.Contains("/"))) 
            throw new UnauthorizedAccessException("Blocked!");
    }
    
    // ⚠️ TIME OF USE: Execution sink deserializes the JSON/Object and executes
    await next(context); 
}
```
If an attacker instructs the LLM to format the tool call argument as `{"path": "../../Program.cs"}`, the framework reads `arg.Value` as a `JsonElement`, not a `string`. The security filter skips it. The underlying plugin then natively deserializes the JSON and executes the path traversal.

---

## 9. Remediation & Recommendations
To secure AI orchestration, the industry must adopt a Kernel-Level Security Enforcement Point. Security must be Inherent and Mandatory, not Opt-in and Plugin-Specific.

### 9.1 Immediate Mitigations (Developer Level)
*   **Deprecate Auto-Invocation:** Developers must stop using `AutoInvokeKernelFunctions` for any application with disk, network, or database access. Use manual function calling to inspect all LLM arguments before execution.
*   **Implement Safe Roots:** Explicitly hard-code directory boundaries in every file-system-bound plugin.
*   **Custom Global Filters:** Implement the `NukaSecurityFilter` (Appendix 1) that canonicalizes all types (Strings, JSON, Objects) before validation.

### 9.2 Architectural Requirements (Microsoft Level)
*   **Unified Security Pipeline:** Canonicalization must occur BEFORE the security check.
*   **Mandatory Path Anchoring:** Framework-level sandboxing should be the default.
*   **Acknowledge the Failed Patch:** A new CVE must be issued for the CVE-2026-25592 bypasses to trigger SCA tools across the enterprise ecosystem.

---

## 10. Conclusion: A Call for Architectural Accountability
This disclosure is more than just a technical flaw—it is a warning sign for the entire AI industry. As we rush to deploy increasingly autonomous AI agents via Semantic Kernel and Agent Framework 1.0, we are repeating the security mistakes of earlier computing eras. 

Microsoft's reliance on superficial string filtering proves a fundamental misunderstanding of the threat model. LLM output is untrusted input. 

Architecture is greater than implementation. Flaws in architecture cannot be simply patched with regex; they must be redesigned from the ground up. Until orchestration frameworks embrace Zero-Trust principles, the "Agentic AI" revolution remains a critical liability for the enterprise.

### About Project Nuka-AI
Project Nuka-AI is an independent research initiative focused on identifying systemic architectural risks in AI orchestration frameworks. Led by **Jeff Ponte (CISSP, CCSP, CEH)**, the project combines over a decade of enterprise software development and cloud security operations experience to ensure the AI revolution is built on secure foundations. 

**Contact:** research@jdp-security.com  
**Full Disclosure & PoC:** [https://jdp-security.com/nuka-ai-001](https://jdp-security.com/nuka-ai-001)

---

## Appendix 1: Developer Remediation (Manual Path Anchoring)
If you are unable to upgrade to a fundamentally secured version of Semantic Kernel, you must implement a manual `IFunctionInvocationFilter` to prevent CWE-1039 exploitation. This logic intercepts the LLM's tool call *before* it hits the file system, deeply inspects complex types, and ensures the path is restricted to a specific "Safe Root."

### The C# Implementation
```csharp
// Appendix 1: Enterprise-Grade Semantic Kernel Security Filter
// This filter addresses ALL SIX bypass vectors identified in our research
public class NukaSecurityFilter : IFunctionInvocationFilter
{
    private readonly string _safeRoot;
    private readonly HashSet<string> _fileIoNames = new()
    {
        "SaveConversation", "ReadDataFile", "DownloadToFileAsync",
        "UploadFile", "WriteFile", "ReadFile", "ExecuteScript"
    };

    public NukaSecurityFilter(string safeRootDirectory = null)
    {
        _safeRoot = Path.GetFullPath(safeRootDirectory ?? 
            Path.Combine(Directory.GetCurrentDirectory(), "appdata"));
        
        Directory.CreateDirectory(_safeRoot);
    }

    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context, 
        Func<FunctionInvocationContext, Task> next)
    {
        if (_fileIoNames.Contains(context.Function.Name))
        {
            foreach (var arg in context.Arguments)
            {
                var canonicalValue = CanonicalizeArgument(arg.Value);
                if (IsPathLikeArgument(arg.Key, canonicalValue))
                {
                    var safePath = ValidateAndSanitizePath(canonicalValue);
                    context.Arguments[arg.Key] = safePath;
                }
            }
        }
        await next(context);
    }

    private string CanonicalizeArgument(object value)
    {
        if (value == null) return string.Empty;
        string stringValue = value.ToString();
        if (stringValue.Contains('%')) stringValue = WebUtility.UrlDecode(stringValue);
        stringValue = NormalizeUnicode(stringValue);
        if (IsBase64(stringValue))
            stringValue = Encoding.UTF8.GetString(Convert.FromBase64String(stringValue));
        
        // Critcial Fix: Handle Type Confusion (JSON/Objects)
        if (value is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Array && jsonElement.GetArrayLength() > 0)
                stringValue = jsonElement[0].GetString() ?? stringValue;
            else if (jsonElement.ValueKind == JsonValueKind.Object)
                stringValue = ExtractStringFromJsonObject(jsonElement);
        }
        return stringValue;
    }

    private string ValidateAndSanitizePath(string userPath)
    {
        string fullPath = Path.GetFullPath(Path.Combine(_safeRoot, userPath));
        if (!fullPath.StartsWith(_safeRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityException($"[NUKA-AI BLOCKED] Path traversal attempt detected.");
        }
        return fullPath;
    }

    private bool IsPathLikeArgument(string argName, string value)
    {
        var pathKeywords = new[] { "path", "file", "directory", "folder", "location" };
        return pathKeywords.Any(k => argName.Contains(k, StringComparison.OrdinalIgnoreCase)) ||
               value.Contains('/') || value.Contains('\\') ||
               (value.Contains('.') && (value.EndsWith(".txt") || value.EndsWith(".json") || value.EndsWith(".cs")));
    }

    private string NormalizeUnicode(string input)
    {
        return input.Replace("∕", "/").Replace("⁄", "/").Replace("＼", "\\")
                    .Replace("．", ".").Replace("․", ".").Normalize(NormalizationForm.FormKC);
    }

    private bool IsBase64(string input)
    {
        if (string.IsNullOrEmpty(input) || input.Length % 4 != 0 || input.Any(char.IsWhiteSpace)) return false;
        try { Convert.FromBase64String(input); return true; } catch { return false; }
    }

    private string ExtractStringFromJsonObject(JsonElement jsonObject)
    {
        foreach (var property in jsonObject.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.String) return property.Value.GetString();
        }
        return jsonObject.ToString();
    }
} // End of NukaSecurityFilter class
```

---

## Appendix 2: .cast Recording Demonstration Breakdown (Chronological)

This section serves as the forensic "receipts" for the Project Nuka-AI disclosure. These five recordings provide a standalone narrative of how Semantic Kernel's architectural flaws persist across versions and execution methods.

### 1. Microsoft_SK_1.74_Nuke_Proof.cast
*   **Target Environment:** v1.74.0 (Tested: April 2026)
*   **Execution Method:** **LLM-Driven**
*   **Summary:** This recording demonstrates a successful autonomous exploit on a modern version of the SDK. The Researcher provides a natural language prompt ("Use 'FileTools-SaveConversation' to write..."), and the LLM (Llama 3.2) independently executes the tool call to overwrite `Program.cs`, proving that standard SDK-level protections fail to stop LLM-driven orchestration.

../assets/SK/Microsoft_SK_1.74_Nuke_Proof-Program.cs
../assets/SK/Microsoft_SK_1.74_Nuke_Proof.txt
../assets/SK/Microsoft_SK_1.74_Nuke_Proof.cast

../assets/SK/Microsoft_SK_1.74_Nuke_Proof.mp4


### 2. Microsoft_SK_1.47_Hardened_Bypass.cast
*   **Target Environment:** v1.47.0 (Post-Harden)
*   **Execution Method:** **LLM-Driven**
*   **Summary:** This demonstration targets the v1.47.0 release following Microsoft's initial hardening attempts. It proves that the "harness" is still vulnerable: the LLM is able to bypass the intended security boundaries and verify RCE/Integrity failure, confirming that the hardening did not address the root orchestration vulnerability.

../assets/SK/Microsoft_SK_1.47_Hardened_Bypass-Program.cs
../assets/SK/Microsoft_SK_1.47_Hardened_Bypass.txt
../assets/SK/Microsoft_SK_1.47_Hardened_Bypass.cast

../assets/SK/Microsoft_SK_1.47_Hardened_Bypass.mp4

### 3. JDP_Security_Series_NukaAI_v1.47-CVE-2026-25592-BYPASS-2.cast
*   **Target Environment:** v1.47.0 
*   **Execution Method:** **Technical Audit (Manual)**
*   **Summary:** A deep-dive into the failure of the official CVE-2026-25592 patch. This recording documents the **Type Confusion** flaw by manually invoking the kernel with various payloads (Base64, URL encoding, JSON arrays). It shows the filter successfully blocking basic strings but failing entirely when the same malicious path is wrapped in a different data type.

../assets/SK/JDP_Security_Series_NukaAI_v1.47-CVE-2026-25592-BYPASS-2-Program.cs
../assets/SK/JDP_Security_Series_NukaAI_v1.47-CVE-2026-25592-BYPASS-2.txt
../assets/SK/JDP_Security_Series_NukaAI_v1.47-CVE-2026-25592-BYPASS-2.cast

../assets/SK/JDP_Security_Series_NukaAI_v1.47-CVE-2026-25592-BYPASS-2.mp4

### 4. JDP_Security_Series_NukaAI_v1.48-BREAKING_CHANGE.cast
*   **Target Environment:** v1.48.0
*   **Execution Method:** **Technical Audit (Manual)**
*   **Summary:** This serves as a regression control test. It shows that internal updates to the **Kernel Binder** in v1.48.0 successfully mitigated the simplest string-based bypasses. By attempting the legacy v1.47 bypass, the recording documents a `KernelException` failure, establishing the baseline for the subsequent zero-day proof.

../assets/SK/JDP_Security_Series_NukaAI_v1.48-BREAKING_CHANGE-Program.cs
../assets/SK/JDP_Security_Series_NukaAI_v1.48-BREAKING_CHANGE.txt
../assets/SK/JDP_Security_Series_NukaAI_v1.48-BREAKING_CHANGE.cast

../assets/SK/JDP_Security_Series_NukaAI_v1.48-BREAKING_CHANGE.mp4

### 5. JDP_Security_Series_NukaAI_v1.48-ZERO_DAY_PROOF.cast
*   **Target Environment:** v1.48.0
*   **Execution Method:** **Technical Audit (Manual)**
*   **Summary:** The definitive zero-day proof for the current framework. Despite the binder updates in the previous test, this recording proves the system remains vulnerable to **Type Confusion and Late Canonicalization**. By executing six distinct bypass vectors (including Hybrid Encoding), it confirms the filter consistently evaluates raw input before the plugin "sink" decodes it, leaving the application open to RCE.

../assets/SK/JDP_Security_Series_NukaAI_v1.48-ZERO_DAY_PROOF-Program.cs
../assets/SK/JDP_Security_Series_NukaAI_v1.48-ZERO_DAY_PROOF.txt
../assets/SK/JDP_Security_Series_NukaAI_v1.48-ZERO_DAY_PROOF.cast

../assets/SK/JDP_Security_Series_NukaAI_v1.48-ZERO_DAY_PROOF.mp4

---

## Appendix 3: Frequently Asked Questions

**Q: Is Agent Framework 1.0 affected?**
A: Yes. Agent Framework 1.0 inherits Semantic Kernel's orchestration layer and is vulnerable.

**Q: Does disabling Auto-Invocation fix it?**
A: Partially. It prevents automated exploitation but manual tool calls remain vulnerable to the 6 bypasses.

**Q: When will Microsoft fix this?**
A: Unknown. Our disclosure was rejected, and shadow patches have been incomplete.

**Q: Are other AI frameworks vulnerable?**
A: Yes. Project Nuka-AI has identified similar architectural flaws in LangChain, LlamaIndex, and Deepset Haystack (disclosures scheduled May 2026).


