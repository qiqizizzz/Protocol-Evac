/*
 * ┌──────────────────────────────────────────────────────────────┐
 * │  描    述: 玩家层级状态机，负责状态树校验、活动路径维护与状态切换
 * │  类    名: PlayerStateMachine.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Utils.log;

namespace Module.Player.HFSM
{
    //sealed:禁止被继承
    public sealed class PlayerStateMachine
    {
        public bool IsInited { get; private set; }
        
        public bool IsFaulted { get; private set; } //是否有故障

        private readonly Dictionary<PlayerStateId, BasePlayerState> m_states;
        private readonly List<PlayerStateId> m_activeStatePath;//内部可读可写：状态链条
        private readonly ReadOnlyCollection<PlayerStateId> m_readOnlyActiveStatePath;//已激活的状态链条

        private bool m_isRegistryLocked;
        private bool m_isExecutingLifecycle;

        //当前叶子节点
        public PlayerStateId CurrentLeafStateId =>
            m_activeStatePath.Count > 0 ? m_activeStatePath[^1] : PlayerStateId.None;

        public IReadOnlyList<PlayerStateId> ActiveStatePath => m_readOnlyActiveStatePath;

        public PlayerStateMachine()
        {
            m_states = new Dictionary<PlayerStateId, BasePlayerState>();
            m_activeStatePath = new List<PlayerStateId>();
            m_readOnlyActiveStatePath = m_activeStatePath.AsReadOnly();
        }
        
        //注册状态
        public void RegisterState(BasePlayerState state)
        {
            if (state == null)
            {
                QLog.Error("注册状态失败：state 为空");
                return;
            }

            if(m_isRegistryLocked)
            {
                QLog.Error("注册状态失败：状态机初始化后不能继续注册状态");
                return;
            }
            
            if(state.Id == PlayerStateId.None)
            {
                QLog.Error("注册状态失败：PlayerStateId.None 只能作为顶层状态的父节点");
                return;
            }
            
            if(m_states.ContainsKey(state.Id))
            {
                QLog.Error($"注册状态失败：状态 ID 重复注册：{state.Id}");
                return;
            }
            
            m_states.Add(state.Id, state);
        }
        
        //初始化
        public void Init(PlayerStateId initStateId)
        {
            if(m_isRegistryLocked)
            {
                QLog.Error("初始化失败：状态机不能重复初始化");
                return;
            }
            
            if(initStateId == PlayerStateId.None)
            {
                QLog.Error("初始化失败：初始状态不能是 PlayerStateId.None");
                return;
            }

            if (!ValidateStateTree())
                return;

            List<PlayerStateId> initialPath =
                BuildExpandedPath(initStateId);

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
        public void ChangeState(PlayerStateId targetStateId, bool allowReentry = false)
        {
            if (!IsStateMachineValid(nameof(ChangeState)))
                return;

            List<PlayerStateId> targetPath = BuildExpandedPath(targetStateId);

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
                {
                    PlayerStateId stateId = m_activeStatePath[i];
                    m_states[stateId].Tick(deltaTime);
                }
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
                {
                    PlayerStateId stateId = m_activeStatePath[i];
                    m_states[stateId].FixedTick(fixedDeltaTime);
                }
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

            foreach (BasePlayerState state in m_states.Values)
            {
                if (!ValidateParent(state))
                    return false;
            }

            foreach (BasePlayerState state in m_states.Values)
            {
                if (!ValidateParentChain(state))
                    return false;
            }

            foreach (BasePlayerState state in m_states.Values)
            {
                if (state is PlayerCompositeState compositeState && !ValidateInitialChild(compositeState))
                    return false;
            }

            return true;
        }

        // 校验状态声明的父节点存在且为复合状态
        private bool ValidateParent(BasePlayerState state)
        {
            if (state.ParentId == PlayerStateId.None)
                return true;

            if (!m_states.TryGetValue(state.ParentId, out BasePlayerState parentState))
            {
                QLog.Error($"状态树校验失败：状态 {state.Id} 的父状态不存在：{state.ParentId}");
                return false;
            }

            if (!(parentState is PlayerCompositeState))
            {
                QLog.Error($"状态树校验失败：状态 {state.Id} 的父状态不是复合状态：{state.ParentId}");
                return false;
            }

            return true;
        }

        // 校验状态的父链不存在环路
        private bool ValidateParentChain(BasePlayerState state)
        {
            HashSet<PlayerStateId> visitedStateIds = new HashSet<PlayerStateId>();

            BasePlayerState currentState = state;

            while (currentState.ParentId != PlayerStateId.None)
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

        // 校验复合状态的默认状态是已注册的直接子状态
        private bool ValidateInitialChild(PlayerCompositeState compositeState)
        {
            PlayerStateId initialChildId = compositeState.GetInitialChildId();

            if (initialChildId == PlayerStateId.None)
            {
                QLog.Error($"状态树校验失败：复合状态 {compositeState.Id} 未指定默认子状态");
                return false;
            }

            if (!m_states.TryGetValue(initialChildId, out BasePlayerState initialChildState))
            {
                QLog.Error($"状态树校验失败：复合状态 {compositeState.Id} 的默认子状态未注册：{initialChildId}");
                return false;
            }

            if (initialChildState != null && initialChildState.ParentId != compositeState.Id)
            {
                QLog.Error($"状态树校验失败：状态 {initialChildId} 不是复合状态 {compositeState.Id} 的直接子状态");
                return false;
            }

            return true;
        }

        // 构建从顶层状态到目标叶子状态的完整路径
        private List<PlayerStateId> BuildExpandedPath(PlayerStateId targetStateId)
        {
            if (!m_states.TryGetValue(targetStateId, out BasePlayerState targetState))
            {
                QLog.Error($"构建状态路径失败：目标状态未注册：{targetStateId}");
                return new List<PlayerStateId>();
            }

            List<PlayerStateId> targetPath = new List<PlayerStateId>();

            BasePlayerState currentState = targetState;

            while (true)
            {
                targetPath.Add(currentState.Id);

                if (currentState.ParentId == PlayerStateId.None)
                    break;

                if (!m_states.TryGetValue(currentState.ParentId, out currentState))
                    return new List<PlayerStateId>();
            }

            targetPath.Reverse();

            while (m_states.TryGetValue(targetPath[^1], out BasePlayerState leafState) && leafState is PlayerCompositeState compositeState)
            {
                targetPath.Add(compositeState.GetInitialChildId());
            }

            return targetPath;
        }

        // 按父状态到叶子状态的顺序进入初始路径
        private void EnterInitialPath(IReadOnlyList<PlayerStateId> initialPath)
        {
            for (int i = 0; i < initialPath.Count; i++)
            {
                PlayerStateId stateId = initialPath[i];

                m_activeStatePath.Add(stateId);
                m_states[stateId].Enter();
            }
        }
        #endregion

        #region 检验状态
        // 校验状态机是否正常执行
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

        //判断目标路径是否与当前活动路径完全一致,避免切换到重复state
        private bool IsSameActivePath(IReadOnlyList<PlayerStateId> targetPath)
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

        // 计算当前活动路径与目标路径的公共前缀长度, 用于判断需要退出和进入的状态
        private int GetCommonPrefixLength(IReadOnlyList<PlayerStateId> targetPath)
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
                PlayerStateId stateId = m_activeStatePath[i];

                m_states[stateId].Exit();
                m_activeStatePath.RemoveAt(i);
            }
        }

        // 从公共前缀之后进入目标路径到叶子状态
        private void EnterTargetPath(IReadOnlyList<PlayerStateId> targetPath, int commonPrefixLength)
        {
            for (int i = commonPrefixLength; i < targetPath.Count; i++)
            {
                PlayerStateId stateId = targetPath[i];

                m_activeStatePath.Add(stateId);
                m_states[stateId].Enter();
            }
        }
        #endregion
    }
}
