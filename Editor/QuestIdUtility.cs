using System.Text;

namespace DynamicBox.Quest.Editor
{
    /// <summary>
    /// Builds readable default QuestIds / ObjectiveIds from asset names ("TalkToGrandpa" → "talk_to_grandpa").
    /// IDs are typed in Yarn scripts and stored in saves, so they should be short and stable rather
    /// than random GUIDs. Shared by the inspectors and the graph editor.
    /// </summary>
    public static class QuestIdUtility
    {
        public static string FromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(name.Length + 8);
            char previous = '\0';

            foreach (char c in name.Trim())
            {
                if (char.IsLetterOrDigit(c))
                {
                    // Word boundary at lower/digit → upper ("talkTo" → "talk_to")
                    if (char.IsUpper(c) && (char.IsLower(previous) || char.IsDigit(previous)))
                    {
                        builder.Append('_');
                    }

                    builder.Append(char.ToLowerInvariant(c));
                }
                else if (builder.Length > 0 && builder[builder.Length - 1] != '_')
                {
                    builder.Append('_');
                }

                previous = c;
            }

            return builder.ToString().Trim('_');
        }
    }
}
