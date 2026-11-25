/*
 * ┌──────────────────────────────────────────────────────────────┐
 * │  描    述: 角色层级状态机外壳，负责状态树校验、活动路径维护与状态切换
 * │  类    名: CharacterStateMachine.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Utils.log;

namespace Framework.Character.HFSM
{
    /// <summary>
    /// 角色层级状态机，所有角色共用同一套状态树校验与切换流程
    /// </summary>
    /// <typeparam name="TStateId">角色状态标识枚举，默认值表示空状态</typeparam>
    public sealed class CharacterStateMachine<TStateId> where TStateId : struct, Enum
    {
        public bool IsInited { get; private set; }

        // 是否发生过生命周期异常，故障后状态机拒绝继续执行
        public bool IsFaulted { get; private set; }

        private static readonly EqualityComparer<TStateId> m_idComparer = EqualityComparer<TStateId>.Default;

        private readonly Dictionary<TStateId, CharacterStateBase<TStateId>> m_states;
        private readonly List<TStateId> m_activeStatePath;
        private readonly ReadOnlyCollection<TStateId> m_readOnlyActiveStatePath;

        private bool m_isRegistryLocked;
        private bool m_isExecutingLifecycle;

        // 当前叶子状态，未初始化时返回默认状态标识
        public TStateId CurrentLeafStateId =>
            m_activeStatePath.Count > 0 ? m_activeStatePath[^1] : default;

        // 当前完整活动路径，从顶层状态到叶子状态
        public IReadOnlyList<TStateId> ActiveStatePath => m_readOnlyActiveStatePath;

        public CharacterStateMachine()
        {
            m_states = new Dictionary<TStateId, CharacterStateBase<TStateId>>();
            m_activeStatePath = new List<TStateId>();
            m_readOnlyActiveStatePath = m_activeStatePath.AsReadOnly();
        }

        /// <summary>
        /// 注册状态，初始化完成后禁止继续注册
        /// </summary>
        /// <param name="state">待注册的状态实例</param>
        public void RegisterState(CharacterStateBase<TStateId> state)
        {
            if (state == null)
            {
                QLog.Error("注册状态失败：state 为空");
                return;
            }

            if (m_isRegistryLocked)
            {
                QLog.Error("注册状态失败：状态机初始化后不能继续注册状态");
                return;
            }

            if (m_idComparer.Equals(state.Id, default))
            {
                QLog.Error("注册状态失败：默认状态标识只能作为顶层状态的父节点");
                return;
            }

            if (m_states.ContainsKey(state.Id))
            {
                QLog.Error($"注册状态失败：状态 ID 重复注册：{state.Id}");
                return;
            }

            m_states.Add(state.Id, state);
        }

        /// <summary>
        /// 校验状态树并进入初始状态
        /// </summary>
        /// <param name="initStateId">初始状态标识</param>
        public void Init(TStateId initStateId)
        {
            if (m_isRegistryLocked)
            {
                QLog.Error("初始化失败：状态机不能重复初始化");
                return;
            }

            if (m_idComparer.Equals(initStateId, default))
            {
                QLog.Error("初始化失败：初始状态不能是默认状态标识");
                return;
            }

            if (!ValidateStateTree())
                return;

            List<TStateId> initialPath = BuildExpandedPath(initStateId);

            if (initialPath.Count == 0)
                return;

            m_isRegistryLocked = true;
            m_isExecutingLifecycle = true;

            try
            {
                EnterInitialPath(initialPath);
                IsInited = true;
            }
            catch (Exception exception)
            {
                IsFaulted = true;
                QLog.Throw(exception);
            }
            finally
            {
                m_isExecutingLifecycle = false;
            }
        }

        /// <summary>
        /// 切换到目标状态，复合状态会自动展开到默认叶子状态
        /// </summary>
        /// <param name="targetStateId">目标状态标识</param>
        /// <param name="allowReentry">目标已是当前叶子状态时是否强制重新进入</param>
        public void ChangeState(TStateId targetStateId, bool allowReentry = false)
        {
            if (!IsStateMachineValid(nameof(ChangeState)))
                return;

            List<TStateId> targetPath = BuildExpandedPath(targetStateId);

            if (targetPath.Count == 0)
                return;

            bool isSameActivePath = IsSameActivePath(targetPath);
            if (isSameActivePath && !allowReentry)
                return;

            int commonPrefixLength = isSameActivePath
                ? targetPath.Count - 1
                : GetCommonPrefixLength(targetPath);

            m_isExecutingLifecycle = true;

            try
            {
                ExitCurrentPath(commonPrefixLength);
                EnterTargetPath(targetPath, commonPrefixLength);
            }
            catch (Exception exception)
            {
                IsFaulted = true;
                QLog.Throw(exception);
            }
            finally
            {
                m_isExecutingLifecycle = false;
            }
        }

        /// <summary>
        /// 按父状态到叶子状态的顺序执行帧更新
        /// </summary>
        /// <param name="deltaTime">本帧时间增量</param>
        public void Tick(float deltaTime)
        {
            if (!IsStateMachineValid(nameof(Tick)))
                return;

            m_isExecutingLifecycle = true;

            try
            {
                for (int i = 0; i < m_activeStatePath.Count; i++)
                    m_states[m_activeStatePath[i]].Tick(deltaTime);
            }
            catch (Exception exception)
            {
                IsFaulted = true;
                QLog.Throw(exception);
            }
            finally
            {
                m_isExecutingLifecycle = false;
            }
        }

        /// <summary>
        /// 按父状态到叶子状态的顺序执行物理帧更新
        /// </summary>
        /// <param name="fixedDeltaTime">本物理帧时间增量</param>
        public void FixedTick(float fixedDeltaTime)
        {
            if (!IsStateMachineValid(nameof(FixedTick)))
                return;

            m_isExecutingLifecycle = true;

            try
            {
                for (int i = 0; i < m_activeStatePath.Count; i++)
                    m_states[m_activeStatePath[i]].FixedTick(fixedDeltaTime);
            }
            catch (Exception exception)
            {
                IsFaulted = true;
                QLog.Throw(exception);
            }
            finally
            {
                m_isExecutingLifecycle = false;
            }
        }

        /// <summary>
        /// 判断指定状态是否位于当前活动路径中
        /// </summary>
        /// <param name="stateId">待查询的状态标识</param>
        /// <returns>该状态当前是否处于活动路径</returns>
        public bool IsStateActive(TStateId stateId)
        {
            for (int i = 0; i < m_activeStatePath.Count; i++)
            {
                if (m_idComparer.Equals(m_activeStatePath[i], stateId))
                    return true;
            }

            return false;
        }

        #region 检验状态树
        // 校验状态树的父子关系、环路与默认子状态
        private bool ValidateStateTree()
        {
            if (m_states.Count == 0)
            {
                QLog.Error("状态树校验失败：状态机至少需要注册一个状态");
                return false;
            }

            foreach (CharacterStateBase<TStateId> state in m_states.Values)
            {
                if (!ValidateParent(state))
                    return false;
            }

            foreach (CharacterStateBase<TStateId> state in m_states.Values)
            {
                if (!ValidateParentChain(state))
                    return false;
            }

            foreach (CharacterStateBase<TStateId> state in m_states.Values)
            {
                if (state is ICharacterCompositeState<TStateId> compositeState &&
                    !ValidateInitialChild(state, compositeState))
                    return false;
            }

            return true;
        }

        // 校验状态声明的父节点存在且为复合状态
        private bool ValidateParent(CharacterStateBase<TStateId> state)
        {
            if (m_idComparer.Equals(state.ParentId, default))
                return true;

            if (!m_states.TryGetValue(state.ParentId, out CharacterStateBase<TStateId> parentState))
            {
                QLog.Error($"状态树校验失败：状态 {state.Id} 的父状态不存在：{state.ParentId}");
                return false;
            }

            if (parentState is not ICharacterCompositeState<TStateId>)
            {
                QLog.Error($"状态树校验失败：状态 {state.Id} 的父状态不是复合状态：{state.ParentId}");
                return false;
            }

            return true;
        }

        // 校验状态的父链不存在环路
        private bool ValidateParentChain(CharacterStateBase<TStateId> state)
        {
            HashSet<TStateId> visitedStateIds = new HashSet<TStateId>();

            CharacterStateBase<TStateId> currentState = state;

            while (!m_idComparer.Equals(currentState.ParentId, default))
            {
                if (!visitedStateIds.Add(currentState.Id))
                {
                    QLog.Error($"状态树校验失败：状态树存在父子环路：{currentState.Id}");
                    return false;
                }

                if (!m_states.TryGetValue(currentState.ParentId, out currentState))
                    return false;
            }

            return true;
        }

        // 校验复合状态的默认子状态是已注册的直接子状态
        private bool ValidateInitialChild(CharacterStateBase<TStateId> state,
            ICharacterCompositeState<TStateId> compositeState)
        {
            TStateId initialChildId = compositeState.GetInitialChildId();

            if (m_idComparer.Equals(initialChildId, default))
            {
                QLog.Error($"状态树校验失败：复合状态 {state.Id} 未指定默认子状态");
                return false;
            }

            if (!m_states.TryGetValue(initialChildId, out CharacterStateBase<TStateId> initialChildState))
            {
                QLog.Error($"状态树校验失败：复合状态 {state.Id} 的默认子状态未注册：{initialChildId}");
                return false;
            }

            if (!m_idComparer.Equals(initialChildState.ParentId, state.Id))
            {
                QLog.Error($"状态树校验失败：状态 {initialChildId} 不是复合状态 {state.Id} 的直接子状态");
                return false;
            }

            return true;
        }

        // 构建从顶层状态到目标叶子状态的完整路径
        private List<TStateId> BuildExpandedPath(TStateId targetStateId)
        {
            if (!m_states.TryGetValue(targetStateId, out CharacterStateBase<TStateId> targetState))
            {
                QLog.Error($"构建状态路径失败：目标状态未注册：{targetStateId}");
                return new List<TStateId>();
            }

            List<TStateId> targetPath = new List<TStateId>();

            CharacterStateBase<TStateId> currentState = targetState;

            while (true)
            {
                targetPath.Add(currentState.Id);

                if (m_idComparer.Equals(currentState.ParentId, default))
                    break;

                if (!m_states.TryGetValue(currentState.ParentId, out currentState))
                    return new List<TStateId>();
            }

            targetPath.Reverse();

            while (m_states.TryGetValue(targetPath[^1], out CharacterStateBase<TStateId> leafState) &&
                   leafState is ICharacterCompositeState<TStateId> compositeState)
            {
                targetPath.Add(compositeState.GetInitialChildId());
            }

            return targetPath;
        }

        // 按父状态到叶子状态的顺序进入初始路径
        private void EnterInitialPath(IReadOnlyList<TStateId> initialPath)
        {
            for (int i = 0; i < initialPath.Count; i++)
            {
                m_activeStatePath.Add(initialPath[i]);
                m_states[initialPath[i]].Enter();
            }
        }
        #endregion

        #region 检验状态
        // 校验状态机是否处于可执行状态
        private bool IsStateMachineValid(string operationName)
        {
            if (m_isExecutingLifecycle)
            {
                QLog.Throw(new InvalidOperationException(
                    $"状态机正在执行生命周期方法，禁止重入调用：{operationName}"));
                return false;
            }

            if (!IsInited)
            {
                QLog.Error($"状态机未初始化，不能执行：{operationName}");
                return false;
            }

            if (IsFaulted)
            {
                QLog.Error($"状态机处于故障状态，不能执行：{operationName}");
                return false;
            }

            return true;
        }

        // 判断目标路径是否与当前活动路径完全一致
        private bool IsSameActivePath(IReadOnlyList<TStateId> targetPath)
        {
            if (m_activeStatePath.Count != targetPath.Count)
                return false;

            for (int i = 0; i < m_activeStatePath.Count; i++)
            {
                if (!m_idComparer.Equals(m_activeStatePath[i], targetPath[i]))
                    return false;
            }

            return true;
        }

        // 计算当前活动路径与目标路径的公共前缀长度
        private int GetCommonPrefixLength(IReadOnlyList<TStateId> targetPath)
        {
            int maxLength = m_activeStatePath.Count < targetPath.Count ? m_activeStatePath.Count : targetPath.Count;

            for (int i = 0; i < maxLength; i++)
            {
                if (!m_idComparer.Equals(m_activeStatePath[i], targetPath[i]))
                    return i;
            }

            return maxLength;
        }

        // 从当前叶子状态向上退出到公共前缀之后
        private void ExitCurrentPath(int commonPrefixLength)
        {
            for (int i = m_activeStatePath.Count - 1; i >= commonPrefixLength; i--)
            {
                m_states[m_activeStatePath[i]].Exit();
                m_activeStatePath.RemoveAt(i);
            }
        }

        // 从公共前缀之后进入目标路径到叶子状态
        private void EnterTargetPath(IReadOnlyList<TStateId> targetPath, int commonPrefixLength)
        {
            for (int i = commonPrefixLength; i < targetPath.Count; i++)
            {
                m_activeStatePath.Add(targetPath[i]);
                m_states[targetPath[i]].Enter();
            }
        }
        #endregion
    }
}
