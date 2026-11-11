/*
* ┌──────────────────────────────────┐
* │  描    述: 丢弃交互链路, 把背包模型的原子丢弃结果登记成地面物品并补上世界落点
* │  类    名: GroundDropInteraction.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Framework.QTower.MVVM.Model.Interfaces;
using Framework.QTower.MVVM.View;
using Module.Inventory.Core.Rule.Extensions;
using Module.Inventory.Core.Rule.Type;
using UnityEngine;
using Utils.log;

namespace UI.Inventory
{
    /// <summary>
    /// 丢弃交互：右键菜单与左键双击共用这一条链路, 世界落点只在这里参与, 背包模型不认识坐标
    /// </summary>
    public sealed class GroundDropInteraction
    {
        private readonly IInventoryModel m_inventoryModel;
        private readonly IGroundItemModel m_groundModel;

        public GroundDropInteraction() : this(ResolveModel<IInventoryModel>(), ResolveModel<IGroundItemModel>())
        {
        }

        public GroundDropInteraction(IInventoryModel inventoryModel, IGroundItemModel groundModel)
        {
            m_inventoryModel = inventoryModel;
            m_groundModel = groundModel;
        }

        /// <summary>
        /// 把一件物品连堆带朝向丢到指定世界坐标, 先过模型层的原子丢弃, 成功才登记地面物品
        /// </summary>
        /// <param name="containerId">物品当前所在容器号</param>
        /// <param name="instanceId">要丢弃的物品实例号</param>
        /// <param name="dropOrigin">落点世界坐标, 一般是玩家所在位置</param>
        /// <returns>是否丢弃成功</returns>
        public bool TryDrop(int containerId, int instanceId, Vector3 dropOrigin)
        {
            if (m_inventoryModel == null || m_groundModel == null)
            {
                QLog.Error("丢弃链路缺少背包模型或地面模型, 丢弃未执行");
                return false;
            }

            InventoryMoveResult result = m_inventoryModel.TryDropItem(containerId, instanceId,
                out int groundContainerId);
            if (!result.IsSuccessOrSame())
            {
                QLog.Warning($"物品丢弃被拒：{result}, 容器 {containerId}, 实例 {instanceId}");
                return false;
            }

            // 地面容器是新建的, 里面只该有刚丢进去的这一件
            InventoryContainerVO ground = m_inventoryModel.GetContainerVO(groundContainerId);
            if (ground.Items.Count == 0)
            {
                QLog.Error($"丢弃成功但地面容器 {groundContainerId} 里没有物品, 无法登记地面物品");
                return false;
            }

            m_groundModel.Register(groundContainerId, ground.Items[0], dropOrigin);
            return true;
        }

        // 从 MVVM 模型表取契约, 取不到由 Get 自己报错, 这里只补宿主缺失的情况
        private static TModel ResolveModel<TModel>() where TModel : class, IModel
        {
            MvvmRoot root = MvvmRoot.Instance;
            if (root == null)
            {
                QLog.Error($"场景缺少 {nameof(MvvmRoot)}, 无法取得模型 {typeof(TModel).Name}");
                return null;
            }

            return root.Models.Get<TModel>();
        }
    }
}
