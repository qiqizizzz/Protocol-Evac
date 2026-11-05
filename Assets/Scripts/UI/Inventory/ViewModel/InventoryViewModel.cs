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
using Module.Inventory.Core.Defines;
using Module.Inventory.Core.Rule.Extensions;
using Module.Inventory.Core.Rule.Type;
using UnityEngine;

namespace UI.Inventory
{
    public class InventoryViewModel : ViewModelBase
    {
        private const int NO_SELECTION = -1; //未选中任何物品的标记号
        private const int NO_DRAG = -1; //本面板当前没有物品在被拖拽

        public IReadOnlyReactiveProperty<InventoryContainerVO> Snapshot => m_snapshot;
        public IReadOnlyReactiveProperty<int> SelectedInstanceId => m_selectedInstanceId;
        public IReadOnlyReactiveProperty<InventoryHintKind> HintKind => m_hintKind;
        public IReadOnlyReactiveProperty<bool> CanRotate => m_canRotate;
        public IReadOnlyReactiveProperty<bool> Visible => m_visible;
        public ISignal HintPulse => m_hintPulse;
        public InventoryMoveResult LastMoveResult => m_lastMoveResult;
        public int DraggingInstanceId => m_dragInstanceId;
        public int ContainerId => m_containerId;
        public bool IsDragging => m_dragInstanceId != NO_DRAG;
        
        private readonly IInventoryModel m_model;
        private readonly int m_containerId;
        private readonly EquipmentPanelViewModel m_owner;
        private int m_dragInstanceId = NO_DRAG; //本面板当前被拖起的物品
        public int SlotCount => m_slots.Count;
        
        private readonly ReactiveProperty<InventoryContainerVO> m_snapshot;
        private readonly ReactiveProperty<int> m_selectedInstanceId;
        private readonly ReactiveProperty<InventoryHintKind> m_hintKind;
        private readonly ReactiveProperty<bool> m_canRotate;
        private readonly ReactiveProperty<bool> m_visible;
        private readonly Signal m_hintPulse;

        private readonly List<InventorySlotViewModel>  m_slots = new List<InventorySlotViewModel>();
        private readonly List<ReactiveProperty<InventoryCellMark>> m_slotMarks = new List<ReactiveProperty<InventoryCellMark>>();
        private readonly List<ReactiveProperty<bool>> m_slotSolids = new List<ReactiveProperty<bool>>();
        
        private int m_slotWidth = -1;
        private InventoryMoveResult m_lastMoveResult;
        private RectInt m_selectedRegion; //已着色的选中区
        private bool m_hasSelectedRegion;
        private RectInt m_previewRegion; //已着色的落点预览区
        private bool m_hasPreviewRegion;
        private bool m_hasDropCandidate; //手上是否有待预览落点, 与已着色缓存分开记保证幂等
        private Vector2Int m_dropCandidateCell;

        public InventoryViewModel() : this(MvvmRoot.Instance.Models.Get<IInventoryModel>())
        {
            
        }

        public InventoryViewModel(IInventoryModel model, int containerId = InventoryContainerIds.BACKPACK,
            EquipmentPanelViewModel owner = null)
        {
            m_model = model;
            m_containerId = containerId;
            m_owner = owner;

            m_snapshot = CreateProperty(InventoryContainerVO.Empty);
            m_selectedInstanceId = CreateProperty(NO_SELECTION);
            m_hintKind = CreateProperty(InventoryHintKind.Idle);
            m_canRotate = CreateProperty(false);
            m_visible = CreateProperty(false);
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
            m_slotMarks.Clear();
            m_slotSolids.Clear();
            ResetPreviewCaches();
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
        
        //从格子上拖起物品
        public void BeginDragAt(Vector2Int cell)
        {
            if (!m_model.TryGetInstanceIdAt(m_containerId, cell, out int instanceId)) return;

            m_dragInstanceId = instanceId;
            m_owner?.NotifyDragBegan(this);
        }

        //把拖起的物品放到指定格子上
        public void PlaceDraggedItemAt(Vector2Int cell)
        {
            ResetDropPreview();

            if (m_dragInstanceId != NO_DRAG)
            {
                ApplyTransferResult(m_model.TryTransferItem(m_containerId, m_containerId, m_dragInstanceId, cell));
                return;
            }

            m_owner?.HandleCrossContainerDrop(this, cell);
        }

        //取消拖拽
        public void CancelDrag()
        {
            ResetDropPreview();
            if (m_dragInstanceId == NO_DRAG) return;

            m_dragInstanceId = NO_DRAG;
            m_owner?.NotifyDragEnded(this);
        }

        //应用一次转移结果
        public void ApplyTransferResult(InventoryMoveResult result)
        {
            ResetDropPreview();
            m_dragInstanceId = NO_DRAG;
            m_lastMoveResult = result;
            SetHint(result.IsSuccessOrSame() ? InventoryHintKind.Placed : ToHintKind(result));
            m_owner?.NotifyDragEnded(this);
        }

        //取消当前选中
        public void ClearSelection()
        {
            ResetSelection(InventoryHintKind.Idle);
        }

        //悬停落点预览, 重复上报同一格幂等
        public void PreviewDropAt(Vector2Int cell)
        {
            if (!CanPreviewDrop) return;

            m_dropCandidateCell = cell;
            m_hasDropCandidate = true;
            RefreshCellMarks();
        }

        //指针离开预览区域后清除预览
        public void ClearDropPreview(Vector2Int cell)
        {
            if (!m_hasDropCandidate) return;
            if (!m_previewRegion.Contains(cell)) return;

            m_hasDropCandidate = false;
            RefreshCellMarks();
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
                m_slotMarks.Clear();
                m_slotSolids.Clear();
                ResetPreviewCaches();
                m_slotWidth = vo.Width;
            }

            //格子不够, 向后补齐(初始化或者背包高度变高)
            int slotCount = vo.SlotCount;
            while (m_slots.Count < slotCount)
            {
                int index = m_slots.Count;
                Vector2Int cell = new Vector2Int(index % m_slotWidth, index / m_slotWidth);
                ReactiveProperty<InventoryCellMark> mark = CreateProperty(InventoryCellMark.None);
                m_slotMarks.Add(mark);

                ReactiveProperty<bool> solid = CreateProperty(vo.Mask[index]);
                m_slotSolids.Add(solid);

                m_slots.Add(new InventorySlotViewModel(cell, mark.AsReadOnly(), solid.AsReadOnly(), InteractAt,
                    PlaceDraggedItemAt, NotifySlotHovered));
            }

            //格子多了, 多余的裁剪(大背包变成小背包)
            while (m_slots.Count > slotCount)
            {
                int last = m_slots.Count - 1;
                m_slots[last].Dispose();
                m_slots.RemoveAt(last);
                m_slotMarks.RemoveAt(last);
                m_slotSolids.RemoveAt(last);
            }

            //行列没变只换了形状时上面两条分支都不会命中, 这里统一刷一遍
            SyncSlotSolids(vo);
        }

        //按快照掩码刷新每格实心状态
        private void SyncSlotSolids(InventoryContainerVO vo)
        {
            for (int i = 0; i < m_slotSolids.Count; i++)
                m_slotSolids[i].TrySet(vo.Mask[i]);
        }
        
        //从模型拉取最新快照
        private void RefreshFromModel()
        {
            //容器没注册就不该让载体刷一张空网格, 更不该去模型里讨报错
            bool registered = m_model.IsContainerRegistered(m_containerId);
            m_visible.TrySet(registered);
            InventoryContainerVO vo = registered ? m_model.GetContainerVO(m_containerId) : InventoryContainerVO.Empty;
            SyncSlots(vo);
            m_snapshot.Set(vo);
            
            //选中物品已被外部挪走或销毁则丢弃选中态
            if(m_selectedInstanceId.Value != NO_SELECTION && !vo.Contains(m_selectedInstanceId.Value))
            {
                ResetSelection(InventoryHintKind.Idle);
                return;
            }
            
            RefreshCellMarks();
            RefreshCanRotate();
        }

        private bool CanPreviewDrop => IsDragging || m_selectedInstanceId.Value != NO_SELECTION ||
                                       m_owner?.DragSource != null;

        private void ResetDropPreview()
        {
            if (!m_hasDropCandidate && !m_hasPreviewRegion) return;

            m_hasDropCandidate = false;
            RefreshCellMarks();
        }

        private void ResetPreviewCaches()
        {
            m_hasSelectedRegion = false;
            m_hasPreviewRegion = false;
            m_hasDropCandidate = false;
        }

        private void RefreshCellMarks()
        {
            if (m_hasSelectedRegion) SetRegionMark(m_selectedRegion, InventoryCellMark.None);
            if (m_hasPreviewRegion) SetRegionMark(m_previewRegion, InventoryCellMark.None);

            m_hasSelectedRegion = false;
            m_hasPreviewRegion = false;

            int selected = m_selectedInstanceId.Value;
            if (selected != NO_SELECTION && m_snapshot.Value.TryGetItem(selected, out InventoryItemVO item))
            {
                m_selectedRegion = new RectInt(item.Origin, item.Size);
                m_hasSelectedRegion = true;
                SetRegionMark(m_selectedRegion, InventoryCellMark.Selected);
            }

            if (!m_hasDropCandidate) return;

            //预览盖在选中之上, 同一格只会有一个颜色
            if (!TryEvaluateDropAt(m_dropCandidateCell, out Vector2Int size, out InventoryCellMark mark))
            {
                m_hasDropCandidate = false;
                return;
            }

            m_previewRegion = new RectInt(m_dropCandidateCell, size);
            m_hasPreviewRegion = true;
            SetRegionMark(m_previewRegion, mark);
        }

        private bool TryEvaluateDropAt(Vector2Int cell, out Vector2Int size, out InventoryCellMark mark)
        {
            size = Vector2Int.one;
            mark = InventoryCellMark.None;

            if (!TryResolveDropCandidate(out int sourceContainerId, out int instanceId, out InventoryItemVO item))
                return false;

            InventoryMoveResult verdict = m_model.PeekTransferItem(sourceContainerId, m_containerId, instanceId, cell,
                out bool willSwap);

            if (verdict == InventoryMoveResult.None || verdict == InventoryMoveResult.SameSpot) return false;

            size = item.Size;
            mark = ToCellMark(verdict, willSwap);
            return true;
        }

        private bool TryResolveDropCandidate(out int sourceContainerId, out int instanceId, out InventoryItemVO item)
        {
            InventoryViewModel source = m_owner?.DragSource ?? this;

            instanceId = source.DraggingInstanceId;
            if (instanceId == NO_DRAG) instanceId = source.SelectedInstanceId.Value;

            if (instanceId == NO_SELECTION)
            {
                sourceContainerId = NO_SELECTION;
                item = null;
                return false;
            }

            sourceContainerId = source.ContainerId;
            return source.Snapshot.Value.TryGetItem(instanceId, out item);
        }

        private static InventoryCellMark ToCellMark(InventoryMoveResult verdict, bool willSwap)
        {
            if (!verdict.IsSuccessOrSame()) return InventoryCellMark.DropInvalid;

            return willSwap ? InventoryCellMark.DropSwap : InventoryCellMark.DropValid;
        }

        private void NotifySlotHovered(Vector2Int cell, bool isEnter)
        {
            if (isEnter)
                PreviewDropAt(cell);
            else
                ClearDropPreview(cell);
        }
        
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

        private void SetRegionMark(RectInt region, InventoryCellMark mark)
        {
            int height = m_snapshot.Value.Height;

            for (int y = region.yMin; y < region.yMax; y++)
            {
                if (y < 0 || y >= height) continue;

                for (int x = region.xMin; x < region.xMax; x++)
                {
                    if (x < 0 || x >= m_slotWidth) continue;

                    int index = y * m_slotWidth + x;
                    if (index >= m_slotMarks.Count) continue;

                    m_slotMarks[index].TrySet(mark);
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
            RefreshCellMarks();
            RefreshCanRotate();
            SetHint(InventoryHintKind.Selected);
        }

        //放置选中物品
        private void PlaceSelectedItem(Vector2Int cell)
        {
            int instanceId = m_selectedInstanceId.Value;
            m_lastMoveResult = m_model.TryTransferItem(m_containerId, m_containerId, instanceId, cell);
            
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
            RefreshCellMarks();
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