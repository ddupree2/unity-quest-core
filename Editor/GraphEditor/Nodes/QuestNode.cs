using DynamicBox.Quest.Core;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace DynamicBox.Quest.Editor.GraphEditor
{
    /// <summary>
    /// Node representing a QuestAsset in the graph.
    /// Top-level node that connects to objective nodes.
    /// </summary>
    public class QuestNode : BaseQuestNode
    {
        public QuestAsset Asset { get; set; }
        public Port OutputPort => _outputPort;

        public QuestNode() : this(null)
        {
        }

        public QuestNode(QuestAsset asset)
        {
            Asset = asset;
            
            title = "📜 QUEST";
            AddToClassList("quest-node");

            // Create output port for objectives
            _outputPort = CreateOutputPort("Objectives", Port.Capacity.Multi);
            outputContainer.Add(_outputPort);

            // Build node contents
            BuildNodeContent();
            
            RefreshExpandedState();
            RefreshPorts();
        }

        /// <summary>
        /// Refreshes the node display after property changes.
        /// </summary>
        public override void RefreshNode()
        {
            Body.Clear();
            BuildNodeContent();
            RefreshExpandedState();
        }

        private void BuildNodeContent()
        {
            if (Asset != null)
            {
                // Quest ID
                var idLabel = CreateLabel($"ID: {Asset.QuestId}", "node-id-label");
                Body.Add(idLabel);

                // Display Name
                var nameLabel = CreateLabel(Asset.DisplayName, "node-name-label");
                Body.Add(nameLabel);

                // Description (truncated)
                if (!string.IsNullOrEmpty(Asset.Description))
                {
                    var desc = Asset.Description.Length > 50 
                        ? Asset.Description.Substring(0, 50) + "..." 
                        : Asset.Description;
                    var descLabel = CreateLabel(desc, "node-description-label");
                    Body.Add(descLabel);
                }

                // Stats
                var statsContainer = new VisualElement();
                statsContainer.AddToClassList("node-stats");
                
                var objectiveCount = Asset.Objectives?.Count ?? 0;
                var statsLabel = CreateLabel($"⚙️ {objectiveCount} Objective(s)", "node-stats-label");
                statsContainer.Add(statsLabel);
                
                Body.Add(statsContainer);
            }
            else
            {
                var placeholderLabel = CreateLabel("New Quest (Not Saved)", "node-placeholder");
                Body.Add(placeholderLabel);
            }
        }

        public override UnityEngine.Object GetAsset()
        {
            return Asset;
        }
    }
}
