/*
 * ┌──────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 特效窗口控制器，负责按时间与命中事件生成特效
 * │  类    名: AbilityVfxWindowController.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Ability.Data.Window.Vfx;
using Module.Combat.Damage;
using UnityEngine;

namespace Module.Ability.Vfx
{
    public sealed class AbilityVfxWindowController
    {
        private readonly GameObject m_sourceRoot;
        private readonly VfxSocketBinder m_sourceSocketBinder;
        private readonly List<AbilityVfxWindowData> m_activeWindows = new List<AbilityVfxWindowData>();
        private readonly List<GameObject> m_durationInstances = new List<GameObject>();
        private readonly List<string> m_durationWindowIds = new List<string>();
        private readonly HashSet<string> m_enteredWindowIds = new HashSet<string>();

        private AbilityVfxWindowTrackData m_currentTrack;
        private int m_currentSegmentIndex = -1;
        private float m_currentNormalizedTime;

        // 创建特效窗口控制器
        public AbilityVfxWindowController(GameObject sourceRoot)
        {
            m_sourceRoot = sourceRoot;
            m_sourceSocketBinder = sourceRoot == null ? null : sourceRoot.GetComponentInChildren<VfxSocketBinder>();
        }

        // 根据当前段落与时间同步持续类和进入类特效
        public void Sync(AbilityVfxWindowTrackData windowTrack, float normalizedTime, int segmentIndex)
        {
            if (m_currentSegmentIndex != segmentIndex || m_currentTrack != windowTrack)
            {
                Close();
                m_currentSegmentIndex = segmentIndex;
                m_currentTrack = windowTrack;
            }

            m_currentNormalizedTime = normalizedTime;
            if (windowTrack == null)
            {
                Close();
                return;
            }

            windowTrack.GetActiveWindows(normalizedTime, m_activeWindows);
            SyncWindowEnterVfx();
            SyncWindowDurationVfx();
        }

        // 在真实命中时播放当前时间可用的 OnHit 特效
        public void PlayHitVfx(DamageData damageData, Component hitTarget)
        {
            if (m_currentTrack == null)
                return;

            m_currentTrack.GetActiveWindows(m_currentNormalizedTime, m_activeWindows);
            for (int windowIndex = 0; windowIndex < m_activeWindows.Count; windowIndex++)
            {
                AbilityVfxWindowData window = m_activeWindows[windowIndex];
                if (window.TriggerType != AbilityVfxTriggerType.OnHit)
                    continue;

                SpawnVfx(window, damageData, hitTarget);
            }
        }

        // 关闭当前所有持续特效
        public void Close()
        {
            for (int instanceIndex = 0; instanceIndex < m_durationInstances.Count; instanceIndex++)
                StopOrDestroyInstance(m_durationInstances[instanceIndex],
                    FindDurationWindowLifeMode(m_durationWindowIds[instanceIndex]));

            m_durationInstances.Clear();
            m_durationWindowIds.Clear();
            m_enteredWindowIds.Clear();
            m_currentSegmentIndex = -1;
            m_currentTrack = null;
            m_currentNormalizedTime = 0f;
        }

        // 同步只在窗口进入时生成一次的特效
        private void SyncWindowEnterVfx()
        {
            for (int windowIndex = 0; windowIndex < m_activeWindows.Count; windowIndex++)
            {
                AbilityVfxWindowData window = m_activeWindows[windowIndex];
                if (window.TriggerType != AbilityVfxTriggerType.WindowEnter || m_enteredWindowIds.Contains(window.Id))
                    continue;

                SpawnVfx(window, default, null);
                m_enteredWindowIds.Add(window.Id);
            }
        }

        // 同步窗口持续期间存在的特效
        private void SyncWindowDurationVfx()
        {
            for (int instanceIndex = m_durationInstances.Count - 1; instanceIndex >= 0; instanceIndex--)
            {
                if (ContainsActiveDurationWindow(m_durationWindowIds[instanceIndex]))
                    continue;

                StopOrDestroyInstance(m_durationInstances[instanceIndex],
                    FindDurationWindowLifeMode(m_durationWindowIds[instanceIndex]));
                m_durationInstances.RemoveAt(instanceIndex);
                m_durationWindowIds.RemoveAt(instanceIndex);
            }

            for (int windowIndex = 0; windowIndex < m_activeWindows.Count; windowIndex++)
            {
                AbilityVfxWindowData window = m_activeWindows[windowIndex];
                if (window.TriggerType != AbilityVfxTriggerType.WindowDuration
                    || m_durationWindowIds.Contains(window.Id))
                    continue;

                GameObject instance = SpawnVfx(window, default, null);
                if (instance == null)
                    continue;

                m_durationInstances.Add(instance);
                m_durationWindowIds.Add(window.Id);
            }
        }

        // 判断指定持续窗口当前是否仍处于活动状态
        private bool ContainsActiveDurationWindow(string windowId)
        {
            for (int windowIndex = 0; windowIndex < m_activeWindows.Count; windowIndex++)
            {
                AbilityVfxWindowData window = m_activeWindows[windowIndex];
                if (window.Id == windowId && window.TriggerType == AbilityVfxTriggerType.WindowDuration)
                    return true;
            }

            return false;
        }

        // 查询持续窗口结束时应使用的生命周期策略
        private AbilityVfxLifeMode FindDurationWindowLifeMode(string windowId)
        {
            if (m_currentTrack == null)
                return AbilityVfxLifeMode.DestroyOnWindowEnd;

            IReadOnlyList<AbilityVfxWindowData> windows = m_currentTrack.Windows;
            for (int windowIndex = 0; windowIndex < windows.Count; windowIndex++)
            {
                AbilityVfxWindowData window = windows[windowIndex];
                if (window.Id == windowId)
                    return window.LifeMode;
            }

            return AbilityVfxLifeMode.DestroyOnWindowEnd;
        }

        // 生成单个特效实例
        private GameObject SpawnVfx(AbilityVfxWindowData window, DamageData damageData, Component hitTarget)
        {
            if (window.VfxPrefab == null || !TryResolveSpawnTarget(window, damageData, hitTarget,
                    out Transform parent, out Vector3 position, out Quaternion rotation))
                return null;

            GameObject instance = Object.Instantiate(window.VfxPrefab, position, rotation,
                window.FollowTarget ? parent : null);
            if (window.FollowTarget)
            {
                instance.transform.localPosition = window.LocalPositionOffset;
                instance.transform.localRotation = Quaternion.Euler(window.LocalEulerOffset);
            }

            if (window.TriggerType != AbilityVfxTriggerType.WindowDuration
                || window.LifeMode == AbilityVfxLifeMode.AutoDestroy)
                AutoDestroyInstance(instance);

            return instance;
        }

        // 解析本次特效的生成目标
        private bool TryResolveSpawnTarget(AbilityVfxWindowData window, DamageData damageData, Component hitTarget,
            out Transform parent, out Vector3 position, out Quaternion rotation)
        {
            parent = null;
            position = Vector3.zero;
            rotation = Quaternion.identity;
            switch (window.TargetType)
            {
                case AbilityVfxTargetType.SourceSocket:
                    if (!TryResolveSourceSocket(window.SocketId, out parent))
                        return false;

                    break;
                case AbilityVfxTargetType.SourceRoot:
                    parent = m_sourceRoot == null ? null : m_sourceRoot.transform;
                    break;
                case AbilityVfxTargetType.HitPoint:
                    if (damageData.Source == null)
                        return false;

                    position = damageData.HitPoint + window.LocalPositionOffset;
                    rotation = Quaternion.LookRotation(-damageData.HitDirection, Vector3.up)
                        * Quaternion.Euler(window.LocalEulerOffset);
                    return true;
                case AbilityVfxTargetType.HitTargetRoot:
                    parent = hitTarget == null ? null : hitTarget.transform;
                    break;
                case AbilityVfxTargetType.HitTargetSocket:
                    if (!TryResolveHitTargetSocket(hitTarget, window.SocketId, out parent))
                        return false;

                    break;
            }

            if (parent == null)
                return false;

            position = parent.TransformPoint(window.LocalPositionOffset);
            rotation = parent.rotation * Quaternion.Euler(window.LocalEulerOffset);
            return true;
        }

        // 查询攻击者特效挂点
        private bool TryResolveSourceSocket(string socketId, out Transform socket)
        {
            socket = null;
            return m_sourceSocketBinder != null && m_sourceSocketBinder.TryGetSocket(socketId, out socket);
        }

        // 查询受击目标特效挂点
        private static bool TryResolveHitTargetSocket(Component hitTarget, string socketId, out Transform socket)
        {
            socket = null;
            if (hitTarget == null)
                return false;

            VfxSocketBinder socketBinder = hitTarget.GetComponentInChildren<VfxSocketBinder>();
            return socketBinder != null && socketBinder.TryGetSocket(socketId, out socket);
        }

        // 按生命周期策略停止或销毁特效
        private static void StopOrDestroyInstance(GameObject instance, AbilityVfxLifeMode lifeMode)
        {
            if (instance == null)
                return;

            if (lifeMode == AbilityVfxLifeMode.DestroyOnWindowEnd)
            {
                Object.Destroy(instance);
                return;
            }

            ParticleSystem[] particleSystems = instance.GetComponentsInChildren<ParticleSystem>();
            for (int particleIndex = 0; particleIndex < particleSystems.Length; particleIndex++)
                particleSystems[particleIndex].Stop(true, ParticleSystemStopBehavior.StopEmitting);

            AutoDestroyInstance(instance);
        }

        // 按粒子系统持续时间安排一次性特效销毁
        private static void AutoDestroyInstance(GameObject instance)
        {
            if (instance == null)
                return;

            ParticleSystem[] particleSystems = instance.GetComponentsInChildren<ParticleSystem>();
            float destroyDelay = 3f;
            for (int particleIndex = 0; particleIndex < particleSystems.Length; particleIndex++)
            {
                ParticleSystem.MainModule mainModule = particleSystems[particleIndex].main;
                destroyDelay = Mathf.Max(destroyDelay, mainModule.duration + mainModule.startLifetime.constantMax);
            }

            Object.Destroy(instance, destroyDelay);
        }
    }
}
