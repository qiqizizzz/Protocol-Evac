/*
 * ┌──────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 特效窗口数据，保存特效预制体、目标与挂点参数
 * │  类    名: AbilityVfxWindowData.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────────────────┘
 */

using System;
using TriInspector;
using UnityEngine;

namespace Module.Ability.Data.Window.Vfx
{
    [Serializable]
    [DeclareFoldoutGroup("Transform", Title = "生成变换", Expanded = false)]
    public sealed class AbilityVfxWindowData : AbilityWindowDataBase
    {
        [LabelText("触发方式")]
        [SerializeField] private AbilityVfxTriggerType TriggerTypeValue;

        [LabelText("生成目标")]
        [SerializeField] private AbilityVfxTargetType TargetTypeValue;

        [LabelText("特效预制体")]
        [SerializeField] private GameObject VfxPrefabValue;

        [LabelText("挂点 Id")]
        [SerializeField] private string SocketIdValue;

        [LabelText("生命周期")]
        [SerializeField] private AbilityVfxLifeMode LifeModeValue;

        [Group("Transform")]
        [LabelText("位置偏移")]
        [SerializeField] private Vector3 LocalPositionOffsetValue;

        [Group("Transform")]
        [LabelText("旋转偏移")]
        [SerializeField] private Vector3 LocalEulerOffsetValue;

        [Group("Transform")]
        [LabelText("跟随目标")]
        [SerializeField] private bool FollowTargetValue = true;

        public AbilityVfxTriggerType TriggerType => TriggerTypeValue;
        public AbilityVfxTargetType TargetType => TargetTypeValue;
        public GameObject VfxPrefab => VfxPrefabValue;
        public string SocketId => SocketIdValue;
        public AbilityVfxLifeMode LifeMode => LifeModeValue;
        public Vector3 LocalPositionOffset => LocalPositionOffsetValue;
        public Vector3 LocalEulerOffset => LocalEulerOffsetValue;
        public bool FollowTarget => FollowTargetValue;

        public AbilityVfxWindowData()
        {
        }

        public AbilityVfxWindowData(float startNormalizedTime, float endNormalizedTime)
            : base(startNormalizedTime, endNormalizedTime)
        {
        }

        public AbilityVfxWindowData(string id, float startNormalizedTime, float endNormalizedTime,
            AbilityVfxTriggerType triggerType, AbilityVfxTargetType targetType, GameObject vfxPrefab,
            string socketId, AbilityVfxLifeMode lifeMode, Vector3 localPositionOffset, Vector3 localEulerOffset,
            bool followTarget)
            : base(id, startNormalizedTime, endNormalizedTime)
        {
            TriggerTypeValue = triggerType;
            TargetTypeValue = targetType;
            VfxPrefabValue = vfxPrefab;
            SocketIdValue = socketId;
            LifeModeValue = lifeMode;
            LocalPositionOffsetValue = localPositionOffset;
            LocalEulerOffsetValue = localEulerOffset;
            FollowTargetValue = followTarget;
        }
    }
}

