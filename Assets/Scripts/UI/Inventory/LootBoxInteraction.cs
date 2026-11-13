/*
* ┌──────────────────────────────────┐
* │  描    述: 搜刮交互桥接, F 打开物资箱, H 打开地面垃圾堆, 容器统一挂到战备面板右侧
* │  类    名: LootBoxInteraction.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Framework.QTower.Common.Defines;
using Framework.QTower.Event;
using Framework.QTower.MVVM.View;
using Module.Inventory.Interaction;
using Module.Inventory.Presentation;
using UI.Inventory.View;
using UnityEngine;
using Utils.log;

namespace UI.Inventory
{
    /// <summary>
    /// 搜刮交互桥接：按目标和按键语义接通背包模型与战备面板, 世界层不认识 UI
    /// 物资箱要现摇一次掉落, 地面堆本来就是容器, 直接用它身上的容器号
    /// </summary>
    public sealed class LootBoxInteraction
    {
        private const float GROUND_PILE_SCAN_RADIUS = 8f;
        private const string NO_LOOT_BOX_HINT = "前方没有可搜刮的物资箱";

        private readonly LootInteractableScanner m_scanner = new LootInteractableScanner();
        private readonly System.Func<bool> m_isContainerPanelVisible;

        private bool m_hasValidatedBoxes;

        public LootBoxInteraction(System.Func<bool> isContainerPanelVisible)
        {
            m_isContainerPanelVisible = isContainerPanelVisible;
        }

        /// <summary>
        /// 响应通用交互, 从距离、视角与视线都满足条件的物资箱中打开一只
        /// </summary>
        /// <param name="origin">交互原点, 一般是玩家胸口</param>
        /// <param name="forward">相机水平朝向</param>
        /// <param name="config">物资箱交互查询参数</param>
        /// <param name="obstacleMask">墙体遮挡检测层</param>
        /// <returns>是否已接受本次打开请求</returns>
        public bool TryInteract(Vector3 origin, Vector3 forward, InteractionQueryConfig config, LayerMask obstacleMask)
        {
            if (m_isContainerPanelVisible()) return false;

            ValidateSceneBoxes();

            ILootInteractable target = m_scanner.ScanBest<LootBoxController>(origin, forward, config, obstacleMask);
            if (target == null)
            {
                QLog.Info($"附近没有物资箱, 原点 {origin}");
                EventManager.PublishEvent(EventDefines.InteractionHintRequested, NO_LOOT_BOX_HINT);
                return false;
            }

            return OpenInteractable(target);
        }

        /// <summary>
        /// 与 origin 附近最近的地面垃圾堆交互, 打开装备面板并展示它的容器
        /// </summary>
        /// <param name="origin">交互原点, 一般是玩家位置</param>
        /// <returns>是否已接受本次打开请求</returns>
        public bool TryInteractGroundPile(Vector3 origin)
        {
            ILootInteractable target = m_scanner.ScanNearest<GroundPileView>(origin, GROUND_PILE_SCAN_RADIUS);
            if (target == null) return false;

            return OpenInteractable(target);
        }

        // 把已选中的世界目标接到模型和战备面板
        private bool OpenInteractable(ILootInteractable target)
        {
            MvvmRoot root = MvvmRoot.Instance;
            if (root == null)
            {
                QLog.Error($"场景缺少 {nameof(MvvmRoot)}, 搜刮目标无法打开");
                return false;
            }

            IInventoryModel model = root.Models.Get<IInventoryModel>();
            int containerId = ResolveContainerId(model, target);
            if (containerId < 0) return false;

            OpenPanelAndShow(root, containerId, target is LootBoxController).Forget();
            return true;
        }

        // 把世界目标换成一只可打开的容器号, 认不出的目标直接报错, 不静默退回
        private int ResolveContainerId(IInventoryModel model, ILootInteractable target)
        {
            if (target is LootBoxController box)
                return model.OpenLootContainer(box.EntityId, box.LootConfig, box.ResolveSeed());

            if (target is GroundPileView pile) return pile.ContainerId;

            QLog.Error($"未接入的搜刮目标类型：{target.GetType().Name}");
            return -1;
        }

        // 面板已显示就直接换箱, 否则先开面板再换箱, 避免两次 Show 抢同一份视图模型
        private async UniTaskVoid OpenPanelAndShow(MvvmRoot root, int containerId, bool requiresSearch)
        {
            if (root.Windows.TryGet(out UIEquipmentPanel panel) && panel.IsVisible)
            {
                panel.ViewModel.OpenLootContainer(containerId, requiresSearch);
                return;
            }

            UIEquipmentPanel shown = await root.Windows.ShowAsync<UIEquipmentPanel>();
            if (shown == null) return;

            shown.ViewModel.OpenLootContainer(containerId, requiresSearch);
        }

        // 复制实例忘改实体号会让两只箱子共用一份残货, 场景加载后只查一次
        private void ValidateSceneBoxes()
        {
            if (m_hasValidatedBoxes) return;
            m_hasValidatedBoxes = true;

            LootBoxController[] boxes = Object.FindObjectsOfType<LootBoxController>();
            if (boxes.Length == 0)
            {
                QLog.Warning("场景内没有任何物资箱, 按 F 不会有效果");
                return;
            }

            HashSet<string> entityIds = new HashSet<string>();
            for (int i = 0; i < boxes.Length; i++)
            {
                string entityId = boxes[i].EntityId;
                if (string.IsNullOrEmpty(entityId)) continue;
                if (entityIds.Add(entityId)) continue;

                QLog.Error($"物资箱实体号重复：{entityId}, 两只箱子会串同一份残货");
            }
        }
    }
}
