using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using DynamicBox.Quest.Core;

namespace DynamicBox.Quest.Editor.GraphEditor
{
    /// <summary>
    /// Inspector panel for editing node properties.
    /// Displays on the right side of the graph editor.
    /// Draws the selected asset's own inspector (custom editor or Unity's default), so every field
    /// of every quest, objective and condition type is editable here with no per-type code.
    /// </summary>
    public class NodeInspectorView : VisualElement
    {
        private const float LabelWidth = 120f;

        private readonly Label _titleLabel;
        private readonly ScrollView _contentContainer;
        private readonly QuestGraphEditorWindow _editorWindow;
        private BaseQuestNode _currentNode;
        private UnityEditor.Editor _assetEditor;
        private string _connectionSignature;

        public NodeInspectorView(QuestGraphEditorWindow editorWindow)
        {
            _editorWindow = editorWindow;

            // Setup inspector styling
            style.width = 320;
            style.backgroundColor = new Color(0.22f, 0.22f, 0.22f, 1f);
            style.borderLeftWidth = 1;
            style.borderLeftColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            style.paddingTop = 10;
            style.paddingBottom = 10;
            style.paddingLeft = 10;
            style.paddingRight = 10;

            // Title
            _titleLabel = new Label("Inspector");
            _titleLabel.style.fontSize = 16;
            _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _titleLabel.style.marginBottom = 10;
            _titleLabel.style.color = Color.white;
            Add(_titleLabel);

            // Content container (scrolls: full inspectors can be long)
            _contentContainer = new ScrollView(ScrollViewMode.Vertical);
            _contentContainer.style.flexGrow = 1;
            Add(_contentContainer);

            RegisterCallback<DetachFromPanelEvent>(_ => DestroyAssetEditor());

            ShowNoSelection();
        }

        /// <summary>
        /// Updates the inspector to show properties for the selected node.
        /// </summary>
        public void UpdateSelection(BaseQuestNode node)
        {
            _currentNode = node;
            _contentContainer.Clear();
            DestroyAssetEditor();

            if (node == null)
            {
                ShowNoSelection();
                return;
            }

            switch (node)
            {
                case QuestNode questNode:
                    _titleLabel.text = "Quest Properties";
                    ShowAssetInspector(questNode.Asset, "Quest");
                    break;
                case ObjectiveNode objectiveNode:
                    _titleLabel.text = "Objective Properties";
                    ShowAssetInspector(objectiveNode.Asset, "Objective");
                    break;
                case BaseConditionNode conditionNode:
                    _titleLabel.text = $"{ConditionTypeCatalog.GetDisplayName(conditionNode.ConditionType)} Condition";
                    ShowAssetInspector(conditionNode.Asset, "Condition");
                    break;
                default:
                    ShowNoSelection();
                    break;
            }
        }

        private void ShowNoSelection()
        {
            _titleLabel.text = "Inspector";
            _contentContainer.Clear();

            var helpLabel = new Label("Select a node to edit its properties");
            helpLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            helpLabel.style.marginTop = 20;
            helpLabel.style.whiteSpace = WhiteSpace.Normal;
            _contentContainer.Add(helpLabel);
        }

        #region Asset inspector

        private void ShowAssetInspector(ScriptableObject asset, string assetType)
        {
            if (asset == null)
            {
                ShowCreateAssetPrompt(assetType);
                return;
            }

            AddAssetNameField(asset);

            _assetEditor = UnityEditor.Editor.CreateEditor(asset);
            _connectionSignature = GetConnectionSignature(asset);
            _contentContainer.Add(new IMGUIContainer(DrawAssetInspector));

            AddAssetReferenceButton(asset);
        }

        /// <summary>
        /// Renames the asset file (and so the asset) when the field is committed with Enter or by
        /// leaving it, so graph-created assets don't keep names like "NewCondition".
        /// </summary>
        private void AddAssetNameField(ScriptableObject asset)
        {
            var nameField = new TextField("Asset Name")
            {
                value = asset.name,
                // Only commit on Enter / focus loss: renaming on every keystroke would rename the file each time
                isDelayed = true
            };
            nameField.labelElement.style.minWidth = 90;
            nameField.style.marginBottom = 8;

            nameField.RegisterValueChangedCallback(evt =>
            {
                string newName = evt.newValue?.Trim();
                if (string.IsNullOrEmpty(newName) || newName == asset.name)
                {
                    nameField.SetValueWithoutNotify(asset.name);
                    return;
                }

                string error = AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(asset), newName);
                if (!string.IsNullOrEmpty(error))
                {
                    EditorUtility.DisplayDialog("Rename Failed", error, "OK");
                    nameField.SetValueWithoutNotify(asset.name);
                    return;
                }

                _currentNode?.RefreshNode();
            });

            _contentContainer.Add(nameField);
        }

        private void DrawAssetInspector()
        {
            if (_assetEditor == null || _assetEditor.target == null)
                return;

            Object target = _assetEditor.target;

            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = LabelWidth;

            EditorGUI.BeginChangeCheck();
            _assetEditor.OnInspectorGUI();
            bool changed = EditorGUI.EndChangeCheck();

            EditorGUIUtility.labelWidth = previousLabelWidth;

            // Compared against the last draw rather than within this one: menu callbacks (e.g. the
            // objective inspector's "Create New Condition") change the asset between draws
            string connections = GetConnectionSignature(target);
            if (connections != _connectionSignature)
            {
                _connectionSignature = connections;

                // Objectives, prerequisites or conditions changed: the edges are stale, so rebuild.
                // Deferred because the rebuild replaces this panel mid-draw.
                EditorApplication.delayCall += () => _editorWindow.ReloadGraph(target);
            }
            else if (changed)
            {
                _currentNode?.RefreshNode();
            }
        }

        /// <summary>
        /// The asset references the graph draws as connections. A change means the graph must be rebuilt
        /// rather than just the node's text refreshed.
        /// </summary>
        private static string GetConnectionSignature(Object asset)
        {
            switch (asset)
            {
                case QuestAsset quest:
                    return string.Join(",", (quest.Objectives ?? Enumerable.Empty<ObjectiveAsset>()).Select(InstanceId));
                case ObjectiveAsset objective:
                    return string.Join(",", (objective.Prerequisites ?? Enumerable.Empty<ObjectiveAsset>()).Select(InstanceId))
                        + "|" + InstanceId(objective.CompletionCondition)
                        + "|" + InstanceId(objective.FailCondition);
                default:
                    return string.Empty;
            }
        }

        private static string InstanceId(Object asset)
        {
            return asset != null ? asset.GetInstanceID().ToString() : "-";
        }

        private void DestroyAssetEditor()
        {
            if (_assetEditor != null)
            {
                Object.DestroyImmediate(_assetEditor);
                _assetEditor = null;
            }
        }

        #endregion

        #region Creating assets for new nodes

        private void ShowCreateAssetPrompt(string assetType)
        {
            var helpLabel = new Label($"This {assetType.ToLower()} node is not yet saved as an asset.");
            helpLabel.style.color = new Color(0.9f, 0.7f, 0.3f, 1f);
            helpLabel.style.marginTop = 10;
            helpLabel.style.whiteSpace = WhiteSpace.Normal;
            _contentContainer.Add(helpLabel);

            var createButton = new Button(CreateAssetFromNode)
            {
                text = $"Create {assetType} Asset"
            };
            createButton.style.marginTop = 10;
            _contentContainer.Add(createButton);
        }

        private void CreateAssetFromNode()
        {
            if (_currentNode == null)
                return;

            string assetType;
            string defaultName;
            ScriptableObject newAsset;

            if (_currentNode is QuestNode)
            {
                assetType = "Quest";
                defaultName = "NewQuest";
                var quest = ScriptableObject.CreateInstance<QuestAsset>();

                // Initialize with reflection
                var displayNameField = typeof(QuestAsset).GetField("displayName",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var descriptionField = typeof(QuestAsset).GetField("description",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var objectivesField = typeof(QuestAsset).GetField("objectives",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                displayNameField?.SetValue(quest, "New Quest");
                descriptionField?.SetValue(quest, "Quest description here");
                objectivesField?.SetValue(quest, new System.Collections.Generic.List<ObjectiveAsset>());

                newAsset = quest;
            }
            else if (_currentNode is ObjectiveNode)
            {
                assetType = "Objective";
                defaultName = "NewObjective";
                var objective = ScriptableObject.CreateInstance<ObjectiveAsset>();

                // Initialize with reflection
                var titleField = typeof(ObjectiveAsset).GetField("title",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var descriptionField = typeof(ObjectiveAsset).GetField("description",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var isOptionalField = typeof(ObjectiveAsset).GetField("isOptional",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var prerequisitesField = typeof(ObjectiveAsset).GetField("prerequisites",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                titleField?.SetValue(objective, "New Objective");
                descriptionField?.SetValue(objective, "Objective description here");
                isOptionalField?.SetValue(objective, false);
                prerequisitesField?.SetValue(objective, new System.Collections.Generic.List<ObjectiveAsset>());

                newAsset = objective;
            }
            else if (_currentNode is BaseConditionNode conditionNode)
            {
                if (conditionNode.ConditionType == null || conditionNode.ConditionType.IsAbstract)
                {
                    EditorUtility.DisplayDialog("Unknown Type", "Unknown condition type. Cannot create asset.", "OK");
                    return;
                }

                // Any condition type: its own field initializers provide the defaults
                assetType = ConditionTypeCatalog.GetDisplayName(conditionNode.ConditionType);
                defaultName = $"New{conditionNode.ConditionType.Name.Replace("Asset", string.Empty)}";
                newAsset = ScriptableObject.CreateInstance(conditionNode.ConditionType);
            }
            else
            {
                return;
            }

            // Save next to the open quest by default
            string folder = _editorWindow.GetQuestAssetPath().Replace('\\', '/');
            string path = EditorUtility.SaveFilePanelInProject(
                $"Save {assetType} Asset",
                defaultName,
                "asset",
                $"Choose a location to save the {assetType} asset",
                folder);

            if (string.IsNullOrEmpty(path))
            {
                Object.DestroyImmediate(newAsset);
                return;
            }

            // Readable ID from the chosen file name ("TalkToGrandpa" → "talk_to_grandpa"); Yarn uses it
            SetIdFromFileName(newAsset, path);

            AssetDatabase.CreateAsset(newAsset, path);
            AssetDatabase.SaveAssets();

            // Update the node to reference the new asset
            switch (_currentNode)
            {
                case QuestNode qNode:
                    qNode.Asset = (QuestAsset)newAsset;
                    break;
                case ObjectiveNode oNode:
                    oNode.Asset = (ObjectiveAsset)newAsset;
                    break;
                case BaseConditionNode cNode:
                    cNode.Asset = (ConditionAsset)newAsset;
                    break;
            }

            _currentNode.RefreshNode();

            // Show the new asset's fields
            UpdateSelection(_currentNode);
        }

        #endregion

        private static void SetIdFromFileName(ScriptableObject asset, string path)
        {
            string fieldName = asset is QuestAsset ? "questId" : asset is ObjectiveAsset ? "objectiveId" : null;
            if (fieldName == null)
                return;

            var idField = asset.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            idField?.SetValue(asset, QuestIdUtility.FromName(System.IO.Path.GetFileNameWithoutExtension(path)));
        }

        private void AddAssetReferenceButton(ScriptableObject asset)
        {
            var button = new Button(() =>
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            })
            {
                text = "Select in Project"
            };
            button.style.marginTop = 15;
            _contentContainer.Add(button);
        }
    }
}
