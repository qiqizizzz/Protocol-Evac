/*
 * ┌──────────────────────────────────┐
 * │  描    述: 游戏应用组合根，负责装配并驱动全局模块
 * │  类    名: GameApp.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Common;
using Framework.QTower;
using Framework.QTower.Common;
using Framework.QTower.Controller;
using Module.Timer;

namespace Game
{
    public sealed class GameApp : Singleton<GameApp>
    {
        public TimeManager TimeManager { get; private set; }
        public ControllerManager ControllerManager { get; private set; }
        public UIManager UIManager { get; private set; }

        protected override void OnInit()
        {
            TimeManager = new TimeManager();
            ControllerManager = new ControllerManager();
        }

        protected override void OnTick(float deltaTime)
        {
            TimeManager.Tick(deltaTime);
            ControllerManager.Tick(deltaTime);
        }

        protected override void OnDestroy()
        {
            UIManager.Destroy();
            ControllerManager.Destroy();
            TimeManager.Destroy();

            UIManager = null;
            ControllerManager = null;
            TimeManager = null;
        }
    }
}
