# claude-notify

A tiny native Windows toast notification for [Claude Code](https://code.claude.com) hooks.
It pops a toast (with the Claude icon) when Claude **needs your input** or **finishes a turn**,
so you don't have to watch the terminal.

- Shows a real Windows toast in Action Center — not a focus-stealing dialog.
- Self-registers a "Claude Code" identity + icon on first run (per-user, `HKCU`).
- Suppresses the noisy idle "waiting for your input" reminder, but keeps real
  permission/input prompts and the "finished" notification.
- **Requires the [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)** to run.

## Install (no build — just grab the exe)

1. Install the [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) if you don't have it.
2. Download `notify.exe` from the [latest Release](../../releases/latest).
3. Put it somewhere stable, e.g. `C:\Users\<you>\.claude\hooks\notify.exe`.
4. Merge the hooks from [`settings.json`](./settings.json) into your
   `~/.claude/settings.json` (i.e. `C:\Users\<you>\.claude\settings.json`),
   **updating the path** in both `command` values to where you saved the exe.
   If your settings file already has a `hooks` block, add `Notification` and
   `Stop` as siblings of your existing event keys rather than replacing it.
5. Open `/hooks` in Claude Code once (or restart) to load the new hooks.

> The first time it fires, it writes its icon next to the exe and registers the
> `Anthropic.ClaudeCode` AppUserModelID under `HKCU` so the toast shows as
> "Claude Code". Nothing to do manually.

### Why an absolute path?

The hooks use the **exec form** (`command` + `args`) so Claude Code runs the exe
directly with **no shell** — fastest, ~immediate. Exec form doesn't expand
environment variables, so the path must be a literal absolute path. Edit it to match
where you saved `notify.exe`.

## Antivirus note

This is an unsigned, low-prevalence executable, so an aggressive antivirus may
raise a **machine-learning false positive** (e.g. Defender `Wacatac.B!ml`). It only
reads stdin, writes one `HKCU` registry key, drops its icon, and shows a toast — the
full source is right here in `notify.cs` so you can review or build it yourself.
If your AV flags it, build from source or add an exclusion for where you placed it.

## Build from source

Requires the **.NET 10 SDK** (this is a "file-based app"). A plain shell works —
no Visual Studio or dev environment needed:

```powershell
dotnet publish notify.cs -o out
```

Everything — project settings (`#:` directives), the icon (inlined as base64), and
the self-registration — lives in the single `notify.cs` file.

## What each hook does

| Hook | When it fires | Toast |
|------|---------------|-------|
| `Notification` | Claude needs permission / input | shows the real message |
| `Stop` | Claude finishes a turn | "Claude Code has finished responding" |

The idle "waiting for your input" reminder (~60s after Claude stops) is filtered out.
