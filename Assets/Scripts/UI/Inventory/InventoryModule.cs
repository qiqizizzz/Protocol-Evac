/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包模块装配处, 注册模型与载体视图工厂
 * │  类    名: InventoryModule.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Common.Res;
using Framework.QTower.Common.Defines;
using Framework.QTower.MVVM.View;
using Module.Inventory.Core.Defines;
using UI.Inventory.View;

namespace UI.Inventory
{
    [MvvmModule]
    public static class InventoryModule
    {
        private const int BACKPACK_WIDTH = 8;
        private const int BACKPACK_HEIGHT = 5;

        //装配背包模块: 注册模型契约与载体视图工厂, 由 MvvmHost 反射调用
        public static void Register(MvvmRoot root)
        {
            IInventoryModel model = root.Models.Register<IInventoryModel>(new InventoryModel());
            model.RegisterContainer(InventoryContainerIds.BACKPACK, BACKPACK_WIDTH, BACKPACK_HEIGHT, float.MaxValue);

            root.Windows.RegisterAsync<UIInventoryCarrier>(
                parent => ResManager.InstantiateAsync(UIDefines.UIInventoryCarrier, parent));
        }
    }
}
