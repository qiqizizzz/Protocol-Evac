/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包面板视图模型                      
 * │  类    名: InventoryViewModel.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Framework.QTower.MVVM.View;
using Framework.QTower.MVVM.ViewModel;
using Framework.QTower.MVVM.ViewModel.Interfaces;
using Module.Inventory.Core.Config;
using Module.Inventory.Core.Config.Type;
using Module.Inventory.Core.Defines;
using Module.Inventory.Core.Rule.Extensions;
using Module.Inventory.Core.Rule.Type;
using UnityEngine;

namespace UI.Inventory
{
    public class InventoryViewModel : ViewModelBase
    {
        private const int NO_SELECTION = -1; //未选中任何物品的标记号

        public IReadOnlyReactiveProperty<InventoryContainerVO> Snapshot => m_snapshot;
        public IReadOnlyReactiveProperty<int> SelectedInstanceId => m_selectedInstanceId;
        public IReadOnlyReactiveProperty<InventoryHintKind> HintKind => m_hintKind;
        public IReadOnlyReactiveProperty<bool> CanRotate => m_canRotate;
        public ISignal HintPulse => m_hintPulse;
        public InventoryMoveResult LastMoveResult => m_lastMoveResult;
        public int ContainerId => m_containerId;
        
        private readonly IInventoryModel m_model;
        private readonly int m_containerId;
        public int SlotCount => m_slots.Count;
        
        private readonly ReactiveProperty<InventoryContainerVO> m_snapshot;
        private readonly ReactiveProperty<int> m_selectedInstanceId;
        private readonly ReactiveProperty<InventoryHintKind> m_hintKind;
        private readonly ReactiveProperty<bool> m_canRotate;
        private readonly Signal m_hintPulse;

        private readonly List<InventorySlotViewModel>  m_slots = new List<InventorySlotViewModel>();
        private readonly List<ReactiveProperty<bool>> m_slotHighlights = new List<ReactiveProperty<bool>>();
        
        private int m_slotWidth = -1;
        private InventoryMoveResult m_lastMoveResult;
        private RectInt m_lastRegion;
        private bool m_hasLastRegion;

        public InventoryViewModel() : this(MvvmRoot.Instance.Models.Get<IInventoryModel>())
        {
            
        }

        public InventoryViewModel(IInventoryModel model, int containerId = InventoryContainerIds.BACKPACK)
        {
            m_model = model;
            m_containerId = containerId;

            m_snapshot = CreateProperty(InventoryContainerVO.Empty);
            m_selectedInstanceId = CreateProperty(NO_SELECTION);
            m_hintKind = CreateProperty(InventoryHintKind.Idle);
            m_canRotate = CreateProperty(false);
            m_hintPulse = CreateSignal();
        }

        #region 生命周期
        protected override void OnBindCore()
        {
            m_model.OnChanged += HandleModelChanged;
            RefreshFromModel();
        }

        protected override void OnUnbindCore()
        {
            m_model.OnChanged -= HandleModelChanged;
        }

        protected override void OnDisposeCore()
        {
            for (int i = 0; i < m_slots.Count; i++)
            {
                m_slots[i].Dispose();
            }
            
            m_slots.Clear();
            m_slotHighlights.Clear();
            m_hasLastRegion = false;
        }
        #endregion

        #region 上行方法
        public InventorySlotViewModel GetSlot(int index) => m_slots[index];

        //交互格子
        public void InteractAt(Vector2Int cell)
        {
            if (m_model.TryGetInstanceIdAt(m_containerId, cell, out int instanceId))
            {
                ToggleItemSelection(instanceId);
                return;
            }
            
            //空格子静默
            if(m_selectedInstanceId.Value == NO_SELECTION) return;
            
            PlaceSelectedItem(cell);
        }
        
        //旋转选中物品
        public void RotateSelectedItem()
        {
            if(m_selectedInstanceId.Value == NO_SELECTION) return;
            
            m_lastMoveResult = m_model.TryRotateItem(m_containerId, m_selectedInstanceId.Value);
            SetHint(ToHintKind(m_lastMoveResult));
        }
        
        //取消当前选中
        public void ClearSelection()
        {
            ResetSelection(InventoryHintKind.Idle);
        }
        #endregion

        #region 内部方法
        private void HandleModelChanged(int containerId)
        {
            if(containerId != m_containerId) return;
            
            RefreshFromModel();
        }
        
        //按容器拓扑增减格子子 VM
        private void SyncSlots(InventoryContainerVO vo)
        {
            //宽度改变, 全部重建
            if (vo.Width != m_slotWidth)
            {
                for (int i = 0; i < m_slots.Count; i++)
                {
                    m_slots[i].Dispose();
                }
                
                m_slots.Clear();
                m_slotHighlights.Clear();
                m_hasLastRegion = false;
                m_slotWidth = vo.Width;
            }

            //格子不够, 向后补齐(初始化或者背包高度变高)
            int cellCount = vo.CellCount;
            while (m_slots.Count < cellCount)
            {
                int index = m_slots.Count;
                Vector2Int cell = new Vector2Int(index % m_slotWidth, index / m_slotWidth);
                ReactiveProperty<bool> highlight = CreateProperty(false);
                
                m_slotHighlights.Add(highlight);
                m_slots.Add(new InventorySlotViewModel(cell, highlight.AsReadOnly(), InteractAt));
            }

            //格子多了, 多余的裁剪(大背包变成小背包)
            while (m_slots.Count > cellCount)
            {
                int last = m_slots.Count - 1;
                m_slots[last].Dispose();
                m_slots.RemoveAt(last);
                m_slotHighlights.RemoveAt(last);
            }
        }
        
        //从模型拉取最新快照
        private void RefreshFromModel()
        {
            InventoryContainerVO vo = m_model.GetContainerVO(m_containerId);
            SyncSlots(vo);
            m_snapshot.Set(vo);
            
            //选中物品已被外部挪走或销毁则丢弃选中态
            if(m_selectedInstanceId.Value != NO_SELECTION && !vo.Contains(m_selectedInstanceId.Value))
            {
                ResetSelection(InventoryHintKind.Idle);
                return;
            }
            
            RefreshHighlight();
            RefreshCanRotate();
        }

        //刷新格子高亮状态
        private void RefreshHighlight()
        {
            if(m_hasLastRegion)
                SetRegionHighlight(m_lastRegion, false);

            m_hasLastRegion = false;

            int selected = m_selectedInstanceId.Value;
            if (selected == NO_SELECTION || !m_snapshot.Value.TryGetItem(selected, out InventoryItemVO item)) return;

            m_lastRegion = new RectInt(item.Origin, item.Size);
            m_hasLastRegion = true;
            SetRegionHighlight(m_lastRegion, true);
        }
        
        //刷新格子旋转状态
        private void RefreshCanRotate()
        {
            int selected = m_selectedInstanceId.Value;

            if (selected == NO_SELECTION || !m_snapshot.Value.TryGetItem(selected, out InventoryItemVO item))
            {
                m_canRotate.TrySet(false);
                return;
            }

            bool canRotate = ItemRegistry.TryGet(item.ItemId, out InventoryItemConfigSO config) && config.CanRotate;
            m_canRotate.TrySet(canRotate);
        }

        //设置区域高亮状态
        private void SetRegionHighlight(RectInt region, bool isHighlighted)
        {
            int height = m_snapshot.Value.Height;

            for (int y = region.yMin; y < region.yMax; y++)
            {
                if (y < 0 || y >= height) continue;

                for (int x = region.xMin; x < region.xMax; x++)
                {
                    if (x < 0 || x >= m_slotWidth) continue;

                    int index = y * m_slotWidth + x;
                    if (index >= m_slotHighlights.Count) continue;

                    m_slotHighlights[index].TrySet(isHighlighted);
                }
            }
        }

        private void ToggleItemSelection(int instanceId)
        {
            if (m_selectedInstanceId.Value == instanceId)
            {
                ResetSelection(InventoryHintKind.Idle);
                return;
            }

            m_selectedInstanceId.TrySet(instanceId);
            RefreshHighlight();
            RefreshCanRotate();
            SetHint(InventoryHintKind.Selected);
        }

        //放置选中物品
        private void PlaceSelectedItem(Vector2Int cell)
        {
            int instanceId = m_selectedInstanceId.Value;
            InventoryRotation rotation = InventoryRotation.None;

            if (m_snapshot.Value.TryGetItem(instanceId, out InventoryItemVO item))
                rotation = item.Rotation;
            
            m_lastMoveResult = m_model.TryMoveItem(m_containerId, instanceId, cell, rotation);
            
            if(!m_lastMoveResult.IsSuccessOrSame())
            {
                SetHint(ToHintKind(m_lastMoveResult));
                return;
            }
            
            ResetSelection(InventoryHintKind.Placed);
        }

        //重置选中态, 清理相关交互表现
        private void ResetSelection(InventoryHintKind hint)
        {
            m_selectedInstanceId.TrySet(NO_SELECTION);
            RefreshHighlight();
            RefreshCanRotate();
            SetHint(hint);
        }
        
        private InventoryHintKind ToHintKind(InventoryMoveResult result)
        {
            if(result.IsSuccessOrSame()) return InventoryHintKind.Selected;
            if(result.IsDataDesync()) return InventoryHintKind.Desync;
            
            return InventoryHintKind.Failed;
        }

        //设置提示信息
        private void SetHint(InventoryHintKind kind)
        {
            m_hintKind.Set(kind);
            m_hintPulse.Fire();
        }
        #endregion

    }
}