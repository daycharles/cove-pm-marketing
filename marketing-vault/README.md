# CovePM Marketing Vault

This folder is an Obsidian vault for CovePM marketing strategy, research, agent sessions,
schedules, content, experiments, and website notes.

## Open the vault

In Obsidian, choose **Open folder as vault** and select this `marketing-vault` folder.

The vault is intentionally stored inside the website repository so marketing documentation can
be versioned beside the site. Website source remains one level above this folder.

## Working conventions

- Add research findings to `Research/` with source URLs and access dates.
- Add each agent run or meaningful work session to `Sessions/`.
- Keep recurring work and next run times in `Schedules/`.
- Record experiments and observed outcomes in `Experiments/`.
- Update `Dashboard.md` when adding a major artifact or changing priorities.
- Do not store credentials, API keys, restricted personal data, or secrets here.

## Source control and sharing

This vault is the shared marketing source of truth and is versioned in the parent GitHub repository.
See [[SHARING]] for Michael's setup and the pull/commit/push workflow. Obsidian workspace state and
plugin binaries stay local; `.obsidian/community-plugins.json` records the plugins to install.

## Website connection

- Site repository root: `..`
- Deployable entry point: `../index.html`
- Hosting configuration: `../.openai/hosting.json`
- Public site: https://covepm.averion.com
