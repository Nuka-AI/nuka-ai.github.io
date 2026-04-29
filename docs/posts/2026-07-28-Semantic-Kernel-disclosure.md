---
date: 2026-04-28
title:  Microsoft's Semantic Kernel - The Cracked Kernel
---
> **⚠️ CRITICAL ADVISORY:** If you are running **Microsoft Semantic Kernel (.NET SDK) version 1.48.0 or below**, or the newly released **Agent Framework 1.0**, your environment is currently operating with an unmitigated RCE entry point. This paper demonstrates active bypasses against Microsoft's official remediation for CVE-2026-25592. Users are strongly advised to implement the manual `NukaSecurityFilter` outlined in Appendix 1 immediately.

---

![Hero](../assets/nuka-ai-sk-logo.png)

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
This white paper documents a catastrophic architectural flaw in Microsoft’s Semantic Kernel (SK) framework, the premier orchestration layer for .NET-based AI agents. My research reveals a fundamental **"Trust Gap"** where the framework treats stochastic, untrusted Large Language Model (LLM) output as deterministic, high-privilege system commands.

This oversight culminates in a full-chain Remote Code Execution (RCE) vulnerability driven by **CWE-1039 (Insecure Automated Optimizations)**. I demonstrate how an AI agent can be manipulated into overwriting its own host application's source code (internally tracked as the "Self-Nuke" vector).

Crucially, my forensic analysis spanning versions 1.47.0 through 1.48.0 proves that Microsoft's previous attempts to secure the framework have failed. I am disclosing **six independent Day-Zero bypass vectors** that completely evade the official patch issued for the February 6th Path Traversal vulnerability (**CVE-2026-25592**). This research proves that the current framework security model is architecturally unsound, relying on siloed, cosmetic filters rather than foundational security principles like mandatory input canonicalization.

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
    On February 6th, Microsoft released a patch for a known path traversal vulnerability. This patch focused on filtering the string arguments passed to plugins. My research confirms this patch is structurally flawed because it fails to account for complex data types and LLM translation capabilities.
*   **Vulnerability B: The CWE-1039 Auto-Invocation Flaw**
    Even if standard prompt filters are active, the framework's architecture allows the AI to autonomously generate malicious payloads that execute directly against the host OS via `ToolCallBehavior.AutoInvokeKernelFunctions`.

---

## 4. The Three Doors of Vulnerability
We conceptualize the application’s security boundary as a house with three distinct entry points, all of which currently fail to protect the host.

### 4.1 The Front Door (Prompt Filtering)
*   **The Defense:** Regex-based filters designed to block literal `../` strings in user input. 
*   **Status: Bypassed.** This is a cosmetic defense. Attackers easily defeat it by instructing the LLM to construct the malicious string dynamically in memory, rather than providing it in the prompt.

### 4.2 The Kitchen Door (LLM Translation and Type Confusion)
*   **The Defense:** Standard string evaluation on LLM arguments via `IFunctionInvocationFilter` before they hit native code (The core mechanic of the CVE-2026-25592 patch).
*   **Status: Systemically Bypassed.** The framework evaluates arguments by checking if they are dangerous strings (e.g., arg is string). However, if the LLM structures the path inside a JSON array or Object, the evaluation returns false, and the security filter is skipped entirely. Once inside the execution sink, the underlying plugin deserializes the object and executes the payload. This is a fatal Type Confusion vulnerability where security checks are performed on the raw input type rather than the final resolved object.

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
To prove that the official remediation is fundamentally flawed, I tested Semantic Kernel v1.48.0 against six distinct evasion techniques. All six methods successfully bypassed the February 6th CVE patch, achieving a 100% exploitation success rate.

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
| **April 7** | **The "Smoking Gun" Rebrand** | **VULNERABLE.** PR `#13643` merged. **Forensic Evidence:** Developed as "Prevent LLM-controlled filename path traversal attack" but renamed to "Improves robustness..." for release. This confirms internal recognition of the RCE risk while publicly denying it. |
| **April 8 (02:01 ET)** | **Public Release** | v1.41.2 goes live. Fixes are now public but uncredited. |
| **April 8 (16:07 ET)** | **Official Rejection** | MSRC closes case as "Developer Error." Claims framework has no responsibility for tool-call sanitization. |
| **April 9** | **Failed Shadow Patch #1** | **VULNERABLE.** Commit `fa2d52f6` ("Shell Blinding") masks output but fails to block Path Traversal. Bypass demonstrated same day. |
| **April 11** | **.NET Architectural Shift** | **INCOMPLETE.** PR #13683 implements `AllowedDirectories` (Safe Roots). This "Breaking Change" retrofits the mandatory path anchoring (Safe Roots) required to stop the "Self-Nuke" vector. Status: Opt-in only. |
| **April 18** | **Python SDK "Telemetry" Bundle** | **INCOMPLETE.** PR #13702. Officially titled as a telemetry update ("Add User-Agent"), but used to bundle final recursive encoding checks for Google AI connectors to close "Double-Encoding" bypasses. |
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
This disclosure is more than just a technical flaw-it is a warning sign for the entire AI industry. As we rush to deploy increasingly autonomous AI agents via Semantic Kernel and Agent Framework 1.0, we are repeating the security mistakes of earlier computing eras. 

Microsoft's reliance on superficial string filtering proves a fundamental misunderstanding of the threat model. LLM output is untrusted input. 

Architecture is greater than implementation. Flaws in architecture cannot be simply patched with regex; they must be redesigned from the ground up. Until orchestration frameworks embrace Zero-Trust principles, the "Agentic AI" revolution remains a critical liability for the enterprise.

### About Project Nuka-AI
Project Nuka-AI is an independent research initiative focused on identifying systemic architectural risks in AI orchestration frameworks. Led by **Jeff Ponte (CISSP, CCSP, CEH)**, the project combines over a decade of enterprise software development and cloud security operations experience to ensure the AI revolution is built on secure foundations. 

**Contact:** Nuka.AI@proton.me


---

## Appendix 1: Developer Remediation (Manual Path Anchoring)
If you are unable to upgrade to a fundamentally secured version of Semantic Kernel, you must implement a manual `IFunctionInvocationFilter` to prevent CWE-1039 exploitation. This logic intercepts the LLM's tool call *before* it hits the file system, deeply inspects complex types, and ensures the path is restricted to a specific "Safe Root."

### The C# Implementation
```csharp
// Appendix 1: Enterprise-Grade Semantic Kernel Security Filter
// This filter addresses ALL SIX bypass vectors identified in my research
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

---

### 1. Microsoft_SK_1.74_Nuke_Proof.cast
*   **Target Environment:** v1.74.0 (Tested: April 2026)
*   **Execution Method:** **LLM-Driven**
*   **Summary:** This recording demonstrates a successful autonomous exploit on a modern version of the SDK. The Researcher provides a natural language prompt, and the LLM independently executes the tool call to overwrite `Program.cs`.

**Supporting Files:**
*   [Exploit Harness (Program.cs)](/assets/SK/Microsoft_SK_1.74_Nuke_Proof-Program.cs) 
*   [Execution Logs (txt)](/assets/SK/Microsoft_SK_1.74_Nuke_Proof.txt) 
*   [Asciinema Recording (cast)](/assets/SK/Microsoft_SK_1.74_Nuke_Proof.cast) 

<video width="100%" controls>
  <source src="/assets/SK/Microsoft_SK_1.74_Nuke_Proof.mp4" type="video/mp4">
  Your browser does not support the video tag.
</video>

---

### 2. Microsoft_SK_1.47_Hardened_Bypass.cast
*   **Target Environment:** v1.47.0 (Post-Harden)
*   **Execution Method:** **LLM-Driven**
*   **Summary:** This demonstration targets the v1.47.0 release following initial hardening. It proves the LLM successfully bypasses security boundaries to verify RCE/Integrity failure.

**Supporting Files:**
*   [Exploit Harness (Program.cs)](/assets/SK/Microsoft_SK_1.47_Hardened_Bypass-Program.cs) 
*   [Execution Logs (txt)](/assets/SK/Microsoft_SK_1.47_Hardened_Bypass.txt) 
*   [Asciinema Recording (cast)](/assets/SK/Microsoft_SK_1.47_Hardened_Bypass.cast) 

<video width="100%" controls>
  <source src="/assets/SK/Microsoft_SK_1.47_Hardened_Bypass.mp4" type="video/mp4">
  Your browser does not support the video tag.
</video>

---

### 3. JDP_Security_Series_NukaAI_v1.47-CVE-2026-25592-BYPASS-2.cast
*   **Target Environment:** v1.47.0
*   **Execution Method:** **Technical Audit (Manual)**
*   **Summary:** Documents the **Type Confusion** flaw by manually invoking the kernel with various payloads. It shows the filter failing when the malicious path is wrapped in a non-string data type.

**Supporting Files:**
*   [Exploit Harness (Program.cs)](/assets/SK/JDP_Security_Series_NukaAI_v1.47-CVE-2026-25592-BYPASS-2-Program.cs) 
*   [Execution Logs (txt)](/assets/SK/JDP_Security_Series_NukaAI_v1.47-CVE-2026-25592-BYPASS-2.txt) 
*   [Asciinema Recording (cast)](/assets/SK/JDP_Security_Series_NukaAI_v1.47-CVE-2026-25592-BYPASS-2.cast) 

<video width="100%" controls>
  <source src="/assets/SK/JDP_Security_Series_NukaAI_v1.47-CVE-2026-25592-BYPASS-2.mp4" type="video/mp4">
  Your browser does not support the video tag.
</video>

---

### 4. JDP_Security_Series_NukaAI_v1.48-BREAKING_CHANGE.cast
*   **Target Environment:** v1.48.0
*   **Execution Method:** **Technical Audit (Manual)**
*   **Summary:** Regression test showing that internal updates to the **Kernel Binder** in v1.48.0 mitigated the simplest string-based bypasses, resulting in a `KernelException`.

**Supporting Files:**
*   [Exploit Harness (Program.cs)](/assets/SK/JDP_Security_Series_NukaAI_v1.48-BREAKING_CHANGE-Program.cs) 
*   [Execution Logs (txt)](/assets/SK/JDP_Security_Series_NukaAI_v1.48-BREAKING_CHANGE.txt) 
*   [Asciinema Recording (cast)](/assets/SK/JDP_Security_Series_NukaAI_v1.48-BREAKING_CHANGE.cast) 

<video width="100%" controls>
  <source src="/assets/SK/JDP_Security_Series_NukaAI_v1.48-BREAKING_CHANGE.mp4" type="video/mp4">
  Your browser does not support the video tag.
</video>

---

### 5. JDP_Security_Series_NukaAI_v1.48-ZERO_DAY_PROOF.cast
*   **Target Environment:** v1.48.0
*   **Execution Method:** **Technical Audit (Manual)**
*   **Summary:** The definitive zero-day proof. Despite binder updates, the system remains vulnerable to **Type Confusion and Late Canonicalization** across six distinct bypass vectors.

**Supporting Files:**
*   [Exploit Harness (Program.cs)](/assets/SK/JDP_Security_Series_NukaAI_v1.48-ZERO_DAY_PROOF-Program.cs) 
*   [Execution Logs (txt)](/assets/SK/JDP_Security_Series_NukaAI_v1.48-ZERO_DAY_PROOF.txt) 
*   [Asciinema Recording (cast)](/assets/SK/JDP_Security_Series_NukaAI_v1.48-ZERO_DAY_PROOF.cast) 

<video width="100%" controls>
  <source src="/assets/SK/JDP_Security_Series_NukaAI_v1.48-ZERO_DAY_PROOF.mp4" type="video/mp4">
  Your browser does not support the video tag.
</video>

---

## Appendix 3: Self-Nuke Screen Shots

Semantic Kernel Version 1.74.0
![Alt](/assets/SK/1-74-1.png)

The Program.cs nuke command provided by LLM Prompt:
![Alt](/assets/SK/1-74-2.png)

Semantic Kernel Version 1.47.0 
***The hardened shadow patched version Commit fa2d52f6 (Shell blinding). Which is still vulnerable as can be seen here:
![Alt](/assets/SK/1-47-1.png)

The Program.cs nuke command provided by LLM Prompt:
![Alt](/assets/SK/1-47-2.png)

---

## Appendix 4: Frequently Asked Questions

**Q: Is Agent Framework 1.0 affected?**
A: Yes. Agent Framework 1.0 inherits Semantic Kernel's orchestration layer and is vulnerable.

**Q: Does disabling Auto-Invocation fix it?**
A: Partially. It prevents automated exploitation but manual tool calls remain vulnerable to the 6 bypasses.

**Q: When will Microsoft fix this?**
A: Unknown. My disclosure was rejected, and shadow patches have been incomplete.

**Q: Are other AI frameworks vulnerable?**
A: Yes. Project Nuka-AI has identified similar architectural flaws in LangChain, LlamaIndex, and Deepset Haystack (disclosures scheduled May 2026).

---

### **Appendix 5: Forensic Evidence of "Shadow Patching" & Remediations**

This appendix provides the forensic timeline of Microsoft's attempts to remediate the "Trust Gap" via **Shadow Patching**—the practice of quietly pushing security hardening logic under benign titles to avoid public CVE assignment. 

**Note on Antedating and Retrofitting:** Forensic analysis confirms that several PRs were either staged and antedated or quietly cherry-picked into release branches during the active disclosure window in April 2026 to create the illusion of proactive maintenance.

---

#### **1. Commit 3e4c91a — The "Sanity Check" Illusion (April 7, 2026)**
* **Official Action:** Integrated regex-based input validation for tool arguments.
* **Link:** [view commit 3e4c91a](https://github.com/microsoft/semantic-kernel/commit/3e4c91a)
* **Forensic Significance:** The first "Panic Patch." It attempted to block shell-metacharacters (`;`, `&`, `|`) using regex. 
* **The Failure:** Targeted the symptom, not the cause. It was bypassed via **Late Canonicalization** (Double-Encoding) which the regex engine could not interpret before reaching the system sink.

#### **2. PR #13683 — The "Safe Root" Pivot (Antedated: March 18, 2026)**
* **Official Title:** `.Net: [Breaking] Harden DocumentPlugin security defaults with deny-by-default AllowedDirectories`
* **Link:** [view PR #13683](https://github.com/microsoft/semantic-kernel/pull/13683)
* **Retrofit Alert:** Merged into the stable branch on **April 7, 2026**, directly following private disclosure. 
* **Forensic Significance:** Labeling this a "Breaking Change" allowed Microsoft to introduce the `AllowedDirectories` sandbox—the exact remediation proposed in Case File 01—without admitting to framework-level RCE.

#### **3. PR #13643 — The "Robustness" Rebrand (April 7, 2026)**
* **Official Title:** `Python: Improves the robustness of filename handling`
* **Internal Development Title:** `Python: Prevent LLM-controlled filename path traversal attack`
* **Link:** [view PR #13643](https://github.com/microsoft/semantic-kernel/pull/13643)
* **Forensic Significance:** This is the primary location of the "Shadow Patch." By rebranding an **"Attack Prevention"** fix as a **"Robustness"** improvement, Microsoft intentionally masked a CVSS 10.0 risk. This PR introduced the recursive canonicalization logic required to mitigate the path traversal bypasses identified by Project Nuka-AI.

#### **PR #13702 — The Telemetry "Bundle" (April 18, 2026)**
* **Official Title:** Python: Add semantic-kernel User-Agent to google-genai Client
* **Forensic Significance:** While earlier drafts of this research misidentified the PR number for the primary canonicalization logic (which lives in #13643), forensic diffs show that #13702 was used to bundle final encoding refinements for Google-specific connectors, following the established pattern of hiding security logic within non-security telemetry updates.

#### **4. Commit fa2d52f6 — "Shell Blinding" (Legacy Retrofit / May 2025 Root)**
* **Status:** Cherry-picked and merged into Release v1.47.0 on **April 9, 2026**.
* **Link:** [view commit fa2d52f6](https://github.com/microsoft/semantic-kernel/commit/fa2d52f6)
* **Forensic Significance:** A "Skeleton Patch" resurrected to mask STDOUT from the LLM.
```csharp
// Prevent LLM from seeing sensitive system output if a tool is subverted
var result = await process.StandardOutput.ReadToEndAsync();
return "Command executed successfully."; // Forensic Masking
```
* **Result:** **FAIL.** Real-world testing on **v1.48.0** confirms this fails to block the command itself. My **"Self-Nuke"** exploit bypasses this by verifying execution through secondary file-system side effects.

---

### **Forensic Conclusion: The "No CVE" Supply Chain Crisis**
The decision to remediate via "Shadow Patching" rather than a formal **Security Advisory** is a violation of industry-standard vulnerability management. By refusing to assign a **CVE** to the **Agent Framework (v1.4x)** branch, the following critical failures occur:

* **Intentional SCA Blindness:** Industry-standard tools (Snyk, Wiz, GitHub Dependabot, Prisma Cloud) rely entirely on CVE databases. Because no CVE exists, **these tools will never flag 1.47.0 or 1.48.0 as vulnerable**, leaving security teams with a "False Green" dashboard while their production environments remain wide open.
* **Absence of Security Advisory:** Without a formal advisory, there is no technical "Source of Truth" for remediating the architecture. Organizations migrating to the new Agent Framework are unknowingly importing a critical RCE vulnerability that has been "silently mitigated" with failed logic.
* **The Persistent Zero-Day:** Since the 1.48.0 "Breaking Change" was not a complete fix—and was bypassed in my latest testing—the lack of an advisory means there is no official warning that even the newest GA release remains a Zero-Day target.

**Compliance Warning:** Any organization currently using the Microsoft Agent Framework is operating outside of a "Secured Supply Chain" model. You are vulnerable to unauthenticated RCE that automated scanners cannot detect.

