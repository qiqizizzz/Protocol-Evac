/*
 * ┌──────────────────────────────────┐
 * │  描    述: 计时器系统，负责对外提供全局计时任务调度
 * │  类    名: TimerSystem.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using Framework.QF;

namespace Module.Timer
{
    public class TimerSystem : AbstractSystem
    {
        private GameTimer m_gameTimer;

        public int Count => m_gameTimer.Count;

        // 初始化计时器系统
        protected override void OnInit()
        {
            m_gameTimer = new GameTimer();
        }

        // 注册计时任务
        public int Register(float duration, Action callback, bool isLooping = false)
        {
            return m_gameTimer.Register(duration, callback, isLooping);
        }

        // 取消指定计时任务
        public void Cancel(int timerId)
        {
            m_gameTimer.Cancel(timerId);
        }

        // 清空全部计时任务
        public void ClearAll()
        {
            m_gameTimer.ClearAll();
        }

        // 推进计时器系统
        public void Tick(float deltaTime)
        {
            m_gameTimer.Tick(deltaTime);
        }
    }
}
