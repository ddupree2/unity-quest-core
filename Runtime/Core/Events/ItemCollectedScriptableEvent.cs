#nullable enable
using StarStoneStudio.Scriptables;
using UnityEngine;

namespace DynamicBox.Quest.Core.Events
{
    /// <summary>
    /// Event channel asset for item pickups (<see cref="ItemCollectedEvent"/>).
    /// Conditions listen to the asset assigned on their condition asset; game code raises the same asset.
    /// </summary>
    [CreateAssetMenu(menuName = "DynamicBox/Quest/Events/Item Collected Event", fileName = "ItemCollectedScriptableEvent")]
    public sealed class ItemCollectedScriptableEvent : ScriptableEvent<ItemCollectedEvent>
    {
    }
}
