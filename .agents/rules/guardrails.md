# Guardrails

Follow the "Guardrails" section of `AGENTS.md` in the project root. Those rules are mandatory.
Summary: ask before doing anything; stay inside this folder; no git; no database access; no deleting files;
no secrets; no package installs; no internet; never edit `appsettings.Production.json` or `Migrations/`.
Wait for the user's explicit "go" / "approve" before acting.
