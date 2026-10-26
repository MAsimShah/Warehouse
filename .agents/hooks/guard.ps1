# Shared guardrail hook. Reads the tool call as JSON on stdin.
# exit 2 = block. JSON on stdout with permissionDecision "ask" = make the user approve. exit 0 = allow.
# Rules are described in AGENTS.md. Tool configs (.claude/settings.json etc.) only call this script.
$logDir = Join-Path $PSScriptRoot '..\logs'
$logFile = Join-Path $logDir 'guard.log'

function Log($status, $detail) {
    try {
        if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir | Out-Null }
        $line = "{0} {1} {2}" -f (Get-Date -Format 's'), $status, ($detail -replace '\s+', ' ')
        Add-Content -Path $logFile -Value $line
    } catch { }
}

function Block($reason, $detail) {
    Log 'BLOCK' "$reason | $detail"
    [Console]::Error.WriteLine("BLOCKED by guardrails: $reason. Ask the user first.")
    exit 2
}

try {
    $data = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path.TrimEnd('\')
} catch {
    Block 'could not read hook input' ''
}

function InsideRoot($p) {
    $p = $p -replace '/', '\'
    if (-not [IO.Path]::IsPathRooted($p)) { $p = Join-Path $root $p }
    $full = [IO.Path]::GetFullPath($p).TrimEnd('\')
    return ($full -eq $root) -or $full.StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase)
}

# Secret / protected files (read AND write are blocked).
$protected = '(?i)(^|[\\/\s"''=])(\.env[\w.\-]*|appsettings\.Production\.json|secrets\.json|[^\\/\s"'']*\.(pfx|pem|key))(?=$|[\\/\s"'';|&)])|[\\/]Migrations([\\/]|$)|(^|[\s"''=])Migrations[\\/]'

# Guardrail files themselves (writes are blocked, reading is fine).
$guardFiles = '(?i)(^|[\\/\s"''=])(AGENTS\.md|CLAUDE\.md|GEMINI\.md|\.gitignore)(?=$|[\s"'';|&)])|(^|[\\/\s"''=])\.(agents|claude|codex)([\\/]|$)'
$writeTools = '^(Write|Edit|MultiEdit|NotebookEdit)$'
$readOnlyTools = '^(Read|Glob|Grep)$'

$tool = $data.tool_name
$ti = $data.tool_input
$detail = "$tool $($ti.command)$($ti.file_path)$($ti.path)"

# File tools: path must stay inside the project folder and not be protected.
foreach ($p in @($ti.file_path, $ti.path, $ti.notebook_path)) {
    if ($p) {
        if (-not (InsideRoot $p)) { Block "path is outside the project folder ($p)" $detail }
        if ($p -match $protected) { Block "protected file or folder ($p)" $detail }
        if ($tool -match $writeTools -and $p -match $guardFiles) { Block "guardrail files cannot be edited by AI ($p)" $detail }
    }
}

# Search tools can read protected files by pattern.
if ($ti.pattern -and $ti.pattern -match $protected) { Block 'protected file or folder in search pattern' $detail }

# Shell tools (and Codex apply_patch, which carries the patch text in "command").
$cmd = $ti.command
if ($cmd) {
    $start = '(^|[\s;&|(`])'
    if ($cmd -match "$start(git|gh)(\s|$)")                                       { Block 'git commands are not allowed' $detail }
    if ($cmd -match "$start(rm|rmdir|rd|del|erase|unlink|ri|Remove-Item)(\s|$)")  { Block 'delete commands are not allowed' $detail }
    if ($cmd -match '(?i)\b(drop|truncate)\s+(table|database|schema)\b|\bdelete\s+from\b') { Block 'destructive SQL is not allowed' $detail }
    if ($cmd -match '(?i)\b(sqlcmd|sqlpackage|Invoke-Sqlcmd|osql)\b|dotnet\s+ef\b')        { Block 'database commands are not allowed' $detail }
    if ($cmd -match '(?i)\b(npm|npx|yarn|pnpm|bun|pip|pip3|choco|winget|scoop)\s+(install|i|add|ci|update|upgrade|remove|uninstall|exec|dlx)\b|dotnet\s+(add|remove|tool|restore|new|publish)\b|nuget\s+(install|restore|update)') {
        Block 'installing or changing packages is not allowed' $detail
    }
    if ($cmd -match '(?i)\b(curl|wget|Invoke-WebRequest|Invoke-RestMethod|iwr|irm|ssh|scp|ftp|telnet|nc|ncat|Start-BitsTransfer|bitsadmin|certutil)\b|https?://') {
        Block 'internet access is not allowed' $detail
    }
    if ($cmd -match $protected)                                                   { Block 'command touches a protected file or folder' $detail }

    # Hidden / nested commands that could dodge the checks above.
    if ($cmd -match '(?i)(^|[\s;&|(`])(powershell|pwsh)(\.exe)?\s+.*-(c|command|e|ec|enc|encodedcommand)\b|(^|[\s;&|(`])cmd(\.exe)?\s+/[ck]\b|(^|[\s;&|(`])(bash|sh|zsh|wsl)(\.exe)?\s+(-\w*c\b|-e\b|\S)') {
        Block 'nested shell or encoded command is not allowed' $detail
    }
    if ($cmd -match '(?i)\b(iex|Invoke-Expression|Start-Process|FromBase64String|certutil)\b|base64\s+(-d|--decode)|\b(python3?|py|node|perl|ruby|php)(\.exe)?\s+(-c|-e|-r)\b') {
        Block 'hidden code execution is not allowed' $detail
    }
    if ($cmd -match '(?i)\$env:|\$\{env:|\[Environment\]::|%[A-Za-z_]+%|\b(printenv|Get-ChildItem\s+env:|gci\s+env:|dir\s+env:)|(^|[\s;&|(`])(env|set)\s*($|[;&|])') {
        Block 'reading environment variables is not allowed' $detail
    }

    # System-level changes.
    if ($cmd -match '(?i)\b(Set-ExecutionPolicy|regedit|reg(\.exe)?\s+(add|delete|import|export|query|load|save)|taskkill|Stop-Process|icacls|takeown|netsh|schtasks|setx|shutdown|Restart-Computer|Stop-Computer|diskpart|Set-ItemProperty|New-ItemProperty|Remove-ItemProperty|Set-Service|Stop-Service|Disable-\w+|Add-LocalGroupMember|New-LocalUser)\b|\bnet(\.exe)?\s+(user|localgroup|use|share|stop|start)\b|\bsc(\.exe)?\s+(config|stop|delete|create)\b|HK(LM|CU|CR):|(^|[\s;&|(`])(kill|killall|pkill)(\s|$)') {
        Block 'system-level changes are not allowed' $detail
    }

    # Guardrail files: block anything that writes to them.
    if ($cmd -match $guardFiles -and ($tool -eq 'apply_patch' -or $cmd -match '(?i)>|\b(Set-Content|Add-Content|Out-File|Copy-Item|Move-Item|Rename-Item|New-Item|Clear-Content|tee|cp|mv|ni|sc|sed|truncate|attrib|ren)\b')) {
        Block 'guardrail files cannot be edited by AI' $detail
    }

    if ($cmd -match '\.\.[\\/]|(^|[\s"''=])~[\\/]?')                              { Block 'command reaches outside the project folder' $detail }
    foreach ($m in [regex]::Matches($cmd, '[A-Za-z]:[\\/][^\s"'';|&)]*')) {
        if (-not (InsideRoot $m.Value)) { Block "command touches a path outside the project folder ($($m.Value))" $detail }
    }
}

# Network tools are never allowed.
if ($tool -match '^(WebFetch|WebSearch)$') { Block 'internet access is not allowed' $detail }

# Anything that is not a plain read must be approved by the user, in every mode.
if ($tool -notmatch $readOnlyTools) {
    Log 'ASK' $detail
    @{ hookSpecificOutput = @{
        hookEventName            = 'PreToolUse'
        permissionDecision       = 'ask'
        permissionDecisionReason = 'Guardrails: the user must approve creating, editing or running anything.'
    } } | ConvertTo-Json -Depth 4 -Compress
    exit 0
}

Log 'ALLOW' $detail
exit 0
