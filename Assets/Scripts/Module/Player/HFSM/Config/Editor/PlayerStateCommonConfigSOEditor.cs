/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家状态通用配置 Inspector，提供动画时长同步按钮
 * │  类    名: PlayerStateCommonConfigSOEditor.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using Module.Player.HFSM.Config.Common;
using UnityEditor;
using UnityEngine;

namespace Module.Player.HFSM.Config.Editor
{
    [CustomEditor(typeof(PlayerStateCommonConfigSO), true)]
    public sealed class PlayerStateCommonConfigSOEditor : UnityEditor.Editor
    {
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
            PlayerStateCommonConfigSO config = (PlayerStateCommonConfigSO)target;

            if (config.StateClipCount == 0)
                EditorGUILayout.HelpBox("未配置状态动画段落，无法同步动画时长", MessageType.Info);

            if (!GUILayout.Button("同步全部动画时长"))
                return;

            Undo.RecordObject(config, "Sync State Clip Durations");

            if (!config.SyncAllClipDurations())
            {
                EditorUtility.DisplayDialog("同步失败", "请至少配置一个有效的动画片段", "确定");
                return;
            }

            EditorUtility.SetDirty(config);
        }
    }
}
