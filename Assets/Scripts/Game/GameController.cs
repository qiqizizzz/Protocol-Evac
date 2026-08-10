/*
 * ┌──────────────────────────────────┐
 * │  描    述: 游戏场景控制器，负责启动游戏场景默认模块与界面
 * │  类    名: GameController.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.QTower;
using Framework.QTower.Controller;
using UI.Combat;

namespace Game
{
    public sealed class GameController : BaseController
    {
        protected override void OnInit()
        {
            if (!GameApp.UIManager.Register<UICombatHUD>(ViewType.UICombatHUD, this))
                return;

            GameApp.UIManager.OpenAsync(ViewType.UICombatHUD, null);
        }

        protected override void OnDestroy()
        {
            GameApp.UIManager.UnregisterByController(this);
        }
    }
}
