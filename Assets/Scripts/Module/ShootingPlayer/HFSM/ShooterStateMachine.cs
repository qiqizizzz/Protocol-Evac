/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色层级状态机，负责状态树校验、活动路径维护与状态切换
* │  类    名: ShooterStateMachine.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Utils.log;

namespace Module.ShootingPlayer.HFSM
{
    // 射击角色的层级状态机，独立于千咲状态机
    public sealed class ShooterStateMachine
    {
        public bool IsInited { get; private set; }

        public bool IsFaulted { get; private set; }

        private readonly Dictionary<ShooterStateId, BaseShooterState> m_states;
        private readonly List<ShooterStateId> m_activeStatePath;
        private readonly ReadOnlyCollection<ShooterStateId> m_readOnlyActiveStatePath;

        private bool m_isRegistryLocked;
        private bool m_isExecutingLifecycle;

        // 当前叶子状态
        public ShooterStateId CurrentLeafStateId =>
            m_activeStatePath.Count > 0 ? m_activeStatePath[^1] : ShooterStateId.None;

        // 当前完整活动路径
        public IReadOnlyList<ShooterStateId> ActiveStatePath => m_readOnlyActiveStatePath;

        public ShooterStateMachine()
        {
            m_states = new Dictionary<ShooterStateId, BaseShooterState>();
            m_activeStatePath = new List<ShooterStateId>();
            m_readOnlyActiveStatePath = m_activeStatePath.AsReadOnly();
        }

        // 注册状态
        public void RegisterState(BaseShooterState state)
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

            if (state.Id == ShooterStateId.None)
            {
                QLog.Error("注册状态失败：ShooterStateId.None 只能作为顶层状态的父节点");
                return;
            }

            if (m_states.ContainsKey(state.Id))
            {
                QLog.Error($"注册状态失败：状态 ID 重复注册：{state.Id}");
                return;
            }

            m_states.Add(state.Id, state);
        }

        // 初始化状态机并进入初始状态
        public void Init(ShooterStateId initStateId)
        {
            if (m_isRegistryLocked)
            {
                QLog.Error("初始化失败：状态机不能重复初始化");
                return;
            }

            if (initStateId == ShooterStateId.None)
            {
                QLog.Error("初始化失败：初始状态不能是 ShooterStateId.None");
                return;
            }

            if (!ValidateStateTree())
                return;

            List<ShooterStateId> initialPath = BuildExpandedPath(initStateId);

            if (initialPath.Count == 0)
                return;

            m_isRegistryLocked = true;
            m_isExecutingLifecycle = true;

            try
            {
                EnterInitialPath(initialPath);
                IsInited = true;
            }
            catch (System.Exception exception)
            {
                IsFaulted = true;
                QLog.Throw(exception);
            }
            finally
            {
                m_isExecutingLifecycle = false;
            }
        }

        // 切换到目标状态，复合状态会自动展开到默认叶子状态
        public void ChangeState(ShooterStateId targetStateId, bool allowReentry = false)
        {
            if (!IsStateMachineValid(nameof(ChangeState)))
                return;

            List<ShooterStateId> targetPath = BuildExpandedPath(targetStateId);

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
            catch (System.Exception exception)
            {
                IsFaulted = true;
                QLog.Throw(exception);
            }
            finally
            {
                m_isExecutingLifecycle = false;
            }
        }

        // 按父状态到叶子状态的顺序执行帧更新
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
            catch (System.Exception exception)
            {
                IsFaulted = true;
                QLog.Throw(exception);
            }
            finally
            {
                m_isExecutingLifecycle = false;
            }
        }

        // 按父状态到叶子状态的顺序执行物理帧更新
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
            catch (System.Exception exception)
            {
                IsFaulted = true;
                QLog.Throw(exception);
            }
            finally
            {
                m_isExecutingLifecycle = false;
            }
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

            foreach (BaseShooterState state in m_states.Values)
            {
                if (!ValidateParent(state))
                    return false;
            }

            foreach (BaseShooterState state in m_states.Values)
            {
                if (!ValidateParentChain(state))
                    return false;
            }

            foreach (BaseShooterState state in m_states.Values)
            {
                if (state is ShooterCompositeState compositeState && !ValidateInitialChild(compositeState))
                    return false;
            }

            return true;
        }

        // 校验状态声明的父节点存在且为复合状态
        private bool ValidateParent(BaseShooterState state)
        {
            if (state.ParentId == ShooterStateId.None)
                return true;

            if (!m_states.TryGetValue(state.ParentId, out BaseShooterState parentState))
            {
                QLog.Error($"状态树校验失败：状态 {state.Id} 的父状态不存在：{state.ParentId}");
                return false;
            }

            if (!(parentState is ShooterCompositeState))
            {
                QLog.Error($"状态树校验失败：状态 {state.Id} 的父状态不是复合状态：{state.ParentId}");
                return false;
            }

            return true;
        }

        // 校验状态的父链不存在环路
        private bool ValidateParentChain(BaseShooterState state)
        {
            HashSet<ShooterStateId> visitedStateIds = new HashSet<ShooterStateId>();

            BaseShooterState currentState = state;

            while (currentState.ParentId != ShooterStateId.None)
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
        private bool ValidateInitialChild(ShooterCompositeState compositeState)
        {
            ShooterStateId initialChildId = compositeState.GetInitialChildId();

            if (initialChildId == ShooterStateId.None)
            {
                QLog.Error($"状态树校验失败：复合状态 {compositeState.Id} 未指定默认子状态");
                return false;
            }

            if (!m_states.TryGetValue(initialChildId, out BaseShooterState initialChildState))
            {
                QLog.Error($"状态树校验失败：复合状态 {compositeState.Id} 的默认子状态未注册：{initialChildId}");
                return false;
            }

            if (initialChildState.ParentId != compositeState.Id)
            {
                QLog.Error($"状态树校验失败：状态 {initialChildId} 不是复合状态 {compositeState.Id} 的直接子状态");
                return false;
            }

            return true;
        }

        // 构建从顶层状态到目标叶子状态的完整路径
        private List<ShooterStateId> BuildExpandedPath(ShooterStateId targetStateId)
        {
            if (!m_states.TryGetValue(targetStateId, out BaseShooterState targetState))
            {
                QLog.Error($"构建状态路径失败：目标状态未注册：{targetStateId}");
                return new List<ShooterStateId>();
            }

            List<ShooterStateId> targetPath = new List<ShooterStateId>();

            BaseShooterState currentState = targetState;

            while (true)
            {
                targetPath.Add(currentState.Id);

                if (currentState.ParentId == ShooterStateId.None)
                    break;

                if (!m_states.TryGetValue(currentState.ParentId, out currentState))
                    return new List<ShooterStateId>();
            }

            targetPath.Reverse();

            while (m_states.TryGetValue(targetPath[^1], out BaseShooterState leafState) &&
                   leafState is ShooterCompositeState compositeState)
            {
                targetPath.Add(compositeState.GetInitialChildId());
            }

            return targetPath;
        }

        // 按父状态到叶子状态的顺序进入初始路径
        private void EnterInitialPath(IReadOnlyList<ShooterStateId> initialPath)
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
                QLog.Throw(new System.InvalidOperationException(
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
        private bool IsSameActivePath(IReadOnlyList<ShooterStateId> targetPath)
        {
            if (m_activeStatePath.Count != targetPath.Count)
                return false;

            for (int i = 0; i < m_activeStatePath.Count; i++)
            {
                if (m_activeStatePath[i] != targetPath[i])
                    return false;
            }

            return true;
        }

        // 计算当前活动路径与目标路径的公共前缀长度
        private int GetCommonPrefixLength(IReadOnlyList<ShooterStateId> targetPath)
        {
            int maxLength = m_activeStatePath.Count < targetPath.Count ? m_activeStatePath.Count : targetPath.Count;

            for (int i = 0; i < maxLength; i++)
            {
                if (m_activeStatePath[i] != targetPath[i])
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
        private void EnterTargetPath(IReadOnlyList<ShooterStateId> targetPath, int commonPrefixLength)
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
