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

            root.Windows.RegisterAsync<UIEquipmentPanel>(
                parent => ResManager.InstantiateAsync(UIDefines.UIEquipmentPanel, parent));
        }

        /// <summary>
        /// 按穿戴表把角色身上的每个容器槽位注册进模型
        /// </summary>
        /// <param name="model">背包模型</param>
        /// <param name="loadout">穿戴表, 取不到时整批槽位都不会被注册</param>
        public static void ApplyLoadout(IInventoryModel model, InventoryLoadoutConfigSO loadout)
        {
            if (loadout == null)
            {
                QLog.Error("穿戴表为空, 角色身上不会注册任何容器");
                return;
            }

            int[] slotIds = InventoryContainerIds.ROLE_SLOT_IDS;
            for (int i = 0; i < slotIds.Length; i++)
            {
                if (!loadout.TryGetContainer(slotIds[i], out InventoryContainerConfigSO container))
                {
                    //脱下装备后空槽位会变成合法状态, 那时再把这条报错降级成跳过
                    QLog.Error($"槽位未配置容器定义：{slotIds[i]}");
                    continue;
                }

                ApplyContainer(model, slotIds[i], container);
            }
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
    }
}
