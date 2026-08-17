/*
 * ┌───────────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 时间轴窗口内存草稿，承载独立窗口轨道的编辑数据
 * │  类    名: AbilityWindowDraft.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using Module.Ability.Data.Window.Audio;
using Module.Ability.Data.Window.Vfx;
using UnityEngine;

namespace Tools.AbilityComposer.Editor.View.Center.Timeline
{
    public enum AbilityWindowDraftType
    {
        Hit,
        StepAdvance,
        MovementLock,
        Vfx,
        Audio
    }

    public sealed class AbilityWindowDraft
    {
        private readonly List<AudioClip> m_audioClips = new List<AudioClip>();

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
        public AbilityAudioTriggerType AudioTriggerType { get; private set; }
        public AbilityAudioPlaybackType AudioPlaybackType { get; private set; }
        public IReadOnlyList<AudioClip> AudioClips => m_audioClips;
        public AudioClip AudioClipA => GetAudioClip(0);
        public AudioClip AudioClipB => GetAudioClip(1);
        public AudioClip AudioClipC => GetAudioClip(2);
        public float AudioVolume { get; private set; }
        public float AudioPitch { get; private set; }
        public float AudioRandomPitchRange { get; private set; }
        public bool AudioSpatial { get; private set; }
        public bool AudioStopOnWindowEnd { get; private set; }
        public AbilityAudioTargetType AudioTargetType { get; private set; }
        public string AudioSocketId { get; private set; }
        public Vector3 AudioLocalPositionOffset { get; private set; }

        // 在指定起始帧和右边界帧创建默认命中窗口草稿
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
            AudioTriggerType = AbilityAudioTriggerType.WindowEnter;
            AudioPlaybackType = AbilityAudioPlaybackType.OneShot;
            m_audioClips.Add(null);
            AudioVolume = 1f;
            AudioPitch = 1f;
            AudioSpatial = true;
            AudioStopOnWindowEnd = true;
            AudioTargetType = AbilityAudioTargetType.SourceRoot;
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

        // 更新窗口起始帧和右边界帧
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

        // 更新音效窗口触发方式
        public void SetAudioTriggerType(AbilityAudioTriggerType triggerType)
        {
            AudioTriggerType = triggerType;
        }

        // 更新音效窗口播放类型
        public void SetAudioPlaybackType(AbilityAudioPlaybackType playbackType)
        {
            AudioPlaybackType = playbackType;
        }

        // 更新音效窗口资源槽位
        public void SetAudioClip(int clipSlotIndex, AudioClip audioClip)
        {
            if (clipSlotIndex < 0)
                return;

            while (m_audioClips.Count <= clipSlotIndex)
                m_audioClips.Add(null);

            m_audioClips[clipSlotIndex] = audioClip;
        }

        // 整体替换音效窗口资源列表
        public void SetAudioClips(IReadOnlyList<AudioClip> audioClips)
        {
            m_audioClips.Clear();
            if (audioClips == null)
                return;

            for (int clipIndex = 0; clipIndex < audioClips.Count; clipIndex++)
                m_audioClips.Add(audioClips[clipIndex]);
        }

        // 新增一个音效资源槽位
        public void AddAudioClip(AudioClip audioClip)
        {
            m_audioClips.Add(audioClip);
        }

        // 删除指定音效资源槽位
        public void RemoveAudioClip(int clipSlotIndex)
        {
            if (clipSlotIndex < 0 || clipSlotIndex >= m_audioClips.Count)
                return;

            m_audioClips.RemoveAt(clipSlotIndex);
        }

        // 更新音效窗口音量
        public void SetAudioVolume(float volume)
        {
            AudioVolume = volume;
        }

        // 更新音效窗口音高
        public void SetAudioPitch(float pitch)
        {
            AudioPitch = pitch;
        }

        // 更新音效窗口随机音高范围
        public void SetAudioRandomPitchRange(float randomPitchRange)
        {
            AudioRandomPitchRange = randomPitchRange;
        }

        // 更新音效窗口空间化状态
        public void SetAudioSpatial(bool spatial)
        {
            AudioSpatial = spatial;
        }

        // 更新音效窗口结束截断状态
        public void SetAudioStopOnWindowEnd(bool stopOnWindowEnd)
        {
            AudioStopOnWindowEnd = stopOnWindowEnd;
        }

        // 更新音效窗口播放目标
        public void SetAudioTargetType(AbilityAudioTargetType targetType)
        {
            AudioTargetType = targetType;
        }

        // 更新音效窗口挂点 Id
        public void SetAudioSocketId(string socketId)
        {
            AudioSocketId = socketId;
        }

        // 更新音效窗口位置偏移
        public void SetAudioLocalPositionOffset(Vector3 localPositionOffset)
        {
            AudioLocalPositionOffset = localPositionOffset;
        }

        // 读取指定音效资源槽位
        private AudioClip GetAudioClip(int clipSlotIndex)
        {
            if (clipSlotIndex < 0 || clipSlotIndex >= m_audioClips.Count)
                return null;

            return m_audioClips[clipSlotIndex];
        }
    }
}
