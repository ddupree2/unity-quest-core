#nullable enable
using StarStoneStudio.Scriptables;
using UnityEngine;

namespace DynamicBox.Quest.Core.Events
{
    /// <summary>
    /// Event channel asset for gameplay flag changes (<see cref="FlagChangedEvent"/>).
    /// Conditions listen to the asset assigned on their condition asset; game code raises the same asset.
    /// </summary>
    [CreateAssetMenu(menuName = "DynamicBox/Quest/Events/Flag Changed Event", fileName = "FlagChangedScriptableEvent")]
    public sealed class FlagChangedScriptableEvent : ScriptableEvent<FlagChangedEvent>
    {
    }
}
