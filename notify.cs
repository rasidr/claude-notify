#:sdk Microsoft.NET.Sdk
#:property TargetFramework=net9.0-windows10.0.19041.0
#:property OutputType=WinExe
#:property Nullable=enable
#:property ImplicitUsings=enable
#:property RuntimeIdentifier=win-x64
#:property PublishAot=true
#:property DebugType=none
#:property DebugSymbols=false
#:property SatelliteResourceLanguages=en

// Claude Code notification hook: shows a native Windows toast when Claude needs
// input (Notification hook) or finishes a turn (Stop hook).
//
// Single-file .NET 10 "file-based app" with Native AOT — project settings, icon,
// and self-registration all live in this one file. Build from a
// "Developer PowerShell for VS" (Native AOT needs the MSVC linker + SDK env):
//   dotnet publish notify.cs -o <dir>
//
// Reads the hook payload (JSON) from stdin and uses its "message" field for the
// toast body. An optional first command-line argument supplies the default body
// when stdin has no message (used by the Stop hook).

using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32;
using Windows.UI.Notifications;

// Ensure our AppUserModelID is registered (writes the registry entry + the inlined
// icon on first run) so the toast shows as "Claude Code" with the Claude icon on
// ANY machine — no external setup needed.
try { Identity.Ensure(); } catch { }

const string title = Identity.DisplayName;

string message = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
    ? args[0]
    : "Claude Code is waiting for your input";

try
{
    string stdin = Console.In.ReadToEnd();
    if (!string.IsNullOrWhiteSpace(stdin))
    {
        using var doc = JsonDocument.Parse(stdin);
        if (doc.RootElement.TryGetProperty("message", out var m) &&
            m.ValueKind == JsonValueKind.String)
        {
            string? s = m.GetString();
            if (!string.IsNullOrWhiteSpace(s)) message = s;
        }
    }
}
catch { /* malformed/empty JSON: keep the default message */ }

// The Notification hook fires both for permission/input prompts AND for the idle
// "waiting for your input" reminder (~60s after Claude stops). Suppress only the
// idle reminder; show everything else (so a permission prompt is never swallowed).
// The Stop hook passes its message via args (fromArgs), so it is never affected.
bool fromArgs = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]);
if (!fromArgs && message.Contains("waiting for your input", StringComparison.OrdinalIgnoreCase))
    return;

// Tie this process to our AppUserModelID so Windows attributes the toast to it.
try { Native.SetCurrentProcessExplicitAppUserModelID(Identity.Aumid); } catch { }

try
{
    var xml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
    var texts = xml.GetElementsByTagName("text");
    texts.Item(0).AppendChild(xml.CreateTextNode(title));   // CreateTextNode escapes content for us.
    texts.Item(1).AppendChild(xml.CreateTextNode(message));

    // Keep the built-in ding.
    var audio = xml.CreateElement("audio");
    audio.SetAttribute("src", "ms-winsoundevent:Notification.IM");
    xml.DocumentElement.AppendChild(audio);

    var toast = new ToastNotification(xml);
    ToastNotificationManager.CreateToastNotifier(Identity.Aumid).Show(toast);
}
catch { /* never break the hook over a failed notification */ }

static class Identity
{
    public const string Aumid = "Anthropic.ClaudeCode";
    public const string DisplayName = "Claude Code";

    // The Claude icon (32x32 PNG), base64-encoded so this stays a single file.
    const string IconBase64 =
        "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";

    // Idempotent: registers HKCU\Software\Classes\AppUserModelId\<Aumid> with the
    // display name + icon so toasts are attributed to "Claude Code". The icon is
    // written next to the exe (a stable, machine-local path) from the inlined PNG.
    public static void Ensure()
    {
        string iconPath = Path.Combine(AppContext.BaseDirectory, "claude-icon.png");
        string subkey = $@"Software\Classes\AppUserModelId\{Aumid}";

        // Fast path: already registered with our values and the icon present -> nothing to do.
        using (var existing = Registry.CurrentUser.OpenSubKey(subkey))
        {
            if (existing != null
                && (existing.GetValue("DisplayName") as string) == DisplayName
                && (existing.GetValue("IconUri") as string) == iconPath
                && File.Exists(iconPath))
                return;
        }

        // First run (or incomplete): write the inlined icon next to the exe...
        if (!File.Exists(iconPath))
            File.WriteAllBytes(iconPath, Convert.FromBase64String(IconBase64));

        // ...and register the AppUserModelID.
        using var key = Registry.CurrentUser.CreateSubKey(subkey);
        key.SetValue("DisplayName", DisplayName);
        if (File.Exists(iconPath)) key.SetValue("IconUri", iconPath);
    }
}

static class Native
{
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    public static extern void SetCurrentProcessExplicitAppUserModelID(
        [MarshalAs(UnmanagedType.LPWStr)] string appId);
}
