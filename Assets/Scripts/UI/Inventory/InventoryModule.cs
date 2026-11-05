/*
* ┌──────────────────────────────────┐
* │  描    述: 背包模块装配处, 按总配置登记物品表与容器并注册战备面板视图工厂
* │  类    名: InventoryModule.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common.Res;
using Framework.QTower.Common.Defines;
using Framework.QTower.MVVM.View;
using Module.Inventory.Core.Config;
using Module.Inventory.Core.Data;
using Module.Inventory.Core.Defines;
using Module.Inventory.Core.Rule.Extensions;
using Module.Inventory.Core.Rule.Type;
using UI.Inventory.View;
using Utils.log;

namespace UI.Inventory
{
    [MvvmModule]
    public static class InventoryModule
    {
        public const string CONFIG_ADDRESS = "Config/Inventory/Config";

        //装配背包模块: 注册模型契约与载体视图工厂, 由 MvvmHost 反射调用
        public static void Register(MvvmRoot root)
        {
            InventoryConfigSO config = ResManager.LoadAsset<InventoryConfigSO>(CONFIG_ADDRESS);
            if (config == null)
            {
                QLog.Error($"背包总配置未加载：{CONFIG_ADDRESS}");
                return;
            }

            IInventoryModel model = root.Models.Register<IInventoryModel>(new InventoryModel(config));
            config.RegisterItems();
            ApplyLoadout(model, config.Loadout);
            SeedDemoItems(model);

            root.Windows.RegisterAsync<UIEquipmentPanel>(
                parent => ResManager.InstantiateAsync(UIDefines.UIEquipmentPanel, parent));
        }

        /// <summary>
        /// 按穿戴表注册角色身上的容器: 口袋与安全箱是先天容量, 背包与胸挂要穿进装备槽才撑起容量
        /// </summary>
        /// <param name="model">背包模型</param>
        /// <param name="loadout">穿戴表, 取不到时角色身上不会注册任何容器</param>
        public static void ApplyLoadout(IInventoryModel model, InventoryLoadoutConfigSO loadout)
        {
            if (loadout == null)
            {
                QLog.Error("穿戴表为空, 角色身上不会注册任何容器");
                return;
            }

            ApplyInnateContainer(model, InventoryContainerIds.POCKETS, loadout);
            ApplyInnateContainer(model, InventoryContainerIds.SAFEBOX, loadout);

            EquipFromLoadout(model, InventoryContainerIds.BACKPACK_SLOT, loadout.Backpack, "背包");
            EquipFromLoadout(model, InventoryContainerIds.CHESTRIG_SLOT, loadout.Rig, "胸挂");
        }

        /// <summary>
        /// 用一份容器定义注册单个槽位
        /// </summary>
        /// <param name="model">背包模型</param>
        /// <param name="slotId">角色容器槽位号</param>
        /// <param name="container">该槽位要撑起的容器定义</param>
        public static void ApplyContainer(IInventoryModel model, int slotId, InventoryContainerConfigSO container)
        {
            if (container == null)
            {
                QLog.Error($"容器定义为空, 槽位注册失败：{slotId}");
                return;
            }

            //负重上限归角色整体负重系统统一结算, 单容器这一层不设限
            model.RegisterContainer(slotId,
                new InventoryContainerSpec(container.Width, container.Height, float.MaxValue, container.Mask,
                    container.DisplayName));
        }

        //先天容量直接按穿戴表里的容器定义注册, 不经过装备槽
        private static void ApplyInnateContainer(IInventoryModel model, int slotId, InventoryLoadoutConfigSO loadout)
        {
            if (!loadout.TryGetContainer(slotId, out InventoryContainerConfigSO container))
            {
                QLog.Error($"槽位未配置容器定义：{slotId}");
                return;
            }

            ApplyContainer(model, slotId, container);
        }

        //把穿戴表里的容器装备塞进对应装备槽, 模型内部顺势撑起它的容量容器
        private static void EquipFromLoadout(IInventoryModel model, int slotId, InventoryItemConfigSO equipment,
            string label)
        {
            if (equipment == null) return;

            if (!equipment.IsContainer)
            {
                QLog.Error($"{label}穿戴的物品不是容器装备：{equipment.DisplayName}");
                return;
            }

            InventoryMoveResult result = model.TryAddItemAuto(slotId, equipment.ItemId, 1);
            if (!result.IsSuccessOrSame()) QLog.Error($"{label}穿戴失败：{result}");
        }

        //演示种子, 掉落系统接上后移除
        private static void SeedDemoItems(IInventoryModel model)
        {
            int backpack = InventoryContainerIds.BACKPACK;
            if (!model.IsContainerRegistered(backpack)) return;

            model.TryAddItemAuto(backpack, 6100, 1);
            model.TryAddItemAuto(backpack, 1000, 1);
            model.TryAddItemAuto(backpack, 1002, 1);
            model.TryAddItemAuto(backpack, 1003, 1);
            model.TryAddItemAuto(backpack, 3001, 1);
        }
    }
}