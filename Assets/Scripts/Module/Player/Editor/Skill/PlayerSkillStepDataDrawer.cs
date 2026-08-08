/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家技能段落数据绘制器，使用唯一属性路径隔离数组元素输入焦点
 * │  类    名: PlayerSkillStepDataDrawer.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using Module.Player.Skill.Data;
using TriInspector;
using TriInspector.Utilities;
using UnityEditor;
using UnityEngine;

namespace Module.Player.Editor.Skill
{
    [CustomPropertyDrawer(typeof(PlayerSkillStepData))]
    public sealed class PlayerSkillStepDataDrawer : PropertyDrawer
    {
        private const float GROUP_HEADER_HEIGHT = 22f;
        private const float GROUP_INSET = 4f;
        private const float HEADER_HORIZONTAL_INSET = 6f;

        private const string BEGIN_ANIMATION_GROUP = "BeginAnimation";
        private const string RECOVERY_ANIMATION_GROUP = "RecoveryAnimation";
        private const string STEP_SETTINGS_GROUP = "StepSettings";
        private const string STEP_ADVANCE_GROUP = "StepAdvanceWindow";
        private const string HIT_GROUP = "HitWindow";

        private const string BEGIN_ANIMATION_CLIP_PROPERTY = "BeginAnimationClipValue";
        private const string BEGIN_DURATION_PROPERTY = "BeginDurationValue";
        private const string BEGIN_USE_ROOT_MOTION_PROPERTY = "BeginUseRootMotionValue";
        private const string BEGIN_CAN_END_EARLY_PROPERTY = "BeginCanEndEarlyValue";
        private const string RECOVERY_ANIMATION_CLIP_PROPERTY = "RecoveryAnimationClipValue";
        private const string RECOVERY_DURATION_PROPERTY = "RecoveryDurationValue";
        private const string RECOVERY_USE_ROOT_MOTION_PROPERTY = "RecoveryUseRootMotionValue";
        private const string RECOVERY_CAN_END_EARLY_PROPERTY = "RecoveryCanEndEarlyValue";
        private const string SHOW_WEAPON_PROPERTY = "ShowWeaponValue";
        private const string USE_STEP_ADVANCE_WINDOW_PROPERTY = "UseStepAdvanceWindowValue";
        private const string STEP_ADVANCE_OPEN_PROPERTY = "StepAdvanceOpenNormalizedTimeValue";
        private const string STEP_ADVANCE_CLOSE_PROPERTY = "StepAdvanceCloseNormalizedTimeValue";
        private const string USE_HIT_WINDOW_PROPERTY = "UseHitWindowValue";
        private const string HIT_OPEN_PROPERTY = "HitOpenNormalizedTimeValue";
        private const string HIT_CLOSE_PROPERTY = "HitCloseNormalizedTimeValue";
        private const string DAMAGE_PROPERTY = "DamageValue";

        private static readonly GUIContent SBeginAnimationClipLabel = new GUIContent("动画片段");
        private static readonly GUIContent SBeginDurationLabel = new GUIContent("持续时间");
        private static readonly GUIContent SBeginUseRootMotionLabel = new GUIContent("使用 Root Motion");
        private static readonly GUIContent SBeginCanEndEarlyLabel = new GUIContent("允许提前结束");
        private static readonly GUIContent SRecoveryAnimationClipLabel = new GUIContent("动画片段");
        private static readonly GUIContent SRecoveryDurationLabel = new GUIContent("持续时间");
        private static readonly GUIContent SRecoveryUseRootMotionLabel = new GUIContent("使用 Root Motion");
        private static readonly GUIContent SRecoveryCanEndEarlyLabel = new GUIContent("允许提前结束");
        private static readonly GUIContent SShowWeaponLabel = new GUIContent("显示武器");
        private static readonly GUIContent SUseStepAdvanceWindowLabel = new GUIContent("启用推进窗口");
        private static readonly GUIContent SStepAdvanceOpenLabel = new GUIContent("开始时间");
        private static readonly GUIContent SStepAdvanceCloseLabel = new GUIContent("结束时间");
        private static readonly GUIContent SUseHitWindowLabel = new GUIContent("启用命中窗口");
        private static readonly GUIContent SHitOpenLabel = new GUIContent("开始时间");
        private static readonly GUIContent SHitCloseLabel = new GUIContent("结束时间");
        private static readonly GUIContent SDamageLabel = new GUIContent("伤害");

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
                return height;

            height += EditorGUIUtility.standardVerticalSpacing;
            height += GetGroupHeight(property, BEGIN_ANIMATION_GROUP, 4);
            height += EditorGUIUtility.standardVerticalSpacing;
            height += GetGroupHeight(property, RECOVERY_ANIMATION_GROUP, 4);
            height += EditorGUIUtility.standardVerticalSpacing;
            height += GetGroupHeight(property, STEP_SETTINGS_GROUP, 1);
            height += EditorGUIUtility.standardVerticalSpacing;

            SerializedProperty useStepAdvance = property.FindPropertyRelative(USE_STEP_ADVANCE_WINDOW_PROPERTY);
            height += GetGroupHeight(property, STEP_ADVANCE_GROUP, useStepAdvance.boolValue ? 3 : 1);
            height += EditorGUIUtility.standardVerticalSpacing;

            SerializedProperty useHit = property.FindPropertyRelative(USE_HIT_WINDOW_PROPERTY);
            height += GetGroupHeight(property, HIT_GROUP, useHit.boolValue ? 4 : 1);
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect elementHeaderRect = TakeRect(ref position, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(elementHeaderRect, property.isExpanded, label, true);
            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            position.y += EditorGUIUtility.standardVerticalSpacing;
            DrawBeginAnimationGroup(ref position, property);
            position.y += EditorGUIUtility.standardVerticalSpacing;
            DrawRecoveryAnimationGroup(ref position, property);
            position.y += EditorGUIUtility.standardVerticalSpacing;
            DrawStepSettingsGroup(ref position, property);
            position.y += EditorGUIUtility.standardVerticalSpacing;
            DrawStepAdvanceGroup(ref position, property);
            position.y += EditorGUIUtility.standardVerticalSpacing;
            DrawHitGroup(ref position, property);

            EditorGUI.EndProperty();
        }

        // 绘制攻击阶段动画分组
        private static void DrawBeginAnimationGroup(ref Rect position, SerializedProperty property)
        {
            const int FIELD_COUNT = 4;
            float groupHeight = GetGroupHeight(property, BEGIN_ANIMATION_GROUP, FIELD_COUNT);
            Rect groupRect = TakeRect(ref position, groupHeight);
            if (!DrawGroupHeader(groupRect, property, BEGIN_ANIMATION_GROUP, "攻击阶段"))
                return;

            Rect contentRect = GetGroupContentRect(groupRect, FIELD_COUNT);
            DrawProperty(ref contentRect, property.FindPropertyRelative(BEGIN_ANIMATION_CLIP_PROPERTY), SBeginAnimationClipLabel);
            DrawNonNegativeFloat(ref contentRect, property.FindPropertyRelative(BEGIN_DURATION_PROPERTY), SBeginDurationLabel);
            DrawProperty(ref contentRect, property.FindPropertyRelative(BEGIN_USE_ROOT_MOTION_PROPERTY), SBeginUseRootMotionLabel);
            DrawProperty(ref contentRect, property.FindPropertyRelative(BEGIN_CAN_END_EARLY_PROPERTY), SBeginCanEndEarlyLabel);
        }

        // 绘制收招阶段动画分组
        private static void DrawRecoveryAnimationGroup(ref Rect position, SerializedProperty property)
        {
            const int FIELD_COUNT = 4;
            float groupHeight = GetGroupHeight(property, RECOVERY_ANIMATION_GROUP, FIELD_COUNT);
            Rect groupRect = TakeRect(ref position, groupHeight);
            if (!DrawGroupHeader(groupRect, property, RECOVERY_ANIMATION_GROUP, "收招阶段"))
                return;

            Rect contentRect = GetGroupContentRect(groupRect, FIELD_COUNT);
            DrawProperty(ref contentRect, property.FindPropertyRelative(RECOVERY_ANIMATION_CLIP_PROPERTY), SRecoveryAnimationClipLabel);
            DrawNonNegativeFloat(ref contentRect, property.FindPropertyRelative(RECOVERY_DURATION_PROPERTY), SRecoveryDurationLabel);
            DrawProperty(ref contentRect, property.FindPropertyRelative(RECOVERY_USE_ROOT_MOTION_PROPERTY), SRecoveryUseRootMotionLabel);
            DrawProperty(ref contentRect, property.FindPropertyRelative(RECOVERY_CAN_END_EARLY_PROPERTY), SRecoveryCanEndEarlyLabel);
        }

        // 绘制段落通用设置分组
        private static void DrawStepSettingsGroup(ref Rect position, SerializedProperty property)
        {
            const int FIELD_COUNT = 1;
            float groupHeight = GetGroupHeight(property, STEP_SETTINGS_GROUP, FIELD_COUNT);
            Rect groupRect = TakeRect(ref position, groupHeight);
            if (!DrawGroupHeader(groupRect, property, STEP_SETTINGS_GROUP, "段落设置"))
                return;

            Rect contentRect = GetGroupContentRect(groupRect, FIELD_COUNT);
            DrawProperty(ref contentRect, property.FindPropertyRelative(SHOW_WEAPON_PROPERTY), SShowWeaponLabel);
        }

        // 绘制段落推进窗口分组
        private static void DrawStepAdvanceGroup(ref Rect position, SerializedProperty property)
        {
            SerializedProperty useWindow = property.FindPropertyRelative(USE_STEP_ADVANCE_WINDOW_PROPERTY);
            int fieldCount = useWindow.boolValue ? 3 : 1;
            float groupHeight = GetGroupHeight(property, STEP_ADVANCE_GROUP, fieldCount);
            Rect groupRect = TakeRect(ref position, groupHeight);
            if (!DrawGroupHeader(groupRect, property, STEP_ADVANCE_GROUP, "段落推进窗口"))
                return;

            Rect contentRect = GetGroupContentRect(groupRect, fieldCount);
            DrawProperty(ref contentRect, useWindow, SUseStepAdvanceWindowLabel);
            if (!useWindow.boolValue)
                return;

            DrawNormalizedSlider(ref contentRect, property.FindPropertyRelative(STEP_ADVANCE_OPEN_PROPERTY), SStepAdvanceOpenLabel);
            DrawNormalizedSlider(ref contentRect, property.FindPropertyRelative(STEP_ADVANCE_CLOSE_PROPERTY), SStepAdvanceCloseLabel);
        }

        // 绘制命中窗口分组
        private static void DrawHitGroup(ref Rect position, SerializedProperty property)
        {
            SerializedProperty useWindow = property.FindPropertyRelative(USE_HIT_WINDOW_PROPERTY);
            int fieldCount = useWindow.boolValue ? 4 : 1;
            float groupHeight = GetGroupHeight(property, HIT_GROUP, fieldCount);
            Rect groupRect = TakeRect(ref position, groupHeight);
            if (!DrawGroupHeader(groupRect, property, HIT_GROUP, "命中窗口"))
                return;

            Rect contentRect = GetGroupContentRect(groupRect, fieldCount);
            DrawProperty(ref contentRect, useWindow, SUseHitWindowLabel);
            if (!useWindow.boolValue)
                return;

            DrawNormalizedSlider(ref contentRect, property.FindPropertyRelative(HIT_OPEN_PROPERTY), SHitOpenLabel);
            DrawNormalizedSlider(ref contentRect, property.FindPropertyRelative(HIT_CLOSE_PROPERTY), SHitCloseLabel);
            DrawNonNegativeFloat(ref contentRect, property.FindPropertyRelative(DAMAGE_PROPERTY), SDamageLabel);
        }

        // 绘制原生 Tri 风格的分组标题并返回展开状态
        private static bool DrawGroupHeader(Rect groupRect, SerializedProperty property, string groupName, string title)
        {
            Rect headerRect = new Rect(groupRect.x, groupRect.y, groupRect.width, GROUP_HEADER_HEIGHT);
            TriEditorGUI.DrawBox(headerRect, TriEditorStyles.TabOnlyOne);

            Rect labelRect = new Rect(
                headerRect.x + HEADER_HORIZONTAL_INSET,
                headerRect.y + 2f,
                headerRect.width - HEADER_HORIZONTAL_INSET * 2f,
                headerRect.height - 4f);

            bool expanded = GetGroupExpanded(property, groupName);
            bool nextExpanded = EditorGUI.Foldout(labelRect, expanded, title, true);
            if (expanded != nextExpanded)
                SetGroupExpanded(property, groupName, nextExpanded);

            if (nextExpanded)
            {
                Rect contentBoxRect = new Rect(
                    groupRect.x,
                    headerRect.yMax,
                    groupRect.width,
                    groupRect.height - GROUP_HEADER_HEIGHT);
                TriEditorGUI.DrawBox(contentBoxRect, TriEditorStyles.ContentBox);
            }

            return nextExpanded;
        }

        // 绘制普通序列化字段
        private static void DrawProperty(ref Rect contentRect, SerializedProperty property, GUIContent label)
        {
            Rect fieldRect = TakeFieldRect(ref contentRect);
            GUIContent content = new GUIContent(label.text, property.tooltip);
            EditorGUI.PropertyField(fieldRect, property, content, true);
        }

        // 绘制使用完整属性路径命名的归一化滑动条
        private static void DrawNormalizedSlider(ref Rect contentRect, SerializedProperty property, GUIContent label)
        {
            Rect fieldRect = TakeFieldRect(ref contentRect);
            GUIContent content = new GUIContent(label.text, property.tooltip);

            EditorGUI.BeginProperty(fieldRect, content, property);
            GUI.SetNextControlName(property.propertyPath);
            EditorGUI.BeginChangeCheck();
            float value = EditorGUI.Slider(fieldRect, content, property.floatValue, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
                property.floatValue = value;
            EditorGUI.EndProperty();
        }

        // 绘制使用完整属性路径命名的非负浮点字段
        private static void DrawNonNegativeFloat(ref Rect contentRect, SerializedProperty property, GUIContent label)
        {
            Rect fieldRect = TakeFieldRect(ref contentRect);
            GUIContent content = new GUIContent(label.text, property.tooltip);

            EditorGUI.BeginProperty(fieldRect, content, property);
            GUI.SetNextControlName(property.propertyPath);
            EditorGUI.BeginChangeCheck();
            float value = EditorGUI.FloatField(fieldRect, content, property.floatValue);
            if (EditorGUI.EndChangeCheck())
                property.floatValue = Mathf.Max(0f, value);
            EditorGUI.EndProperty();
        }

        // 获取 Tri 分组总高度
        private static float GetGroupHeight(SerializedProperty property, string groupName, int fieldCount)
        {
            if (!GetGroupExpanded(property, groupName))
                return GROUP_HEADER_HEIGHT;

            return GROUP_HEADER_HEIGHT + GROUP_INSET * 2f + GetFieldsHeight(fieldCount);
        }

        // 获取分组内容绘制区域
        private static Rect GetGroupContentRect(Rect groupRect, int fieldCount)
        {
            return new Rect(
                groupRect.x + GROUP_INSET,
                groupRect.y + GROUP_HEADER_HEIGHT + GROUP_INSET,
                groupRect.width - GROUP_INSET * 2f,
                GetFieldsHeight(fieldCount));
        }

        // 获取指定数量字段占用的总高度
        private static float GetFieldsHeight(int fieldCount)
        {
            return fieldCount * EditorGUIUtility.singleLineHeight
                   + (fieldCount - 1) * EditorGUIUtility.standardVerticalSpacing;
        }

        // 从内容区域顶部取出一个字段矩形
        private static Rect TakeFieldRect(ref Rect contentRect)
        {
            Rect fieldRect = TakeRect(ref contentRect, EditorGUIUtility.singleLineHeight);
            contentRect.y += EditorGUIUtility.standardVerticalSpacing;
            return fieldRect;
        }

        // 从剩余区域顶部取出指定高度矩形
        private static Rect TakeRect(ref Rect position, float height)
        {
            Rect result = new Rect(position.x, position.y, position.width, height);
            position.y += height;
            position.height -= height;
            return result;
        }

        // 读取当前数组元素下的分组展开状态
        private static bool GetGroupExpanded(SerializedProperty property, string groupName)
        {
            return SessionState.GetBool(GetGroupStateKey(property, groupName), true);
        }

        // 保存当前数组元素下的分组展开状态
        private static void SetGroupExpanded(SerializedProperty property, string groupName, bool expanded)
        {
            SessionState.SetBool(GetGroupStateKey(property, groupName), expanded);
        }

        // 创建包含对象和完整属性路径的唯一分组状态键
        private static string GetGroupStateKey(SerializedProperty property, string groupName)
        {
            int targetId = property.serializedObject.targetObject.GetInstanceID();
            return $"PlayerSkillStepData.{targetId}.{property.propertyPath}.{groupName}";
        }
    }
}
