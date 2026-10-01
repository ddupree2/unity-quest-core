using DynamicBox.Quest.Core;
using DynamicBox.Quest.Core.Conditions;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DynamicBox.Quest.Editor
{
    [CustomEditor(typeof(ObjectiveAsset))]
    public class ObjectiveAssetEditor : UnityEditor.Editor
    {
        private SerializedProperty _objectiveIdProp;
        private SerializedProperty _titleProp;
        private SerializedProperty _descriptionProp;
        private SerializedProperty _isOptionalProp;
        private SerializedProperty _prerequisitesProp;
        private SerializedProperty _completionConditionProp;
        private SerializedProperty _failConditionProp;

        #region Unity Methods

        void OnEnable()
        {
            _objectiveIdProp = serializedObject.FindProperty("objectiveId");
            _titleProp = serializedObject.FindProperty("title");
            _descriptionProp = serializedObject.FindProperty("description");
            _isOptionalProp = serializedObject.FindProperty("isOptional");
            _prerequisitesProp = serializedObject.FindProperty("prerequisites");
            _completionConditionProp = serializedObject.FindProperty("completionCondition");
            _failConditionProp = serializedObject.FindProperty("failCondition");
            
            // Validate that all properties were found
            if (_objectiveIdProp == null || _titleProp == null || _descriptionProp == null || 
                _isOptionalProp == null || _prerequisitesProp == null || 
                _completionConditionProp == null || _failConditionProp == null)
            {
                Debug.LogError($"ObjectiveAssetEditor: Could not find all required properties on ObjectiveAsset.");
            }
        }

        #endregion

        public override void OnInspectorGUI()
        {
            // Safety check - if properties are null, fall back to default inspector
            if (_objectiveIdProp == null || _titleProp == null || _descriptionProp == null || 
                _isOptionalProp == null || _prerequisitesProp == null || 
                _completionConditionProp == null || _failConditionProp == null)
            {
                EditorGUILayout.HelpBox("Custom inspector has issues. Using default inspector.", MessageType.Warning);
                base.OnInspectorGUI();
                return;
            }
            
            serializedObject.Update();

            EditorGUILayout.LabelField("Objective Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Basic Info
            EditorGUILayout.LabelField("Basic Information", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(_objectiveIdProp, new GUIContent("Objective ID",
                "Name used in Yarn (objective_active(\"questId\", \"id\")) and in saves; unique within its quest. Changing it after release breaks existing saves."));
            DrawIdFromNameButton(_objectiveIdProp);
            EditorGUILayout.PropertyField(_titleProp, new GUIContent("Title"));
            
            EditorGUILayout.LabelField("Description");
            _descriptionProp.stringValue = EditorGUILayout.TextArea(_descriptionProp.stringValue, GUILayout.Height(40));
            
            EditorGUILayout.Space();

            // Configuration
            EditorGUILayout.LabelField("Configuration", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(_isOptionalProp, new GUIContent("Is Optional", "Optional objectives don't block quest completion"));
            
            EditorGUILayout.Space();

            // Prerequisites
            EditorGUILayout.LabelField("Prerequisites", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(_prerequisitesProp, new GUIContent("Required Objectives"), true);
            
            EditorGUILayout.Space();

            // Completion Conditions
            EditorGUILayout.LabelField("Completion Conditions", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(_completionConditionProp, new GUIContent("Completion Condition"));

            if (GUILayout.Button("Create New Condition"))
            {
                ShowConditionCreationMenu(false);
            }

            EditorGUILayout.Space();
            
            // Fail Conditions
            EditorGUILayout.LabelField("Failure Conditions (Optional)", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(_failConditionProp, new GUIContent("Fail Condition"));
            
            if (GUILayout.Button("Create New Fail Condition"))
            {
                ShowConditionCreationMenu(true);
            }

            EditorGUILayout.Space();

            // Validation
            ValidateObjective();

            serializedObject.ApplyModifiedProperties();
        }

        // Offers a readable ID from the asset name when the ID is empty
        private void DrawIdFromNameButton(SerializedProperty idProperty)
        {
            if (!string.IsNullOrWhiteSpace(idProperty.stringValue))
            {
                return;
            }

            string suggestion = QuestIdUtility.FromName(target.name);
            if (!string.IsNullOrEmpty(suggestion) && GUILayout.Button($"Use \"{suggestion}\" (from asset name)"))
            {
                idProperty.stringValue = suggestion;
            }
        }

        private void ShowConditionCreationMenu(bool isFailCondition)
        {
            // Every creatable condition type, so new condition classes appear without editor code
            var menu = new GenericMenu();
            foreach (var conditionType in ConditionTypeCatalog.CreatableTypes)
            {
                var type = conditionType;
                menu.AddItem(new GUIContent(ConditionTypeCatalog.GetDisplayName(type)), false, () => CreateCondition(type, isFailCondition));
            }
            menu.ShowAsContext();
        }

        private void CreateCondition(System.Type conditionType, bool isFailCondition)
        {
            string assetPath = AssetDatabase.GetAssetPath(target);
            string directory = System.IO.Path.GetDirectoryName(assetPath);
            // Named after the objective so the file is recognisable in the Project window
            string conditionName = $"{target.name}_{(isFailCondition ? "Fail" : "Completion")}";
            string conditionPath = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{conditionName}.asset");
            
            var condition = (ConditionAsset)CreateInstance(conditionType);
            condition.name = conditionName;
            AssetDatabase.CreateAsset(condition, conditionPath);
            AssetDatabase.SaveAssets();
            
            // Assign to objective
            if (isFailCondition)
            {
                _failConditionProp.objectReferenceValue = condition;
            }
            else
            {
                _completionConditionProp.objectReferenceValue = condition;
            }
            serializedObject.ApplyModifiedProperties();
            
            EditorGUIUtility.PingObject(condition);
        }

        private void ValidateObjective()
        {
            var objective = target as ObjectiveAsset;
            if (objective == null) return;

            if (string.IsNullOrEmpty(objective.ObjectiveId))
            {
                EditorGUILayout.HelpBox("Objective ID is required and should be unique.", MessageType.Error);
            }

            if (string.IsNullOrEmpty(objective.Title))
            {
                EditorGUILayout.HelpBox("Title is required for UI purposes.", MessageType.Warning);
            }

            if (objective.CompletionCondition == null)
            {
                EditorGUILayout.HelpBox("Completion Condition is required to define completion criteria.", MessageType.Error);
            }
        }
    }
}
