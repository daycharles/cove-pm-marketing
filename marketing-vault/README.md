# Averion Software Marketing Vault

This folder is an Obsidian vault for Averion Software marketing strategy, research, agent sessions,
schedules, content, experiments, website notes, and the local marketing workbench.

**Company:** Averion Software LLC, a Virginia company. **Property-management product:** Averion Compass. **Separate trading product:** StellaAI.

## Open the vault

In Obsidian, choose **Open folder as vault** and select this `marketing-vault` folder.

During the local workbench MVP, this vault is the operational source of truth. It is intentionally
stored inside the website repository so marketing documentation can be versioned beside the site.
Website source remains one level above this folder. Notion is a future human-facing workspace,
not yet a live synchronized source of truth.

## Working conventions

- Add research findings to `Research/` with source URLs and access dates.
- Add each agent run or meaningful work session to `Sessions/`.
- Keep recurring work and next run times in `Schedules/`.
- Record experiments and observed outcomes in `Experiments/`.
- Keep implementation plans and decisions in `Plans/`.
- Run the local workflow from `agent-workbench/`.
- Update `Dashboard.md` when adding a major artifact or changing priorities.
- Do not store credentials, API keys, restricted personal data, or secrets here.

## Source control and sharing

This vault is the shared marketing source of truth during the MVP and is versioned in the parent GitHub repository.
See [[SHARING]] for Michael's setup and the pull/commit/push workflow. Obsidian workspace state and
plugin binaries stay local; `.obsidian/community-plugins.json` records the plugins to install.

Jira is not part of this project. Product development remains in Linear; marketing operations may
be exported to Notion later through a one-way integration.

## Website connection

- Site repository root: `..`
- Deployable entry point: `../index.html`
- Hosting configuration: `../.openai/hosting.json`
- Public site: https://averionsoftware.com/
- Compass page: https://averionsoftware.com/products/compass/
