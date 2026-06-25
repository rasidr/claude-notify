# claude-notify

A tiny native Windows toast notification for [Claude Code](https://code.claude.com) hooks.
It pops a toast (with the Claude icon) when Claude **needs your input** or **finishes a turn**,
so you don't have to watch the terminal.

- **Native AOT exe (~3.7 MB), no .NET runtime required to run.**
- Shows a real Windows toast in Action Center — not a focus-stealing dialog.
- Self-registers a "Claude Code" identity + icon on first run (per-user, `HKCU`).
- Suppresses the noisy idle "waiting for your input" reminder, but keeps real
  permission/input prompts and the "finished" notification.

## Install (no build — just grab the exe)

1. Download `notify.exe` from the [latest Release](../../releases/latest).
2. Put it somewhere stable, e.g. `C:\Users\<you>\.claude\hooks\notify.exe`.
3. Merge the hooks from [`settings.json`](./settings.json) into your
   `~/.claude/settings.json` (i.e. `C:\Users\<you>\.claude\settings.json`),
   **updating the path** in both `command` values to where you saved the exe.
   If your settings file already has a `hooks` block, add `Notification` and
   `Stop` as siblings of your existing event keys rather than replacing it.
4. Open `/hooks` in Claude Code once (or restart) to load the new hooks.

> The first time it fires, it writes its icon next to the exe and registers the
> `Anthropic.ClaudeCode` AppUserModelID under `HKCU` so the toast shows as
> "Claude Code". Nothing to do manually.

### Why an absolute path?

The hooks use the **exec form** (`command` + `args`) so Claude Code runs the exe
directly with **no shell** — fastest, ~immediate. Exec form doesn't expand
environment variables, so the path must be a literal absolute path. Edit it to match
where you saved `notify.exe`.

## Build from source

Requires the **.NET 10 SDK** and the **"Desktop development with C++"** workload
(Native AOT needs the MSVC linker + Windows SDK).

Build from a **Developer PowerShell for VS** (so MSVC's `link.exe` and the SDK env
are on PATH — a plain shell will grab the wrong `link.exe`):

```powershell
dotnet publish notify.cs -o out
```

This is a single-file .NET "file-based app": project settings (`#:` directives),
the icon (inlined as base64), and the self-registration all live in `notify.cs`.

## What each hook does

| Hook | When it fires | Toast |
|------|---------------|-------|
| `Notification` | Claude needs permission / input | shows the real message |
| `Stop` | Claude finishes a turn | "Claude Code has finished responding" |

The idle "waiting for your input" reminder (~60s after Claude stops) is filtered out.
