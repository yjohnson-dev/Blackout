using System;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.GamePad;
using Dalamud.Game.Command;
using Dalamud.Game.Config;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Blackout;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/blackout";
    private const long SaveDebounceMs = 500;

    [PluginService] private static IDalamudPluginInterface PluginInterface { get; set; } = null!;
    [PluginService] private static ICommandManager Commands { get; set; } = null!;
    [PluginService] private static IFramework Framework { get; set; } = null!;
    [PluginService] private static ICondition Condition { get; set; } = null!;
    [PluginService] private static IClientState ClientState { get; set; } = null!;
    [PluginService] private static IGamepadState Gamepad { get; set; } = null!;
    [PluginService] private static IChatGui Chat { get; set; } = null!;
    [PluginService] private static IPluginLog Log { get; set; } = null!;
    [PluginService] private static IAddonLifecycle AddonLifecycle { get; set; } = null!;
    [PluginService] private static IGameConfig GameConfig { get; set; } = null!;
    [PluginService] private static IDtrBar DtrBar { get; set; } = null!;

    private readonly WindowSystem windows = new("Blackout");
    private readonly ConfigWindow configWindow;
    private readonly Overlay overlay = new();
    private readonly StateStore state;
    private readonly AudioMuter audio;
    private readonly ServerInfoBar infoBar;
    private readonly WakeWatcher wake;

    private long? dirtySince;
    private BlackoutReason lastReason = BlackoutReason.None;

    public Plugin()
    {
        this.Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        this.Config.EnsureDefaults();

        this.state = new StateStore(PluginInterface, Log);
        this.wake = new WakeWatcher(AddonLifecycle, Chat);
        this.Controller = new BlackoutController(this.Config, Condition, ClientState, Gamepad, this.wake);
        this.audio = new AudioMuter(GameConfig, this.Config, this.state, Log);
        this.infoBar = new ServerInfoBar(DtrBar, Log);

        this.configWindow = new ConfigWindow(this);
        this.windows.AddWindow(this.configWindow);

        // The overlay and the window must draw even when the game hides its interface.
        var ui = PluginInterface.UiBuilder;
        ui.DisableAutomaticUiHide = true;
        ui.DisableUserUiHide = true;
        ui.DisableCutsceneUiHide = true;
        ui.DisableGposeUiHide = true;
        ui.Draw += this.OnDraw;
        ui.OpenConfigUi += this.OpenConfig;
        ui.OpenMainUi += this.OpenConfig;

        Framework.Update += this.OnUpdate;

        Commands.AddHandler(CommandName, new CommandInfo(this.OnCommand)
        {
            HelpMessage = "Open the Blackout settings. You can also use /blackout now, /blackout preview, /blackout on, or /blackout off.",
        });
    }

    public Configuration Config { get; private set; }

    internal BlackoutController Controller { get; }

    public void Dispose()
    {
        Commands.RemoveHandler(CommandName);
        Framework.Update -= this.OnUpdate;

        var ui = PluginInterface.UiBuilder;
        ui.Draw -= this.OnDraw;
        ui.OpenConfigUi -= this.OpenConfig;
        ui.OpenMainUi -= this.OpenConfig;

        this.wake.Dispose();
        this.audio.Dispose();
        this.infoBar.Dispose();
        this.windows.RemoveAllWindows();

        PluginInterface.SavePluginConfig(this.Config);
    }

    public void MarkDirty() => this.dirtySince ??= Clock.Now;

    private void OpenConfig() => this.configWindow.IsOpen = true;

    private void OnUpdate(IFramework framework)
    {
        this.Controller.Update();
        var status = this.Controller.Status;

        if (status.Reason != this.lastReason)
        {
            this.lastReason = status.Reason;
            Log.Debug($"State {status.Reason}. focused={status.Focused} idle={status.IdleMs}ms background={status.BackgroundForMs}ms contexts=[{string.Join(", ", status.ActiveContexts)}]");
        }

        var blackedOut = this.Controller.BlackedOut;
        this.audio.SetMuted(this.Config.MuteEnabled && blackedOut);
        this.infoBar.Update(this.Config, blackedOut, status.BlackoutInMs);

        if (this.dirtySince is { } since && Clock.Now - since >= SaveDebounceMs)
        {
            PluginInterface.SavePluginConfig(this.Config);
            this.dirtySince = null;
        }
    }

    private void OnDraw()
    {
        this.windows.Draw();
        this.overlay.Draw(this.Controller.Alpha, this.Config, this.Controller.Status);
    }

    private void OnCommand(string command, string args)
    {
        switch (args.Trim().ToLowerInvariant())
        {
            case "":
                this.configWindow.Toggle();
                return;
            case "now":
                this.Controller.ToggleManual();
                return;
            case "preview":
                this.Controller.StartPreview();
                return;
            case "on":
                this.Config.Enabled = true;
                break;
            case "off":
                this.Config.Enabled = false;
                break;
            default:
                Chat.Print(Strings.CommandUsage);
                return;
        }

        this.MarkDirty();
        Chat.Print(this.Config.Enabled ? Strings.EnabledMessage : Strings.DisabledMessage);
    }
}
