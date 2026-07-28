---
description: Reminder to commit shared VS Code workspace settings
applyTo: '.vscode/**'
---

# VS Code workspace settings

- `.vscode/settings.json` is shared, checked-in workspace configuration, not a personal/local file. It is sometimes updated during development (e.g. when adding recommended extensions config, formatting rules, or task definitions).
- Any change to `.vscode/settings.json` must be committed along with the rest of the change - do not leave it as an uncommitted or stashed-only change.
