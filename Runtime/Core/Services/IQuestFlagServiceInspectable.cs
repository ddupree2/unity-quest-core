#nullable enable
using System.Collections.Generic;

namespace DynamicBox.Quest.Core
{
    /// <summary>
    /// Optional interface for flag services that can list their contents, used by debugging tools
    /// such as the Quest Debugger window. Quest evaluation never needs it.
    /// </summary>
    public interface IQuestFlagServiceInspectable
    {
        /// <summary>All flags that have been set, with their current values.</summary>
        IReadOnlyDictionary<string, bool> GetAllFlags();

        /// <summary>All counters that have been set, with their current values.</summary>
        IReadOnlyDictionary<string, int> GetAllCounters();
    }
}
