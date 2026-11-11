/*
* ┌──────────────────────────────────┐
* │  描    述: 按 F 的交互桥接, 扫描最近物资箱后开模型容器并把面板拉到右侧
* │  类    名: LootBoxInteraction.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Framework.QTower.MVVM.View;
using Module.Inventory.Interaction;
using UI.Inventory.View;
using UnityEngine;
using Utils.log;

namespace UI.Inventory
{
    /// <summary>
    /// 物资箱交互桥接：把世界侧的扫描结果接到背包模型与战备面板, 世界层不认识 UI
    /// </summary>
    public sealed class LootBoxInteraction
    {
        private readonly LootInteractableScanner m_scanner = new LootInteractableScanner();

        private bool m_hasValidatedBoxes;

        /// <summary>
        /// 与 origin 附近最近的物资箱交互, 打开装备面板并把该箱容器挂到右侧动态载体
        /// </summary>
        /// <param name="origin">交互原点, 一般是玩家位置</param>
        public void TryInteract(Vector3 origin)
        {
            ValidateSceneBoxes();

            LootBoxController box = m_scanner.ScanNearest(origin);
            if (box == null)
            {
                QLog.Info($"附近没有可交互的物资箱, 原点 {origin}");
                return;
            }

            MvvmRoot root = MvvmRoot.Instance;
            if (root == null)
            {
                QLog.Error($"场景缺少 {nameof(MvvmRoot)}, 物资箱无法打开");
                return;
            }

            IInventoryModel model = root.Models.Get<IInventoryModel>();
            int containerId = model.OpenLootContainer(box.EntityId, box.LootConfig, box.ResolveSeed());
            if (containerId < 0) return;

            OpenPanelAndShow(root, containerId).Forget();
        }

        // 面板已显示就直接换箱, 否则先开面板再换箱, 避免两次 Show 抢同一份视图模型
        private async UniTaskVoid OpenPanelAndShow(MvvmRoot root, int containerId)
        {
            if (root.Windows.TryGet(out UIEquipmentPanel panel) && panel.IsVisible)
            {
                panel.ViewModel.OpenLootContainer(containerId);
                return;
            }

            UIEquipmentPanel shown = await root.Windows.ShowAsync<UIEquipmentPanel>();
            if (shown == null) return;

            shown.ViewModel.OpenLootContainer(containerId);
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
