#nullable enable
using StarStoneStudio.Scriptables;
using UnityEngine;

namespace DynamicBox.Quest.Core.Events
{
    /// <summary>
    /// Event channel asset for the player entering an area (<see cref="AreaEnteredEvent"/>).
    /// Conditions listen to the asset assigned on their condition asset; game code raises the same asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Star Stone Studio/Quests/Legacy/Area Entered Event", fileName = "AreaEnteredScriptableEvent")]
    public sealed class AreaEnteredScriptableEvent : ScriptableEvent<AreaEnteredEvent>
    {
    }
}
