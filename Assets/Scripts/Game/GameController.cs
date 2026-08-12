/*
 * ┌──────────────────────────────────┐
 * │  描    述: 游戏场景控制器，负责启动游戏场景默认模块与界面
 * │  类    名: GameController.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Cysharp.Threading.Tasks;
using Framework.QTower;
using Framework.QTower.Common.Defines;
using Framework.QTower.Controller;
using UI.Combat;

namespace Game
{
    public sealed class GameController : BaseController
    {

        // 注册游戏场景默认 HUD
        public GameController()
        {
            GameApp.UIManager.Register<UICombatHUD>(new UIData
            {
                Address = UIDefines.UICombatHUD,
                Parent = GameApp.UIManager.UIRoot,
                Controller = this,
                SortingOrder = 0
            });
        }

        protected override void OnInit()
        {
            GameApp.UIManager.Open<UICombatHUD>().Forget();
        }

        protected override void OnDestroy()
        {
            GameApp.UIManager.UnregisterByController(this);
        }
    }
}
