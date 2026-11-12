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
        private const int NO_DRAG = -1; //本面板当前没有物品在被拖拽
        private const int ROTATION_COUNT = 4; //四次旋转构成一周

        public IReadOnlyReactiveProperty<InventoryContainerVO> Snapshot => m_snapshot;
        public IReadOnlyReactiveProperty<int> SelectedInstanceId => m_selectedInstanceId;
        // 左键查看详情的目标, 与选中状态分开记录
        public IReadOnlyReactiveProperty<int> DetailInstanceId => m_detailInstanceId;
        public IReadOnlyReactiveProperty<InventoryHintKind> HintKind => m_hintKind;
        public IReadOnlyReactiveProperty<bool> CanRotate => m_canRotate;
        public IReadOnlyReactiveProperty<bool> Visible => m_visible;
        public ISignal HintPulse => m_hintPulse;
        public ISignal DetailRequested => m_detailRequested;
        public ISignal MenuRequested => m_menuRequested;
        public ISignal PopupClosed => m_popupClosed;
        public InventoryMoveResult LastMoveResult => m_lastMoveResult;
        public int DraggingInstanceId => m_dragInstanceId;
        // 拖拽中使用的朝向, 与快照里的实际朝向分开记录
        public InventoryRotation DragRotation => m_dragRotation;
        public int ContainerId => m_containerId;
        public bool IsDragging => m_dragInstanceId != NO_DRAG;
        // 是否为开箱载体(地面垃圾堆 / 物资箱), 这类容器里的物品双击是收进背包而不是丢回地面
        public bool IsLootContainer => m_owner != null && ReferenceEquals(m_owner.LootContainer, this);
        
        private readonly IInventoryModel m_model;
        private readonly int m_containerId;
        private readonly EquipmentPanelViewModel m_owner;
        private int m_dragInstanceId = NO_DRAG; //本面板当前被拖起的物品
        private InventoryRotation m_dragRotation; //拖拽中暂定的落点朝向
        public int SlotCount => m_slots.Count;
        
        private readonly ReactiveProperty<InventoryContainerVO> m_snapshot;
        private readonly ReactiveProperty<int> m_selectedInstanceId;
        private readonly ReactiveProperty<int> m_detailInstanceId;
        private readonly ReactiveProperty<InventoryHintKind> m_hintKind;
        private readonly ReactiveProperty<bool> m_canRotate;
        private readonly ReactiveProperty<bool> m_visible;
        private readonly Signal m_hintPulse;
        private readonly Signal m_detailRequested;
        private readonly Signal m_menuRequested;
        private readonly Signal m_popupClosed;

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
            m_detailInstanceId = CreateProperty(NO_SELECTION);
            m_hintKind = CreateProperty(InventoryHintKind.Idle);
            m_canRotate = CreateProperty(false);
            m_visible = CreateProperty(false);
            m_hintPulse = CreateSignal();
            m_detailRequested = CreateSignal();
            m_menuRequested = CreateSignal();
            m_popupClosed = CreateSignal();
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

        // 左键交互：仅打开详情, 不改变选中态, 不放置物品
        public void InteractAt(Vector2Int cell)
        {
            if (m_model.TryGetInstanceIdAt(m_containerId, cell, out int instanceId))
            {
                m_detailInstanceId.TrySet(instanceId);
                m_detailRequested.Fire();
                return;
            }

            // 空格子：按点击空白处理, 隐藏浮层并清除选中高亮
            if (m_selectedInstanceId.Value != NO_SELECTION) ResetSelection(InventoryHintKind.Idle);
            else m_popupClosed.Fire();
        }
        
        /// <summary>
        /// 右键请求物品菜单：将该物品置为选中, 不执行反选切换
        /// </summary>
        /// <param name="cell">被右键点击的格子</param>
        public void OpenMenuAt(Vector2Int cell)
        {
            if (!m_model.TryGetInstanceIdAt(m_containerId, cell, out int instanceId)) return;

            if (m_selectedInstanceId.Value != instanceId)
                SelectItem(instanceId);

            m_menuRequested.Fire();
        }

        // 收起本容器的详情与菜单: 物品被丢弃后浮层里的数据已经作废
        public void ClosePopups() => m_popupClosed.Fire();

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
            m_dragRotation = m_snapshot.Value.TryGetItem(instanceId, out InventoryItemVO item)
                ? item.Rotation
                : InventoryRotation.None;
            m_popupClosed.Fire();
            m_owner?.NotifyDragBegan(this);
        }

        //把拖起的物品放到指定格子上
        public void PlaceDraggedItemAt(Vector2Int cell)
        {
            ResetDropPreview();

            if (m_dragInstanceId != NO_DRAG)
            {
                ApplyTransferResult(m_model.TryTransferItem(m_containerId, m_containerId, m_dragInstanceId, cell,
                    m_dragRotation));
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
            m_dragRotation = InventoryRotation.None;
            m_owner?.NotifyDragEnded(this);
        }

        // 拖拽中推进朝向: 只改本面板的拖拽状态, 落点可行性交给下一次预判
        public bool RotateDraggedItem()
        {
            if (m_dragInstanceId == NO_DRAG) return false;

            // 装备槽内的整件装备不参与旋转
            if (InventoryContainerIds.TryGetEquipSlotRole(m_containerId, out _)) return false;
            if (!m_snapshot.Value.TryGetItem(m_dragInstanceId, out InventoryItemVO item)) return false;
            if (!ItemRegistry.TryGet(item.ItemId, out InventoryItemConfigSO config) || !config.CanRotate) return false;

            m_dragRotation = (InventoryRotation)(((int)m_dragRotation + 1) % ROTATION_COUNT);
            return true;
        }

        //应用一次转移结果
        public void ApplyTransferResult(InventoryMoveResult result)
        {
            if (IsDisposed) return;

            ResetDropPreview();
            m_dragInstanceId = NO_DRAG;
            m_dragRotation = InventoryRotation.None;
            m_lastMoveResult = result;

            // 放置成功清除选中态, 失败保留高亮以便调整朝向后重试
            if (result.IsSuccessOrSame()) ResetSelection(InventoryHintKind.Placed);
            else SetHint(ToHintKind(result));

            m_owner?.NotifyDragEnded(this);
        }

        // 双击开箱载体里的物品: 收进玩家自己的容量容器, 塞不下只给一次失败提示, 绝不落回地面
        public bool TryQuickMoveToOwnContainer(int instanceId)
        {
            if (m_owner == null) return false;

            InventoryMoveResult result = m_owner.TryStowLootItem(m_containerId, instanceId);
            ApplyTransferResult(result);
            return result.IsSuccessOrSame();
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

        // 仅拖拽状态下提供落点预览, 选中本身不构成放置意图
        private bool CanPreviewDrop => IsDragging || m_owner?.DragSource != null;

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

            if (!TryResolveDropCandidate(out int sourceContainerId, out int instanceId, out InventoryItemVO item,
                    out InventoryRotation preferredRotation))
                return false;

            InventoryMoveResult verdict = m_model.PeekTransferItem(sourceContainerId, m_containerId, instanceId, cell,
                preferredRotation, out bool willSwap, out bool willMerge, out InventoryRotation rotation);

            if (verdict == InventoryMoveResult.None || verdict == InventoryMoveResult.SameSpot) return false;

            size = ResolvePreviewSize(item, rotation);
            mark = ToCellMark(verdict, willSwap, willMerge);
            return true;
        }

        //预览外形按落点实际采用的朝向取值, 拖出装备槽时不再沿用单格足迹
        private Vector2Int ResolvePreviewSize(InventoryItemVO item, InventoryRotation rotation)
        {
            // 装备槽恒为单格, 其余容器按朝向取配置外形
            if (InventoryContainerIds.TryGetEquipSlotRole(m_containerId, out _)) return Vector2Int.one;

            if (ItemRegistry.TryGet(item.ItemId, out InventoryItemConfigSO config)) return config.GetSizeAt(rotation);

            return item.Size;
        }

        private bool TryResolveDropCandidate(out int sourceContainerId, out int instanceId, out InventoryItemVO item,
            out InventoryRotation preferredRotation)
        {
            InventoryViewModel source = m_owner?.DragSource ?? this;

            preferredRotation = InventoryRotation.None;
            instanceId = source.DraggingInstanceId;
            if (instanceId == NO_DRAG)
            {
                sourceContainerId = NO_SELECTION;
                item = null;
                return false;
            }

            sourceContainerId = source.ContainerId;
            preferredRotation = source.DragRotation;
            return source.Snapshot.Value.TryGetItem(instanceId, out item);
        }

        private static InventoryCellMark ToCellMark(InventoryMoveResult verdict, bool willSwap, bool willMerge)
        {
            if (!verdict.IsSuccessOrSame()) return InventoryCellMark.DropInvalid;
            if (willMerge) return InventoryCellMark.DropMerge;

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

        // 选中指定物品并高亮其占格
        private void SelectItem(int instanceId)
        {
            m_selectedInstanceId.TrySet(instanceId);
            RefreshCellMarks();
            RefreshCanRotate();
            SetHint(InventoryHintKind.Selected);
        }

        //重置选中态, 清理相关交互表现
        private void ResetSelection(InventoryHintKind hint)
        {
            m_selectedInstanceId.TrySet(NO_SELECTION);
            RefreshCellMarks();
            RefreshCanRotate();
            SetHint(hint);
            m_popupClosed.Fire();
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
