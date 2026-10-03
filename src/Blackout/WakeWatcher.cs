using System;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Chat;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;

namespace Blackout;

/// <summary>Watches for events that must bring the picture back.</summary>
public sealed class WakeWatcher : IDisposable
{
    private const string DutyReadyAddon = "ContentsFinderConfirm";

    private readonly IAddonLifecycle addonLifecycle;
    private readonly IChatGui chat;

    public WakeWatcher(IAddonLifecycle addonLifecycle, IChatGui chat)
    {
        this.addonLifecycle = addonLifecycle;
        this.chat = chat;

        addonLifecycle.RegisterListener(AddonEvent.PostShow, DutyReadyAddon, this.OnDutyShow);
        addonLifecycle.RegisterListener(AddonEvent.PostHide, DutyReadyAddon, this.OnDutyHide);
        chat.ChatMessage += this.OnChatMessage;
    }

    public bool DutyPopupVisible { get; private set; }

    public long LastTellTick { get; private set; } = long.MinValue;

    public void Dispose()
    {
        this.addonLifecycle.UnregisterListener(AddonEvent.PostShow, DutyReadyAddon, this.OnDutyShow);
        this.addonLifecycle.UnregisterListener(AddonEvent.PostHide, DutyReadyAddon, this.OnDutyHide);
        this.chat.ChatMessage -= this.OnChatMessage;
    }

    private void OnDutyShow(AddonEvent type, AddonArgs args) => this.DutyPopupVisible = true;

    private void OnDutyHide(AddonEvent type, AddonArgs args) => this.DutyPopupVisible = false;

    private void OnChatMessage(IHandleableChatMessage message)
    {
        if (message.LogKind is XivChatType.TellIncoming or XivChatType.GmTell)
            this.LastTellTick = Environment.TickCount64;
    }
}
