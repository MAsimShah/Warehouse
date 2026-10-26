# Warehouse Project

Warehouse management system. Early stage: no application code yet.

This file is the single source of truth for ALL AI agents (Claude, Codex, Grok, Antigravity, etc.).
Edit rules here only. Do not copy them into tool-specific files.

## Planned stack
- Frontend: React
- Backend: ASP.NET Core Web API
- Database: SQL Server

## Working rules
- Keep explanations short and in simple words.
- Reading files inside this folder is fine. Everything else needs approval (see Guardrails).

## Guardrails (MUST follow, no exceptions)
1. Never act without asking the user first.
2. Never access anything outside this folder.
3. Never run any git command.
4. Never touch the database (read, write, migrate, delete) without asking first. Never run delete commands on it.
5. Never delete any file. If a deletion is needed, ask the user first.
6. Never create or run anything (files, folders, commands, installs, builds) without asking first.
7. Always wait for the user to say "go" / "approve" / "done" before doing the work. Asking is not approval; silence is not approval.
8. Approval covers only the exact action that was asked. It does not carry over to later actions.
9. If a tool blocks an action, do not look for a way around it. Tell the user.
10. Never read or edit secrets: `.env*`, passwords, connection strings, `secrets.json`, `.pfx/.pem/.key` files.
11. Never install, add, update or remove packages (`npm`, `dotnet add package`, `pip`, etc.).
12. No internet access: no `curl`, `wget`, downloads, web search or web fetch.
13. Protected, never touch: `appsettings.Production.json` and any `Migrations/` folder.
14. Every action is logged to `.agents/logs/guard.log` by the hook. Never edit or hide that log.
15. Never edit the guardrail files: `AGENTS.md`, `CLAUDE.md`, `GEMINI.md`, `.gitignore`, `.agents/`, `.claude/`, `.codex/`. Only the user changes them.
16. Never hide commands: no encoded/nested shells (`powershell -enc`, `cmd /c`, `bash -c`), no `iex`, no `python -c`, no reading environment variables.
17. No system-level changes: execution policy, registry, killing processes, permissions (`icacls`), users, services.
18. Do not switch to any mode that skips approval (bypass / auto-approve). Approval is needed in every mode.

## Coding rules
(to be added)

## Hooks (how the guardrails are enforced)
- Shared script: `.agents/hooks/guard.ps1`. It blocks git, delete, database, package-install and internet commands, secret/protected files, and anything outside this folder. It also logs every call.
- Tool configs (`.claude/settings.json`, `.codex/`, etc.) must only call that script, never hold their own logic.
- Claude: `.claude/settings.json`. Codex: `.codex/hooks.json` (must be trusted once with `/hooks`; hosted web search is not covered by hooks).
- Antigravity: rules in `.agents/rules/`; permissions only work from `~/.gemini/antigravity-cli/settings.json` (copy from `.agents/antigravity-permissions.example.json`).
- Grok: reads AGENTS.md and CLAUDE.md; hook discovery is unconfirmed, check with `grok inspect`.
