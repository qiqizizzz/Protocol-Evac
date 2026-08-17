/*
 * ┌───────────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 时间轴窗口内存草稿，承载独立窗口轨道的编辑数据
 * │  类    名: AbilityWindowDraft.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────────────┘
 */

using System;

using Module.Ability.Data.Window.Vfx;
using UnityEngine;

namespace Tools.AbilityComposer.Editor.View.Center.Timeline
{
    public enum AbilityWindowDraftType
    {
        Hit,
        StepAdvance,
        MovementLock,
        Vfx
    }

    public sealed class AbilityWindowDraft
    {
        public string Id { get; private set; }
        public AbilityWindowDraftType Type { get; private set; }
        public int StartFrame { get; private set; }
        public int EndFrame { get; private set; }
        public float Damage { get; private set; }
        public AbilityVfxTriggerType VfxTriggerType { get; private set; }
        public AbilityVfxTargetType VfxTargetType { get; private set; }
        public GameObject VfxPrefab { get; private set; }
        public string VfxSocketId { get; private set; }
        public AbilityVfxLifeMode VfxLifeMode { get; private set; }
        public Vector3 VfxLocalPositionOffset { get; private set; }
        public Vector3 VfxLocalEulerOffset { get; private set; }
        public bool VfxFollowTarget { get; private set; }

        // 在指定帧创建默认命中窗口草稿
        public AbilityWindowDraft(int startFrame, int endFrame)
        {
            Id = Guid.NewGuid().ToString("N");
            Type = AbilityWindowDraftType.Hit;
            StartFrame = startFrame;
            EndFrame = endFrame;
            Damage = 1f;
            VfxTriggerType = AbilityVfxTriggerType.WindowDuration;
            VfxTargetType = AbilityVfxTargetType.SourceSocket;
            VfxSocketId = "WeaponTrail";
            VfxLifeMode = AbilityVfxLifeMode.DestroyOnWindowEnd;
            VfxFollowTarget = true;
        }

        // 切换窗口所属的独立轨道类型
        public void SetType(AbilityWindowDraftType type)
        {
            Type = type;
        }

        // 恢复已保存窗口的稳定标识
        public void SetId(string id)
        {
            if (!string.IsNullOrEmpty(id))
                Id = id;
        }

        // 更新窗口左右边界
        public void SetFrames(int startFrame, int endFrame)
        {
            StartFrame = startFrame;
            EndFrame = endFrame;
        }

        // 更新命中窗口的伤害参数
        public void SetDamage(float damage)
        {
            Damage = damage;
        }

        // 更新特效窗口触发方式
        public void SetVfxTriggerType(AbilityVfxTriggerType triggerType)
        {
            VfxTriggerType = triggerType;
        }

        // 更新特效窗口生成目标
        public void SetVfxTargetType(AbilityVfxTargetType targetType)
        {
            VfxTargetType = targetType;
        }

        // 更新特效窗口预制体
        public void SetVfxPrefab(GameObject vfxPrefab)
        {
            VfxPrefab = vfxPrefab;
        }

        // 更新特效窗口挂点 Id
        public void SetVfxSocketId(string socketId)
        {
            VfxSocketId = socketId;
        }

        // 更新特效窗口生命周期模式
        public void SetVfxLifeMode(AbilityVfxLifeMode lifeMode)
        {
            VfxLifeMode = lifeMode;
        }

        // 更新特效窗口位置偏移
        public void SetVfxLocalPositionOffset(Vector3 localPositionOffset)
        {
            VfxLocalPositionOffset = localPositionOffset;
        }

        // 更新特效窗口旋转偏移
        public void SetVfxLocalEulerOffset(Vector3 localEulerOffset)
        {
            VfxLocalEulerOffset = localEulerOffset;
        }

        // 更新特效窗口跟随目标状态
        public void SetVfxFollowTarget(bool followTarget)
        {
            VfxFollowTarget = followTarget;
        }
    }
}
