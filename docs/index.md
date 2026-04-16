---
hide:
  - navigation
---

# The Nuka AI Research Series

![Nuka-AI Research](assets/hero.png)

## Investigating Security in AI Orchestration Frameworks

Welcome to the central research hub for the **Nuka AI Research Series**. This initiative, led by **JDP Security**, focuses on identifying architectural trust gaps and critical persistence vectors in modern AI ecosystems.

---

### Active Research Tracks

| | |
| :--- | :--- |
| ![Research 1](assets/project-1-rusted.png) | **Classified: The Abstraction Leak**<br>Status: `[AWAITING RESPONSIBLE DISCLOSURE]`<br>**CVSS:** `10.0 (CRITICAL)`<br>**Vector:** `AV:N/AC:L/PR:N/UI:N/S:C/C:H/I:H/A:H`<br>**Justification:** This exploit allows for unauthenticated remote code execution (RCE). No user interaction or local privileges are required to achieve a total breach of confidentiality and integrity. |
| ![Research 2](assets/project-2-rusted.png) | **Classified: The Index Collapse**<br>Status: `[AWAITING RESPONSIBLE DISCLOSURE]`<br>**CVSS:** `10.0 (CRITICAL)`<br>**Vector:** `AV:N/AC:L/PR:N/UI:N/S:C/C:H/I:H/A:H`<br>**Justification:** By manipulating the orchestration state, an attacker can force a full system compromise that escapes the application sandbox. The impact is "Critical" as it weaponizes the core retrieval mechanism against the host. |
| ![Research 3](assets/project-3-rusted.png) | **Classified: The Broken Link**<br>Status: `[AWAITING RESPONSIBLE DISCLOSURE]`<br>**CVSS:** `10.0 (CRITICAL)`<br>**Vector:** `AV:N/AC:L/PR:N/UI:N/S:C/C:H/I:H/A:H`<br>**Justification:** This "No-CVE Trap" exploits a design flaw in the trust model of the framework. It permits remote, unauthenticated actors to hijack the supply chain logic, leading to silent, persistent administrative takeover. |
| ![Research 4](assets/project-4-rusted.png) | **Classified: The Ghost in the Machine**<br>Status: `[AWAITING RESPONSIBLE DISCLOSURE]`<br>**CVSS:** `10.0 (CRITICAL)`<br>**Vector:** `AV:N/AC:L/PR:N/UI:N/S:C/C:H/I:H/A:H`<br>**Justification:** The terminal event. This vulnerability demonstrates full-spectrum host compromise. It requires no authentication and leaves zero forensic footprints, granting an external "Ghost" total control over the underlying cloud infrastructure. |

---

### 📅 July 2026 Disclosure Timeline
Technical white papers (including **Blog Posts**, **Security Advisories**, and **Blue Team Reports**) are currently held in **Secure Storage**. 

* **July 14:** Case File 01 - The Abstraction Leak `[CVSS 10.0]` `[LOCKED]`
* **July 16:** Case File 02 - The Index Collapse `[CVSS 10.0]` `[LOCKED]`
* **July 21:** Case File 03 - The Broken Link `[CVSS 10.0]` `[LOCKED]`
* **July 23:** Case File 04 - The Ghost in the Machine `[CVSS 10.0]` `[LOCKED]`
* **July 28:** **Industry Retrospective: The Fallout** `[Ecosystem Post-Mortem]`

**Contact:** [Nuka.AI@proton.me](mailto:Nuka.AI@proton.me) | [JDP.Sec@proton.me](mailto:JDP.Sec@proton.me)
