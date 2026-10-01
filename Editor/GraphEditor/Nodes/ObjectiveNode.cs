using DynamicBox.Quest.Core;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace DynamicBox.Quest.Editor.GraphEditor
{
    /// <summary>
    /// Node representing an ObjectiveAsset in the graph.
    /// Can connect to quest nodes and conditions.
    /// Prerequisites are shown as incoming connections to the input port.
    /// </summary>
    public class ObjectiveNode : BaseQuestNode
    {
        public ObjectiveAsset Asset { get; set; }
        public Port InputPort => _inputPort;
        public Port CompletionPort { get; private set; }
        public Port FailurePort { get; private set; }

        public ObjectiveNode() : this(null)
        {
        }

        public ObjectiveNode(ObjectiveAsset asset)
        {
            Asset = asset;
            
            title = "🎯 OBJECTIVE";
            AddToClassList("objective-node");

            // Create input port (from quest - prerequisites are data-driven, not port-based)
            _inputPort = CreateInputPort("Input", Port.Capacity.Multi);
            inputContainer.Add(_inputPort);

            // Create output ports
            CompletionPort = CreateOutputPort("✓ Completion", Port.Capacity.Single);
            CompletionPort.AddToClassList("completion-port");
            outputContainer.Add(CompletionPort);

            FailurePort = CreateOutputPort("✗ Failure", Port.Capacity.Single);
            FailurePort.AddToClassList("failure-port");
            outputContainer.Add(FailurePort);

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
                // Objective ID
                var idLabel = CreateLabel($"ID: {Asset.ObjectiveId}", "node-id-label");
                Body.Add(idLabel);

                // Title/Display Name
                var titleLabel = CreateLabel(Asset.DisplayName, "node-name-label");
                Body.Add(titleLabel);

                // Description (truncated)
                if (!string.IsNullOrEmpty(Asset.Description))
                {
                    var desc = Asset.Description.Length > 40 
                        ? Asset.Description.Substring(0, 40) + "..." 
                        : Asset.Description;
                    var descLabel = CreateLabel(desc, "node-description-label");
                    Body.Add(descLabel);
                }

                // Settings/Badges
                var settingsContainer = new VisualElement();
                settingsContainer.AddToClassList("node-settings");

                if (Asset.IsOptional)
                {
                    var optionalBadge = CreateBadge("Optional", new UnityEngine.Color(0.4f, 0.6f, 0.8f));
                    settingsContainer.Add(optionalBadge);
                }

                var prereqCount = Asset.Prerequisites?.Count ?? 0;
                if (prereqCount > 0)
                {
                    var prereqLabel = CreateLabel($"📋 {prereqCount} Prerequisite(s)", "node-stats-label");
                    settingsContainer.Add(prereqLabel);
                    
                    // Show prerequisite objective IDs
                    foreach (var prereq in Asset.Prerequisites)
                    {
                        if (prereq != null)
                        {
                            var prereqIdLabel = CreateLabel($"  ↳ {prereq.ObjectiveId}", "node-prereq-label");
                            settingsContainer.Add(prereqIdLabel);
                        }
                    }
                }

                Body.Add(settingsContainer);
            }
            else
            {
                var placeholderLabel = CreateLabel("New Objective (Not Saved)", "node-placeholder");
                Body.Add(placeholderLabel);
            }
        }

        public override UnityEngine.Object GetAsset()
        {
            return Asset;
        }
    }
}
