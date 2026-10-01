using System;
using System.Collections.Generic;
using System.Linq;
using DynamicBox.Quest.Core;
using UnityEditor;
using UnityEngine;

namespace DynamicBox.Quest.Editor.Windows
{
    /// <summary>
    /// Play-mode window for inspecting and driving quests: active quests with condition progress,
    /// completed/failed history, starting any quest asset, and the flag service's flags and counters.
    /// </summary>
    public class QuestDebuggerWindow : EditorWindow
    {
        private enum Tab { Active, History, Flags }

        private static readonly string[] TabNames = { "Active", "History", "Flags" };

        private Vector2 _scrollPosition;
        private QuestManager _questManager;
        private bool _autoRefresh = true;
        private float _lastRefreshTime;
        private Tab _tab;

        private QuestAsset[] _allQuests = Array.Empty<QuestAsset>();
        private string[] _allQuestNames = Array.Empty<string>();
        private int _questToStart;

        private string _newFlagId = string.Empty;
        private string _newCounterId = string.Empty;

        // Button actions run after drawing, so changing quests never disturbs the GUI loop or the
        // lists being drawn
        private Action _pendingAction;

        #region Unity Methods

        void OnEnable()
        {
            FindQuestManager();
            RefreshQuestAssets();
        }

        void OnGUI()
        {
            DrawToolbar();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("The Quest Debugger shows live quest state. Enter Play mode to use it.", MessageType.Info);
                return;
            }

            if (_questManager == null)
            {
                FindQuestManager();
            }

            if (_questManager == null)
            {
                DrawNoQuestManagerWarning();
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            switch (_tab)
            {
                case Tab.Active:
                    DrawStartQuest();
                    DrawQuestList(_questManager.ActiveQuests, "No active quests.");
                    break;
                case Tab.History:
                    DrawQuestList(_questManager.CompletedQuests.Concat(_questManager.FailedQuests).ToList(), "No completed or failed quests.");
                    break;
                case Tab.Flags:
                    DrawFlags();
                    break;
            }
            EditorGUILayout.EndScrollView();

            if (_pendingAction != null)
            {
                Action action = _pendingAction;
                _pendingAction = null;
                action();
                Repaint();
            }
        }

        private void Update()
        {
            if (_autoRefresh && Time.realtimeSinceStartup - _lastRefreshTime > 0.5f)
            {
                _lastRefreshTime = Time.realtimeSinceStartup;
                Repaint();
            }
        }

        #endregion

        [MenuItem("Tools/Star Stone Studio/Quests/Debugger", false, 100)]
        public static void ShowWindow()
        {
            GetWindow<QuestDebuggerWindow>("Quest Debugger");
        }

        #region Toolbar

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            _tab = (Tab)GUILayout.Toolbar((int)_tab, TabNames, EditorStyles.toolbarButton, GUILayout.Width(240));

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                FindQuestManager();
                RefreshQuestAssets();
            }

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(_questManager == null || !Application.isPlaying))
            {
                if (GUILayout.Button("Clear All", EditorStyles.toolbarButton) &&
                    EditorUtility.DisplayDialog("Clear All Quests", "Stop every active quest and forget all quest history?", "Clear", "Cancel"))
                {
                    _pendingAction = () => _questManager.ClearAll();
                }
            }

            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawNoQuestManagerWarning()
        {
            EditorGUILayout.HelpBox(
                "No QuestManager found in the loaded scenes. Make sure a QuestManager component is active.",
                MessageType.Warning);

            if (GUILayout.Button("Find QuestManager"))
            {
                FindQuestManager();
            }
        }

        #endregion

        #region Quests

        private void DrawStartQuest()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Start Quest", EditorStyles.boldLabel);

            if (_allQuests.Length == 0)
            {
                EditorGUILayout.LabelField("No QuestAssets found in the project.", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                _questToStart = Mathf.Clamp(EditorGUILayout.Popup(_questToStart, _allQuestNames), 0, _allQuests.Length - 1);
                QuestAsset quest = _allQuests[_questToStart];
                if (GUILayout.Button(_questManager.IsActive(quest) ? "Restart" : "Start", GUILayout.Width(80)) && quest != null)
                {
                    _pendingAction = () => ForceStart(quest);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField("Forced start: ignores whether the quest is active or completed.", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawQuestList(IReadOnlyList<QuestState> quests, string emptyMessage)
        {
            if (quests == null || quests.Count == 0)
            {
                EditorGUILayout.HelpBox(emptyMessage, MessageType.Info);
                return;
            }

            // Copy: buttons may end or restart quests, which changes the manager's lists
            foreach (QuestState questState in quests.ToList())
            {
                DrawQuest(questState);
            }
        }

        private void DrawQuest(QuestState questState)
        {
            var quest = questState.Definition;
            var originalColor = GUI.color;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Quest header
            EditorGUILayout.BeginHorizontal();
            GUI.color = GetStatusColor(questState.Status);
            EditorGUILayout.LabelField($"● {quest.DisplayName}", EditorStyles.boldLabel);
            GUI.color = originalColor;
            EditorGUILayout.LabelField($"[{questState.Status}]", GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"ID: {quest.QuestId}", EditorStyles.miniLabel);
            if (!string.IsNullOrEmpty(quest.Description))
            {
                EditorGUILayout.LabelField($"Description: {quest.Description}", EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.Space();

            // Objectives
            if (quest.Objectives != null && quest.Objectives.Count > 0)
            {
                EditorGUILayout.LabelField("Objectives:", EditorStyles.miniLabel);
                EditorGUI.indentLevel++;

                foreach (var objective in quest.Objectives)
                {
                    if (objective == null) continue;

                    questState.TryGetObjective(objective.ObjectiveId, out var objState);
                    var status = objState?.Status ?? ObjectiveStatus.NotStarted;

                    string progress = objState?.CompletionProgress?.ProgressDescription;
                    string progressText = string.IsNullOrEmpty(progress) ? string.Empty : $" ({progress})";

                    GUI.color = GetObjectiveColor(status);
                    EditorGUILayout.LabelField($"{GetObjectiveSymbol(status)} {objective.DisplayName}{progressText} [{status}]");
                    GUI.color = originalColor;

                    if (objState != null && status == ObjectiveStatus.InProgress)
                    {
                        EditorGUI.indentLevel++;
                        DrawObjectiveDetails(objective, objState);
                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Debug actions
            EditorGUILayout.BeginHorizontal();

            if (questState.Status == QuestStatus.InProgress)
            {
                if (GUILayout.Button("Complete", GUILayout.Width(80)))
                {
                    _pendingAction = () => _questManager.CompleteQuest(questState);
                }

                if (GUILayout.Button("Fail", GUILayout.Width(80)))
                {
                    _pendingAction = () => _questManager.FailQuest(questState);
                }

                if (GUILayout.Button("Stop", GUILayout.Width(80)))
                {
                    _pendingAction = () => _questManager.StopQuest(questState);
                }
            }

            if (GUILayout.Button("Restart", GUILayout.Width(80)))
            {
                _pendingAction = () => ForceStart(quest);
            }

            if (GUILayout.Button("Ping Asset", GUILayout.Width(80)))
            {
                EditorGUIUtility.PingObject(quest);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawObjectiveDetails(ObjectiveAsset objective, ObjectiveState objState)
        {
            if (!string.IsNullOrEmpty(objective.ObjectiveId))
            {
                EditorGUILayout.LabelField($"ID: {objective.ObjectiveId}", EditorStyles.miniLabel);
            }

            if (!string.IsNullOrEmpty(objective.Description))
            {
                EditorGUILayout.LabelField($"Description: {objective.Description}", EditorStyles.wordWrappedMiniLabel);
            }

            if (objective.CompletionCondition != null)
            {
                var isMet = objState.CompletionInstance?.IsMet ?? false;
                GUI.color = isMet ? Color.green : Color.white;
                EditorGUILayout.LabelField($"Completion: {objective.CompletionCondition.name} ({objective.CompletionCondition.GetType().Name}) {(isMet ? "✓" : "○")}", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            else
            {
                // An objective without a condition (or whose condition asset failed to load) never completes
                GUI.color = new Color(1f, 0.6f, 0.2f);
                EditorGUILayout.LabelField("Completion: NONE (this objective can never complete)", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            if (objective.FailCondition != null)
            {
                var isMet = objState.FailInstance?.IsMet ?? false;
                GUI.color = isMet ? Color.red : Color.white;
                EditorGUILayout.LabelField($"Fail: {objective.FailCondition.name} {(isMet ? "✗" : "○")}", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            if (objective.Prerequisites != null && objective.Prerequisites.Count > 0)
            {
                EditorGUILayout.LabelField($"Prerequisites: {string.Join(", ", objective.Prerequisites.Where(p => p != null).Select(p => p.DisplayName))}", EditorStyles.miniLabel);
            }

            if (objective.IsOptional)
            {
                EditorGUILayout.LabelField("(Optional)", EditorStyles.miniLabel);
            }
        }

        private void ForceStart(QuestAsset quest)
        {
            var active = _questManager.ActiveQuests.FirstOrDefault(q => q.Definition == quest);
            if (active != null)
            {
                _questManager.StopQuest(active);
            }

            _questManager.StartQuest(quest);
        }

        #endregion

        #region Flags

        private void DrawFlags()
        {
            IQuestFlagService flagService = _questManager.Context?.FlagService;
            if (flagService == null)
            {
                EditorGUILayout.HelpBox("The QuestManager has no flag service (add one next to its QuestPlayerRef).", MessageType.Warning);
                return;
            }

            if (!(flagService is IQuestFlagServiceInspectable inspectable))
            {
                EditorGUILayout.HelpBox($"{flagService.GetType().Name} doesn't implement IQuestFlagServiceInspectable, so its flags can't be listed. You can still set flags by name below.", MessageType.Info);
                DrawSetFlagRow(flagService);
                return;
            }

            EditorGUILayout.LabelField($"Flags ({flagService.GetType().Name})", EditorStyles.boldLabel);
            var flags = inspectable.GetAllFlags().OrderBy(f => f.Key).ToList();
            if (flags.Count == 0)
            {
                EditorGUILayout.LabelField("No flags set.", EditorStyles.miniLabel);
            }

            foreach (var flag in flags)
            {
                bool value = EditorGUILayout.Toggle(flag.Key, flag.Value);
                if (value != flag.Value)
                {
                    string id = flag.Key;
                    _pendingAction = () => flagService.SetFlag(id, value);
                }
            }

            DrawSetFlagRow(flagService);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Counters", EditorStyles.boldLabel);
            var counters = inspectable.GetAllCounters().OrderBy(c => c.Key).ToList();
            if (counters.Count == 0)
            {
                EditorGUILayout.LabelField("No counters set.", EditorStyles.miniLabel);
            }

            foreach (var counter in counters)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(counter.Key, counter.Value.ToString());
                string id = counter.Key;
                if (GUILayout.Button("-", GUILayout.Width(24)))
                {
                    _pendingAction = () => flagService.IncrementCounter(id, -1);
                }
                if (GUILayout.Button("+", GUILayout.Width(24)))
                {
                    _pendingAction = () => flagService.IncrementCounter(id, 1);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            _newCounterId = EditorGUILayout.TextField("New counter", _newCounterId);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_newCounterId)))
            {
                if (GUILayout.Button("+1", GUILayout.Width(40)))
                {
                    string id = _newCounterId.Trim();
                    _pendingAction = () => flagService.IncrementCounter(id, 1);
                    _newCounterId = string.Empty;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSetFlagRow(IQuestFlagService flagService)
        {
            EditorGUILayout.BeginHorizontal();
            _newFlagId = EditorGUILayout.TextField("Set flag", _newFlagId);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_newFlagId)))
            {
                if (GUILayout.Button("True", GUILayout.Width(50)))
                {
                    string id = _newFlagId.Trim();
                    _pendingAction = () => flagService.SetFlag(id, true);
                    _newFlagId = string.Empty;
                }

                if (GUILayout.Button("False", GUILayout.Width(50)))
                {
                    string id = _newFlagId.Trim();
                    _pendingAction = () => flagService.SetFlag(id, false);
                    _newFlagId = string.Empty;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Helpers

        private Color GetStatusColor(QuestStatus status)
        {
            switch (status)
            {
                case QuestStatus.NotStarted: return Color.gray;
                case QuestStatus.InProgress: return Color.yellow;
                case QuestStatus.Completed: return Color.green;
                case QuestStatus.Failed: return Color.red;
                default: return Color.white;
            }
        }

        private Color GetObjectiveColor(ObjectiveStatus status)
        {
            switch (status)
            {
                case ObjectiveStatus.NotStarted: return Color.gray;
                case ObjectiveStatus.InProgress: return Color.yellow;
                case ObjectiveStatus.Completed: return Color.green;
                case ObjectiveStatus.Failed: return Color.red;
                default: return Color.white;
            }
        }

        private string GetObjectiveSymbol(ObjectiveStatus status)
        {
            switch (status)
            {
                case ObjectiveStatus.NotStarted: return "○";
                case ObjectiveStatus.InProgress: return "●";
                case ObjectiveStatus.Completed: return "✓";
                case ObjectiveStatus.Failed: return "✗";
                default: return "?";
            }
        }

        private void FindQuestManager()
        {
            _questManager = Application.isPlaying ? FindFirstObjectByType<QuestManager>() : null;
        }

        private void RefreshQuestAssets()
        {
            _allQuests = AssetDatabase.FindAssets($"t:{nameof(QuestAsset)}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<QuestAsset>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(quest => quest != null)
                .OrderBy(quest => quest.DisplayName)
                .ToArray();
            _allQuestNames = _allQuests
                .Select(quest => $"{(string.IsNullOrEmpty(quest.DisplayName) ? quest.name : quest.DisplayName)} ({quest.QuestId})")
                .ToArray();
            _questToStart = Mathf.Clamp(_questToStart, 0, Mathf.Max(0, _allQuests.Length - 1));
        }

        #endregion
    }
}
