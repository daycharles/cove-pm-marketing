# Sharing the Averion Compass Marketing Vault

This Obsidian vault is the operational source of truth during the local marketing-workbench MVP.
GitHub is the shared history; Obsidian is the working interface. Notion is a future marketing
workspace, and Linear remains the product-development system.

## Michael's setup

1. Clone `https://github.com/daycharles/cove-pm-marketing.git`.
2. Open the cloned `marketing-vault` folder in Obsidian using **Open folder as vault**.
3. Install and enable the community plugins listed in `.obsidian/community-plugins.json`.
4. Complete [[Schedules/Obsidian Setup Checklist]].
5. Open [[Dashboard]] and [[Kanban/Averion Compass Marketing Board]].

## Collaboration rules

- Pull before starting work.
- Make focused changes and commit them with a clear message.
- Pull again before pushing if another person may have worked in the vault.
- Do not commit credentials, API keys, private customer data, or secrets.
- Do not enable Obsidian Git auto-push until the team agrees on a workflow.
- Treat `Dashboard.md`, `Schedules/`, and `Sessions/` as shared operational records.
- Resolve conflicts in Markdown carefully; do not accept an entire side blindly.

## What is shared

- Markdown notes, templates, schedules, research, experiments, leads, pilots, and the native Kanban board.
- Obsidian core/plugin manifests needed to reproduce the setup.

## What stays local

- Obsidian workspace layout, caches, plugin binaries, and other machine-specific state.
