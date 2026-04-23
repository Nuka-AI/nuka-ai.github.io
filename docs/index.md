---
hide:
  - navigation
---

# Nuka-AI: AI Orchestration Research Series

![Nuka-AI Research](assets/hero.png)

## Investigating Security Trust Gaps in AI Frameworks

Welcome to the central research hub for the **Nuka-AI Research Series**. This initiative, led by **JDP Security**, focuses on identifying architectural trust gaps and critical persistence vectors in modern AI ecosystems.

---

### Active Research Tracks

| | |
| :--- | :--- |
| ![Research 1](assets/project-1-rusted.png) | **Case File 01: The Cracked Kernel The Abstraction Leak**<br>Status: `[COORDINATED DISCLOSURE IN PROGRESS]`<br>**CVSS:** `10.0 (CRITICAL)`<br>**Vector:** `AV:N/AC:L/PR:N/UI:N/S:C/C:H/I:H/A:H`<br>**Target:** Microsoft Semantic Kernel. <br>**Justification:** This exploit demonstrates unauthenticated RCE aswell as a RCE via a bypass of CVE-2026-25592. |
| ![Research 2](assets/project-5-rusted.png) | **Case File 02: Agent Down The Containment Breach**<br>Status: `[AWAITING RESPONSIBLE DISCLOSURE]`<br>**CVSS:** `10.0 (CRITICAL)`<br>**Vector:** `AV:N/AC:L/PR:N/UI:N/S:C/C:H/I:H/A:H`<br>**Target:** Microsoft Agent Framework.<br>**Justification:** Identified architectural flaw in containerized agent orchestration allowing for full host-level escape via manipulated execution contexts. |
| ![Research 2](assets/project-2-rusted.png) | **Case File 03: The Index Collapse**<br>Status: `[AWAITING RESPONSIBLE DISCLOSURE]`<br>**CVSS:** `10.0 (CRITICAL)`<br>**Vector:** `AV:N/AC:L/PR:N/UI:N/S:C/C:H/I:H/A:H`<br>**Target:** LlamaIndex<br>**Justification:** By manipulating orchestration state, an attacker can force a full system compromise. |
| ![Research 3](assets/project-3-rusted.png) | **Case File 04: The Broken Link**<br>Status: `[AWAITING RESPONSIBLE DISCLOSURE]`<br>**CVSS:** `10.0 (CRITICAL)`<br>**Vector:** `AV:N/AC:L/PR:N/UI:N/S:C/C:H/I:H/A:H`<br>**Target:** Langchain<br>**Justification:** Design flaw in the framework trust model permitting remote hijacking of supply chain logic. |
| ![Research 4](assets/project-4-rusted.png) | **Case File 05: The Ghost in the Haystack**<br>Status: `[AWAITING RESPONSIBLE DISCLOSURE]`<br>**CVSS:** `10.0 (CRITICAL)`<br>**Vector:** `AV:N/AC:L/PR:N/UI:N/S:C/C:H/I:H/A:H`<br>**Target:** Deepset Haystack<br>**Justification:** Terminal event. Full-spectrum host compromise with zero forensic footprint. |

---

### 📅 April/May 2026 Disclosure Timeline
Technical white papers currently held in **Secure Storage**. 

* **April 28, 2026:** [Case File 01: The Cracked Kernel The Abstraction Leak (RCE Bypass)](posts/2026-07-14-initial-disclosure.md) `[ACTIVE]` **CVSS:** `10.0 (CRITICAL)`
* **May 5, 2026:** Case File 02 - Agent Down The Containment Breach `[LOCKED]` **CVSS:** `10.0 (CRITICAL)`
* **May 12, 2026:** Case File 03 - The Index Collapse `[LOCKED]` **CVSS:** `10.0 (CRITICAL)`
* **May 19, 2026:** Case File 04 - The Broken Link `[LOCKED]` **CVSS:** `10.0 (CRITICAL)`
* **May 26, 2026:** Case File 05 - The Ghost in the Haystack `[LOCKED]` **CVSS:** `10.0 (CRITICAL)`
* **June 5, 2026:** **Industry Retrospective: The Fallout `[Ecosystem Post-Mortem]`

---

**Inquiries:** [JDP.sec@proton.me](mailto:JDP.sec@proton.me) | [Nuka.AI@proton.me](mailto:Nuka.AI@proton.me)
