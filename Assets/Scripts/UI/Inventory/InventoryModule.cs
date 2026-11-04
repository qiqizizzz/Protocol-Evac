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
using Module.Inventory.Core.Data;
using Module.Inventory.Core.Defines;
using UI.Inventory.View;

namespace UI.Inventory
{
    [MvvmModule]
    public static class InventoryModule
    {

        //装配背包模块: 注册模型契约与载体视图工厂, 由 MvvmHost 反射调用
        public static void Register(MvvmRoot root)
        {
            IInventoryModel model = root.Models.Register<IInventoryModel>(new InventoryModel());
            //临时占位规格: Step 2 起改由当前穿戴的背包装备配置提供
            model.RegisterContainer(InventoryContainerIds.BACKPACK, new InventoryContainerSpec(8, 5, float.MaxValue));

            root.Windows.RegisterAsync<UIInventoryCarrier>(
                parent => ResManager.InstantiateAsync(UIDefines.UIInventoryCarrier, parent));
        }
    }
}
