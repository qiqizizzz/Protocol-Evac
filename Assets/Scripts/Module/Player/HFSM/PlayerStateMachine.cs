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
        public PlayerStateId CurrentLeafStateId => m_activeStatePath.Count > 0
            ? m_activeStatePath[m_activeStatePath.Count - 1]
            : PlayerStateId.None;

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
                QLog.Throw(new System.ArgumentNullException(nameof(state)));

            if(m_isRegistryLocked)
                QLog.Throw(new System.InvalidOperationException("状态机初始化后不能继续注册状态"));
            
            if(state.Id == PlayerStateId.None)
                QLog.Throw(new System.ArgumentException(
                    "PlayerStateId.None只能作为顶层状态的父节点！", nameof(state)));
            
            if(m_states.ContainsKey(state.Id))
                QLog.Throw(new System.ArgumentException($"状态ID重复注册：{state.Id}", nameof(state)));
            
            m_states.Add(state.Id, state);
        }
        
        //初始化
        public void Init(PlayerStateId initStateId)
        {
            if(m_isRegistryLocked)
                QLog.Throw(new System.InvalidOperationException("状态机不能重复初始化"));
            
            if(initStateId == PlayerStateId.None)
                QLog.Throw(new System.ArgumentException(
                    "初始状态不能是 PlayerStateId.None", nameof(initStateId)));

            validateStateTree();

            List<PlayerStateId> initialPath =
                buildExpandedPath(initStateId);

            m_isRegistryLocked = true;
            m_isExecutingLifecycle = true;
            
            try
            {
                enterInitialPath(initialPath);
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

        #region 检验状态树
        // 校验状态树的父子关系、环路与默认子状态
        private void validateStateTree()
        {
            if (m_states.Count == 0)
                QLog.Throw(new System.InvalidOperationException("状态机至少需要注册一个状态"));

            foreach (BasePlayerState state in m_states.Values)
                validateParent(state);

            foreach (BasePlayerState state in m_states.Values)
                validateParentChain(state);

            foreach (BasePlayerState state in m_states.Values)
            {
                if (state is PlayerCompositeState compositeState)
                    validateInitialChild(compositeState);
            }
        }

        // 校验状态声明的父节点存在且为复合状态
        private void validateParent(BasePlayerState state)
        {
            if (state.ParentId == PlayerStateId.None)
                return;

            if (!m_states.TryGetValue(
                    state.ParentId,
                    out BasePlayerState parentState))
            {
                QLog.Throw(new System.InvalidOperationException(
                    $"状态 {state.Id} 的父状态不存在：{state.ParentId}"));
            }

            if (!(parentState is PlayerCompositeState))
            {
                QLog.Throw(new System.InvalidOperationException(
                    $"状态 {state.Id} 的父状态不是复合状态：{state.ParentId}"));
            }
        }

        // 校验状态的父链不存在环路
        private void validateParentChain(BasePlayerState state)
        {
            HashSet<PlayerStateId> visitedStateIds =
                new HashSet<PlayerStateId>();

            BasePlayerState currentState = state;

            while (currentState.ParentId != PlayerStateId.None)
            {
                if (!visitedStateIds.Add(currentState.Id))
                {
                    QLog.Throw(new System.InvalidOperationException(
                        $"状态树存在父子环路：{currentState.Id}"));
                }

                currentState = m_states[currentState.ParentId];
            }
        }

        // 校验复合状态的默认状态是已注册的直接子状态
        private void validateInitialChild(
            PlayerCompositeState compositeState)
        {
            PlayerStateId initialChildId =
                compositeState.GetInitialChildId();

            if (initialChildId == PlayerStateId.None)
            {
                QLog.Throw(new System.InvalidOperationException(
                    $"复合状态 {compositeState.Id} 未指定默认子状态"));
            }

            if (!m_states.TryGetValue(
                    initialChildId,
                    out BasePlayerState initialChildState))
            {
                QLog.Throw(new System.InvalidOperationException(
                    $"复合状态 {compositeState.Id} 的默认子状态未注册：" +
                    $"{initialChildId}"));
            }

            if (initialChildState.ParentId != compositeState.Id)
            {
                QLog.Throw(new System.InvalidOperationException(
                    $"状态 {initialChildId} 不是复合状态 " +
                    $"{compositeState.Id} 的直接子状态"));
            }
        }

        // 构建从顶层状态到目标叶子状态的完整路径
        private List<PlayerStateId> buildExpandedPath(
            PlayerStateId targetStateId)
        {
            if (!m_states.TryGetValue(
                    targetStateId,
                    out BasePlayerState targetState))
            {
                QLog.Throw(new System.ArgumentException(
                    $"目标状态未注册：{targetStateId}", nameof(targetStateId)));
            }

            List<PlayerStateId> targetPath =
                new List<PlayerStateId>();

            BasePlayerState currentState = targetState;

            while (true)
            {
                targetPath.Add(currentState.Id);

                if (currentState.ParentId == PlayerStateId.None)
                    break;

                currentState = m_states[currentState.ParentId];
            }

            targetPath.Reverse();

            while (m_states[targetPath[targetPath.Count - 1]]
                   is PlayerCompositeState compositeState)
            {
                targetPath.Add(
                    compositeState.GetInitialChildId());
            }

            return targetPath;
        }

        // 按父状态到叶子状态的顺序进入初始路径
        private void enterInitialPath(
            IReadOnlyList<PlayerStateId> initialPath)
        {
            for (int i = 0; i < initialPath.Count; i++)
            {
                PlayerStateId stateId = initialPath[i];

                m_activeStatePath.Add(stateId);
                m_states[stateId].Enter();
            }
        }
        #endregion

    }
}
