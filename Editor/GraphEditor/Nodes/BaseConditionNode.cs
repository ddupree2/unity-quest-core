using System;
using DynamicBox.Quest.Core;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace DynamicBox.Quest.Editor.GraphEditor
{
    /// <summary>
    /// Base class for all condition nodes in the graph.
    /// Condition nodes represent the logic for completing or failing objectives.
    /// </summary>
    public abstract class BaseConditionNode : BaseQuestNode
    {
        /// <summary>The condition asset; null until a new node's asset is created.</summary>
        public ConditionAsset Asset { get; set; }

        /// <summary>The condition type, known before the asset exists (used to create it).</summary>
        public Type ConditionType { get; }

        public Port InputPort => _inputPort;

        protected BaseConditionNode(ConditionAsset asset, Type conditionType)
        {
            Asset = asset;
            ConditionType = asset != null ? asset.GetType() : conditionType;
            AddToClassList("condition-node");

            // All condition nodes have an input port
            _inputPort = CreateInputPort("Input", Port.Capacity.Single);
            inputContainer.Add(_inputPort);
        }

        /// <summary>
        /// Creates a property display for condition-specific data.
        /// </summary>
        protected VisualElement CreatePropertyDisplay(string label, string value)
        {
            var container = new VisualElement();
            container.AddToClassList("property-container");

            var propertyLabel = new Label($"{label}: {value}");
            propertyLabel.AddToClassList("property-label");
            container.Add(propertyLabel);

            return container;
        }

        public override UnityEngine.Object GetAsset()
        {
            return Asset;
        }
    }
}
