/*
* ┌──────────────────────────────────┐
* │  描    述: 丢弃交互链路, 把背包模型的原子丢弃结果收进地面垃圾堆并补上世界落点
* │  类    名: GroundDropInteraction.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Framework.QTower.MVVM.Model.Interfaces;
using Framework.QTower.MVVM.View;
using Module.Inventory.Core.Data;
using Module.Inventory.Core.Rule.Extensions;
using Module.Inventory.Core.Rule.Type;
using UnityEngine;
using Utils.log;

namespace UI.Inventory
{
    /// <summary>
    /// 丢弃交互：右键菜单与左键双击共用这一条链路, 世界落点只在这里参与, 背包模型不认识坐标
    /// 落点附近有堆就先塞进堆, 塞不下按阶梯撑大, 撑到顶或附近没堆才新起一堆
    /// </summary>
    public sealed class GroundDropInteraction
    {
        // 丢弃时往落点周围找堆收拢的半径(米), 只并脚边的堆, 免得整张图合成一座
        private const float PILE_MERGE_RADIUS = 1.5f;

        // 堆容器从这座起算, 之后按阶梯往上撑
        private static readonly Vector2Int[] PILE_LADDER =
        {
            new Vector2Int(2, 2),
            new Vector2Int(3, 3),
            new Vector2Int(4, 3),
            new Vector2Int(4, 4)
        };

        // 地面堆的显示名与载重, 与模型侧新建的丢弃容器同一口径
        private const string PILE_DISPLAY_NAME = "地面";
        private const float PILE_WEIGHT_LIMIT = float.MaxValue;

        private readonly IInventoryModel m_inventoryModel;
        private readonly IGroundPileModel m_pileModel;

        public GroundDropInteraction() : this(ResolveModel<IInventoryModel>(), ResolveModel<IGroundPileModel>())
        {
        }

        public GroundDropInteraction(IInventoryModel inventoryModel, IGroundPileModel pileModel)
        {
            m_inventoryModel = inventoryModel;
            m_pileModel = pileModel;
        }

        /// <summary>
        /// 把一件物品连堆带朝向丢到指定世界坐标: 先过模型层的原子丢弃, 成功才落到地面堆上
        /// </summary>
        /// <param name="containerId">物品当前所在容器号</param>
        /// <param name="instanceId">要丢弃的物品实例号</param>
        /// <param name="dropOrigin">落点世界坐标, 一般是玩家所在位置</param>
        /// <returns>是否丢弃成功</returns>
        public bool TryDrop(int containerId, int instanceId, Vector3 dropOrigin)
        {
            if (m_inventoryModel == null || m_pileModel == null)
            {
                QLog.Error("丢弃链路缺少背包模型或地面堆模型, 丢弃未执行");
                return false;
            }

            InventoryMoveResult result = m_inventoryModel.TryDropItem(containerId, instanceId,
                out int dropContainerId);
            if (!result.IsSuccessOrSame())
            {
                QLog.Warning($"物品丢弃被拒：{result}, 容器 {containerId}, 实例 {instanceId}");
                return false;
            }

            // 模型侧只会新建一只刚好装下这件物品的临时容器, 收拢与起堆都由这里决定
            if (TryAbsorbIntoNearestPile(dropContainerId, instanceId, dropOrigin)) return true;

            return PromoteToPile(dropContainerId, dropOrigin);
        }

        // 落点附近有堆就把它撑到塞得下为止, 成功收拢后收回临时容器
        private bool TryAbsorbIntoNearestPile(int dropContainerId, int instanceId, Vector3 dropOrigin)
        {
            if (!m_pileModel.TryFindNearest(dropOrigin, PILE_MERGE_RADIUS, out GroundPileVO pile)) return false;
            if (!m_inventoryModel.IsContainerRegistered(pile.ContainerId))
            {
                QLog.Error($"地面堆 {pile.PileId} 指向的容器 {pile.ContainerId} 已不在册, 丢弃改走新起一堆");
                return false;
            }

            InventoryContainerVO target = m_inventoryModel.GetContainerVO(pile.ContainerId);
            for (int i = 0; i < PILE_LADDER.Length; i++)
            {
                // 比现有堆更小的档位没意义, 直接跳到当前档位往后试
                if (PILE_LADDER[i].x < target.Width || PILE_LADDER[i].y < target.Height) continue;

                if (!m_inventoryModel.TryResizeContainer(pile.ContainerId, BuildPileSpec(PILE_LADDER[i]))) continue;

                InventoryMoveResult absorbed = m_inventoryModel.TryTransferItemAuto(dropContainerId,
                    pile.ContainerId, instanceId);
                if (!absorbed.IsSuccessOrSame()) continue;

                m_inventoryModel.UnregisterContainer(dropContainerId);
                return true;
            }

            return false;
        }

        // 把模型刚建的临时容器原地升级成一座新堆, 容器号不变, 只把网格撑到起堆尺寸
        private bool PromoteToPile(int dropContainerId, Vector3 dropOrigin)
        {
            // 临时容器已经带着刚丢的那件物品, 比它小的档位撑不下, 试了只会白报一次放不下
            InventoryContainerVO current = m_inventoryModel.GetContainerVO(dropContainerId);
            for (int i = 0; i < PILE_LADDER.Length; i++)
            {
                if (PILE_LADDER[i].x < current.Width || PILE_LADDER[i].y < current.Height) continue;

                if (!m_inventoryModel.TryResizeContainer(dropContainerId, BuildPileSpec(PILE_LADDER[i]))) continue;

                if (m_pileModel.CreatePile(dropContainerId, dropOrigin) != null) return true;

                QLog.Error($"地面堆登记失败, 收回容器 {dropContainerId}");
                m_inventoryModel.UnregisterContainer(dropContainerId);
                return false;
            }

            // 阶梯到顶也撑不下这件超大物品: 就按临时容器原本的轮廓原地起一堆
            // 这里绝不能收回容器, 物品已经从源箱拿走, 收回去就是凭空消失
            if (m_pileModel.CreatePile(dropContainerId, dropOrigin) == null)
            {
                QLog.Error($"地面堆登记失败, 收回容器 {dropContainerId}");
                m_inventoryModel.UnregisterContainer(dropContainerId);
                return false;
            }

            InventoryContainerVO oversized = m_inventoryModel.GetContainerVO(dropContainerId);
            QLog.Warning($"超出地面堆最大档位, 按原尺寸起堆：{oversized.Width}x{oversized.Height}, " +
                         $"容器 {dropContainerId}");
            return true;
        }

        // 地面堆规格: 实心矩形, 载重不设限, 名字沿用模型侧的地面容器
        private static InventoryContainerSpec BuildPileSpec(Vector2Int size)
        {
            return new InventoryContainerSpec(size.x, size.y, PILE_WEIGHT_LIMIT, null, PILE_DISPLAY_NAME);
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
