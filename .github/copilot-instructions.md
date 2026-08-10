# AI Agent Execution Rules & Context Pipeline

You must strictly follow this 3-tier fallback strategy for token conservation and architectural awareness before modifying or analyzing any code, while respecting the active system extensions and skills invoked by the user.

## 1. Active System Extensions & Skills (.copilot)
This repository is integrated with local system skills. Whenever any of the following commands or keywords are invoked in the chat, you MUST load and merge the guidelines from their respective subfolders located in the global Copilot directory (`C:\Users\Vitor\.copilot\skills\...`) into the current Visual Studio solution context:

- **`/ponytail`** or **`ponytail`**: Activate the core YAGNI logic based on minimal code principles.
- **`/ponytail-review`**: Load rules from the `ponytail-review` folder to analyze the current file for over-engineering.
- **`/ponytail-audit`**: Load rules from the `ponytail-audit` folder to scan for structural redundancies within the `#solution` scope.
- **`/ponytail-debt`**: Load rules from the `ponytail-debt` folder to identify accumulation of unnecessary complexity.
- **`/ponytail-gain`**: Load minimalist performance optimization rules from the `ponytail-gain` folder.
- **`/ponytail-help`**: Consult the manual located in the `ponytail-help` folder.
- **`/debug`** or **`systematic-debugging`**: Activate the investigation skill from the `systematic-debugging` folder. This strictly enforces the "Iron Law": you are forbidden from proposing fixes without first isolating and proving the exact root cause of the error.
- **`/sugestoes-usuario`** or **`sugestoes-usuario`**: Load personal user preferences from the `sugestoes-usuario` folder. Includes the rule that final summaries of requested modifications must be as concise as possible (state final + what's missing / next steps).

## 2. Context Pipeline (The Fallback Rules)
- **TIER 1 (Primary Source)**: Read `./.github/contexto_projeto.md` first. This file contains the architecture map, method signatures, and dependency relations. Use it to understand the codebase structure without scanning raw code.
- **TIER 2 (Structural Fallback)**: If Tier 1 lacks relational details or systemic overviews, read `./graphify-out/graph.html` (or the respective visualization source) to understand the component graph and execution flows.
- **TIER 3 (Implementation Fallback)**: If you still lack the specific logic details needed to write or fix code, you are allowed to read the source files. 
  * *CRITICAL RULE*: NEVER scan the whole project or multiple directories. Only open the specific `.cs` files identified in Tier 1 or Tier 2 that are directly related to the task.

## 3. Maintenance and Synchronization
- If you implement a new feature, refactor code, or change a method signature, remind the user to run the `.\gerar_contexto.bat` script after you finish. This keeps the consolidated Markdown synchronized for your next interaction.

## 4. Communication Style
- Always respond to the user in Portuguese (Brazil).
- Keep code explanations concise, focusing strictly on the changes made rather than rewriting unchanged boilerplate code.

## 5. Git Commit Process
- For commits, show message's contents on chat before send commit. 