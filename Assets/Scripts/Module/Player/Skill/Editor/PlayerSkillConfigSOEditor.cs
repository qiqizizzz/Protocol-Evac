/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家技能配置 Inspector，提供动画时长同步按钮
 * │  类    名: PlayerSkillConfigSOEditor.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using Module.Player.Skill.Data;
using UnityEditor;
using UnityEngine;

namespace Module.Player.Skill.Editor
{
    [CustomEditor(typeof(PlayerSkillConfigSO), true)]
    public sealed class PlayerSkillConfigSOEditor : UnityEditor.Editor
    {
        // 绘制技能配置字段与动画时长同步按钮
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            GUILayout.Space(8f);
            drawSyncDurationButton();
        }

        // 绘制动画时长同步按钮
        private void drawSyncDurationButton()
        {
            PlayerSkillConfigSO config = (PlayerSkillConfigSO)target;

            if (config.StepCount == 0)
                EditorGUILayout.HelpBox("未配置技能段落，无法同步动画时长", MessageType.Info);

            if (!GUILayout.Button("同步全部动画时长"))
                return;

            Undo.RecordObject(config, "Sync Skill Step Durations");

            if (!config.SyncAllStepDurations())
            {
                EditorUtility.DisplayDialog("同步失败", "请至少配置一个有效的动画片段", "确定");
                return;
            }

            EditorUtility.SetDirty(config);
        }
    }
}

