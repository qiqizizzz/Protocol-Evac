/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包模型类                      
 * │  类    名: InventoryModel.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using Module.Inventory.Core.Config;
using Module.Inventory.Core.Config.Loot;
using Module.Inventory.Core.Config.Type;
using Module.Inventory.Core.Data;
using Module.Inventory.Core.Defines;
using Module.Inventory.Core.Loot;
using Module.Inventory.Core.Rule.Extensions;
using Module.Inventory.Core.Rule.Type;
using UnityEngine;
using Utils.log;

namespace UI.Inventory
{
    public class InventoryModel : IInventoryModel
    {
        //物资箱配置无效时返回的容器号
        private const int INVALID_CONTAINER_ID = -1;

        //装备槽按单格收纳, 只够放下一件装备
        private const int EQUIP_SLOT_SIZE = 1;

        //物资箱不设独立负重, 负重结算归角色整体
        private const float LOOT_WEIGHT_LIMIT = float.MaxValue;

        //换尺寸复位内容时的落位顺序: 行优先, 让同一批内容每次都按同样次序复原
        private static readonly Comparison<InventoryItemPlacement> m_placementOrder = ComparePlacementRowFirst;

        private readonly Dictionary<int, InventoryContainer> m_containers = new Dictionary<int, InventoryContainer>();
        
        //背包总配置: 仅在构建快照时查询稀有度底色
        private readonly InventoryConfigSO m_config;

        //已开的物资箱: 实体号到运行时记录, 同一只箱子没关掉就重开要沿用旧箱
        private readonly Dictionary<string, LootContainerRecord> m_lootContainers =
            new Dictionary<string, LootContainerRecord>();

        //容器号反查运行时记录, 注销与换尺寸时不用遍历整张物资箱表
        private readonly Dictionary<int, LootContainerRecord> m_lootContainerRecords =
            new Dictionary<int, LootContainerRecord>();

        public event Action<int> OnChanged;

        //装配处注入总配置, 不存在无配置的半成品模型
        public InventoryModel(InventoryConfigSO config)
        {
            m_config = config;
        }

        #region Model生命周期
        public void OnRegister()
        {
            //装备槽是角色身体拓扑的一部分, 模型注册时就把六个槽位立起来
            int[] equipSlotIds = InventoryContainerIds.EQUIP_SLOT_IDS;
            for (int i = 0; i < equipSlotIds.Length; i++)
            {
                if (!InventoryContainerIds.TryGetEquipSlotRole(equipSlotIds[i], out InventoryEquipSlot role)) continue;

                RegisterContainer(equipSlotIds[i], new InventoryContainerSpec(EQUIP_SLOT_SIZE, EQUIP_SLOT_SIZE,
                    float.MaxValue, null, SlotDisplayName(role), role));
            }
        }

        public void OnUnregister()
        {
            List<int> containerIds = new List<int>(m_containers.Keys);
            foreach (int containerId in containerIds)
                UnregisterContainer(containerId);
        }
        #endregion

        #region 主要方法
        public void RegisterContainer(int containerId, InventoryContainerSpec spec)
        {
            if(m_containers.ContainsKey(containerId)) return;
            
            InventoryContainer container = new InventoryContainer(containerId, spec);
            container.OnChanged += HandleContainerChanged;
            m_containers[containerId] = container;

            //新开容器也是拓扑变化, 运行时注册的战利品箱要靠这条广播刷出网格
            OnChanged?.Invoke(containerId);
        }

        //运行时容器号从 LOOT 段起分配, 物资箱与地面丢弃物容器都走这里, 避免两套分配器撞号
        public int CreateRuntimeContainer(InventoryContainerSpec spec)
        {
            if (spec.Width <= 0 || spec.Height <= 0)
            {
                QLog.Error($"容器规格无效, 无法分配容器号：{spec.Width}x{spec.Height}");
                return INVALID_CONTAINER_ID;
            }

            int containerId = AllocateRuntimeContainerId();
            RegisterContainer(containerId, spec);
            return containerId;
        }

        public void UnregisterContainer(int containerId)
        {
            if(!m_containers.TryGetValue(containerId, out InventoryContainer container)) return;
            
            container.OnChanged -= HandleContainerChanged;
            m_containers.Remove(containerId);
            ForgetLootContainer(containerId);
            
            OnChanged?.Invoke(containerId);
        }

        public bool IsContainerRegistered(int containerId) => m_containers.ContainsKey(containerId);

        /// <summary>
        /// 换容器尺寸: 网格拓扑重建但容器身份不变, 内容优先按原位复位, 有件放不下则整次回滚原拓扑
        /// </summary>
        /// <param name="containerId">要换尺寸的容器号</param>
        /// <param name="spec">新的网格规格</param>
        public bool TryResizeContainer(int containerId, InventoryContainerSpec spec)
        {
            if (!m_containers.TryGetValue(containerId, out InventoryContainer current))
            {
                QLog.Error($"容器未注册：{containerId}");
                return false;
            }

            //拓扑本来就一致时不重建, 免得白白吃一次注销与视图重排
            if (IsSameShape(current, spec)) return true;

            InventoryContainerSpec origin = ToSpec(current);
            List<InventoryItemPlacement> contents = CollectOrdered(current);

            //换尺寸靠注销重注册完成, 先把物资箱记录摘出来, 拓扑落定后原样挂回
            m_lootContainerRecords.TryGetValue(containerId, out LootContainerRecord lootRecord);

            UnregisterContainer(containerId);
            RegisterContainer(containerId, spec);
            if (RestorePlacements(m_containers[containerId], contents))
            {
                RebindLootContainer(ref lootRecord, containerId);
                return true;
            }

            //新网格塞不下旧内容, 连拓扑一起退回原样
            UnregisterContainer(containerId);
            RegisterContainer(containerId, origin);
            RebindLootContainer(ref lootRecord, containerId);
            if (!RestorePlacements(m_containers[containerId], contents))
                QLog.Error($"容器回滚后内容仍复原失败：{containerId}");

            QLog.Error($"容器换尺寸放不下现有内容, 已保持原拓扑：{containerId} " +
                       $"{origin.Width}x{origin.Height} -> {spec.Width}x{spec.Height}");
            return false;
        }

        //自动找第一块空位塞入新物品, 穿戴装备与造演示物品的主路径
        public InventoryMoveResult TryAddItemAuto(int containerId, int itemId, int amount)
        {
            if (!TryGetContainer(containerId, out InventoryContainer container))
            {
                QLog.Error($"容器未注册：{containerId}");
                return InventoryMoveResult.None;
            }

            if (!ItemRegistry.TryGet(itemId, out InventoryItemConfigSO config))
            {
                QLog.Error($"物品配置未登记：{itemId}");
                return InventoryMoveResult.ConfigMissing;
            }

            InventoryItemInstance instance = new InventoryItemInstance(config, amount);
            if (!TryFindFreeCell(container, container.ResolveFootprint(instance, InventoryRotation.None), out Vector2Int cell))
                return InventoryMoveResult.Occupied;

            if (container.TryAdd(instance, cell) == null) return InventoryMoveResult.Occupied;

            SyncWornCapacities();
            return InventoryMoveResult.Success;
        }

        public InventoryContainerVO GetContainerVO(int containerId)
        {
            if (!m_containers.TryGetValue(containerId, out InventoryContainer container))
            {
                QLog.Error($"容器未注册：{containerId}");
                return InventoryContainerVO.Empty;
            }

            InventoryItemVO[] items = new InventoryItemVO[container.Count];
            int index = 0;
            foreach (InventoryItemPlacement placement in container.Placements)
            {
                InventoryItemInstance instance = placement.Instance;
                items[index++] = new InventoryItemVO(instance.InstanceId, instance.ItemId, instance.Config.DisplayName,
                    instance.StackCount, instance.Config.Icon, m_config.GetRarityColor(instance.Config.Rarity), instance.Config.Rarity, instance.Config.UnitValue,
                    placement.Position, placement.Size, placement.Rotation);
            }

            return new InventoryContainerVO(container.ContainerId, container.Grid.Width, container.Grid.Height,
                container.Grid.Mask, container.Version, container.TotalWeight, container.WeightLimit, items,
                container.DisplayName);
        }

        public int OpenLootContainer(string entityId, LootContainerConfigSO loot, int seed)
        {
            //实体号是物资箱的唯一身份, 缺了它两只同配置箱子会共用一个容器
            if (string.IsNullOrEmpty(entityId))
            {
                QLog.Error("物资箱实体号为空, 无法打开");
                return INVALID_CONTAINER_ID;
            }

            if (!TryGetLootGrid(loot, out InventoryContainerConfigSO grid)) return INVALID_CONTAINER_ID;

            //搜刮到一半的箱子重开是继续翻残货, 不再摇一次掉落
            if (m_lootContainers.TryGetValue(entityId, out LootContainerRecord opened) &&
                m_containers.ContainsKey(opened.ContainerId))
            {
                //同一只箱子开着的时候换掉落表, 残货就和配置对不上了, 直接拒绝
                if (opened.Loot != loot)
                {
                    QLog.Error($"物资箱 {entityId} 未关闭就更换掉落配置, 拒绝重开");
                    return INVALID_CONTAINER_ID;
                }

                return opened.ContainerId;
            }

            int containerId = CreateRuntimeContainer(new InventoryContainerSpec(grid.Width, grid.Height,
                LOOT_WEIGHT_LIMIT, grid.Mask, ResolveLootName(loot, grid)));
            if (containerId == INVALID_CONTAINER_ID) return INVALID_CONTAINER_ID;

            LootContainerRecord record = new LootContainerRecord(entityId, containerId, loot);
            m_lootContainers[entityId] = record;
            m_lootContainerRecords[containerId] = record;

            FillLootContainer(m_containers[containerId], loot, seed);
            return containerId;
        }

        public InventoryMoveResult TryAddItem(int containerId, int itemId, int amount, Vector2Int cell)
        {
            if(!TryGetContainer(containerId, out InventoryContainer container))
            {
                QLog.Error($"容器未注册：{containerId}");
                return InventoryMoveResult.None;
            }

            if (!ItemRegistry.TryGet(itemId, out InventoryItemConfigSO config))
            {
                QLog.Error($"物品配置未登记：{itemId}");
                return InventoryMoveResult.ConfigMissing;
            }

            InventoryItemInstance instance = new InventoryItemInstance(config, amount);

            InventoryMoveResult reason = container.Rules.CanAdd(container, instance, cell, InventoryRotation.None);
            if(reason != InventoryMoveResult.Success) return reason;

            if (container.TryAdd(instance, cell) == null) return InventoryMoveResult.Occupied;

            SyncWornCapacities();
            return InventoryMoveResult.Success;
        }

        public InventoryMoveResult TryMoveItem(int containerId, int instanceId, Vector2Int cell, InventoryRotation rotation)
        {
            if(!TryGetContainer(containerId, out InventoryContainer container))
            {
                QLog.Error($"容器未注册：{containerId}");
                return InventoryMoveResult.None;
            }

            if (!TryGetPlacement(container, instanceId, out InventoryItemPlacement placement))
                return InventoryMoveResult.SourceNotFound;

            InventoryMoveResult moveResult = container.TryMoveTo(container, placement, cell, rotation);
            if (moveResult.IsSuccessOrSame()) SyncWornCapacities();

            return moveResult;
        }

        public InventoryMoveResult TryTransferItem(int sourceContainerId, int targetContainerId, int instanceId, Vector2Int cell)
        {
            return TransferItem(sourceContainerId, targetContainerId, instanceId, cell, null, out _);
        }

        public InventoryMoveResult TryTransferItem(int sourceContainerId, int targetContainerId, int instanceId, Vector2Int cell,
            InventoryRotation preferredRotation)
        {
            return TransferItem(sourceContainerId, targetContainerId, instanceId, cell, preferredRotation, out _);
        }

        // 转移落点的共同实现: preferredRotation 为空时沿用物品当前朝向, 落点被同类物品占住时优先并堆
        private InventoryMoveResult TransferItem(int sourceContainerId, int targetContainerId, int instanceId, Vector2Int cell,
            InventoryRotation? preferredRotation, out InventoryRotation rotation)
        {
            rotation = InventoryRotation.None;

            if (!TryGetContainer(sourceContainerId, out InventoryContainer source))
            {
                QLog.Error($"容器未注册：{sourceContainerId}");
                return InventoryMoveResult.None;
            }

            if (!TryGetContainer(targetContainerId, out InventoryContainer target))
            {
                QLog.Error($"容器未注册：{targetContainerId}");
                return InventoryMoveResult.None;
            }

            if (!TryGetPlacement(source, instanceId, out InventoryItemPlacement placement))
                return InventoryMoveResult.SourceNotFound;

            // 落点判定由规则层处理, 指定朝向不可放置时自动改用可用朝向
            InventoryMoveResult verdict = source.Rules.ResolveTransfer(source, target, placement, cell,
                preferredRotation ?? placement.Rotation, out rotation, out InventoryItemPlacement occupant);

            if (verdict != InventoryMoveResult.Success) return verdict;

            // 落点是同类可堆物品时并堆, 数量整堆或部分并走, 格子位置不变
            if (occupant != null && source.Rules.CanMerge(source, target, placement, occupant))
            {
                InventoryMoveResult mergeResult = source.TryAbsorb(target, placement, occupant, out _);
                if (mergeResult.IsSuccessOrSame()) SyncWornCapacities();
                return mergeResult;
            }

            InventoryMoveResult transferResult = occupant == null
                ? source.TryMoveTo(target, placement, cell, rotation)
                : source.TrySwapTo(target, placement, occupant, cell, rotation);

            if (transferResult.IsSuccessOrSame()) SyncWornCapacities();
            return transferResult;
        }

        public InventoryMoveResult PeekTransferItem(int sourceContainerId, int targetContainerId, int instanceId,
            Vector2Int cell, out bool willSwap, out InventoryRotation rotation)
        {
            return PeekTransfer(sourceContainerId, targetContainerId, instanceId, cell, null,
                out willSwap, out _, out rotation);
        }

        public InventoryMoveResult PeekTransferItem(int sourceContainerId, int targetContainerId, int instanceId,
            Vector2Int cell, InventoryRotation preferredRotation, out bool willSwap, out bool willMerge,
            out InventoryRotation rotation)
        {
            return PeekTransfer(sourceContainerId, targetContainerId, instanceId, cell, preferredRotation,
                out willSwap, out willMerge, out rotation);
        }

        // 落点预判的共同实现: 只判定不改状态, 供拖拽每帧的落点着色使用
        private InventoryMoveResult PeekTransfer(int sourceContainerId, int targetContainerId, int instanceId, Vector2Int cell,
            InventoryRotation? preferredRotation, out bool willSwap, out bool willMerge, out InventoryRotation rotation)
        {
            willSwap = false;
            willMerge = false;
            rotation = InventoryRotation.None;

            //悬停每帧都会问, 配错容器由 Try 路径报错, 预判这里静默免得刷屏
            if (!TryGetContainer(sourceContainerId, out InventoryContainer source)) return InventoryMoveResult.None;
            if (!TryGetContainer(targetContainerId, out InventoryContainer target)) return InventoryMoveResult.None;
            if (!TryGetPlacement(source, instanceId, out InventoryItemPlacement placement)) return InventoryMoveResult.None;

            InventoryMoveResult verdict = source.Rules.ResolveTransfer(source, target, placement, cell,
                preferredRotation ?? placement.Rotation, out rotation, out InventoryItemPlacement occupant);

            //被第三方物品压住才会改判并堆或交换, 落回自己身上不算
            bool occupied = occupant != null && verdict == InventoryMoveResult.Success;
            willMerge = occupied && source.Rules.CanMerge(source, target, placement, occupant);
            willSwap = occupied && !willMerge;
            return verdict;
        }

        public InventoryMoveResult TrySplitItem(int containerId, int instanceId, int count, out int newInstanceId)
        {
            newInstanceId = 0;

            if (!TryGetContainer(containerId, out InventoryContainer container))
            {
                QLog.Error($"容器未注册：{containerId}");
                return InventoryMoveResult.None;
            }

            if (!TryGetPlacement(container, instanceId, out InventoryItemPlacement placement))
                return InventoryMoveResult.SourceNotFound;

            // 拆分要求留一整份在原堆, 拆出的数量也至少是一整份
            if (count < 1 || count >= placement.Instance.StackCount) return InventoryMoveResult.None;
            if (!TryFindFreeCell(container, placement.Size, out Vector2Int cell)) return InventoryMoveResult.OutOfBounds;

            // 先减原堆再落新堆, 新堆放不下时把数量原样退回
            placement.Instance.TryReduceStack(count);

            InventoryItemPlacement split = container.TryAdd(
                new InventoryItemInstance(placement.Instance.Config, count), cell, placement.Rotation);
            if (split == null)
            {
                placement.Instance.TryAddStack(count, out _);
                return InventoryMoveResult.Occupied;
            }

            newInstanceId = split.Instance.InstanceId;
            SyncWornCapacities();
            return InventoryMoveResult.Success;
        }

        public InventoryMoveResult TryRotateItem(int containerId, int instanceId)
        {
            if(!TryGetContainer(containerId, out InventoryContainer container))
            {
                QLog.Error($"容器未注册：{containerId}");
                return InventoryMoveResult.None;
            }
            
            if(!TryGetPlacement(container, instanceId, out InventoryItemPlacement placement))
                return InventoryMoveResult.SourceNotFound;

            return container.TryRotate(placement);
        }

        public bool TryGetInstanceIdAt(int containerId, Vector2Int cell, out int instanceId)
        {
            instanceId = 0;
            if(!TryGetContainer(containerId, out InventoryContainer container))
            {
                QLog.Error($"容器未注册：{containerId}");
                return false;
            }
            
            if(!container.Grid.TryGetPlacementAt(cell, out InventoryItemPlacement placement))
                return false;
            
            instanceId = placement.Instance.InstanceId;
            return true;
        }
        #endregion

        #region 事件
        private void HandleContainerChanged(InventoryContainer container)
        {
            OnChanged?.Invoke(container.ContainerId);
        }
        #endregion

        //把装备槽撑起的容量容器同步一遍: 槽里穿着容器装备就刷成型号的网格, 卸下后常驻容器回落基础尺寸, 非常驻容器才注销
        private void SyncWornCapacities()
        {
            int[] slotIds = InventoryContainerIds.EQUIP_SLOT_IDS;
            for (int i = 0; i < slotIds.Length; i++)
            {
                if (!m_containers.TryGetValue(slotIds[i], out InventoryContainer slot)) continue;
                if (!InventoryContainerIds.TryGetEquipSlotRole(slotIds[i], out InventoryEquipSlot role)) continue;
                if (!InventoryContainerIds.TryGetCapacityContainerId(role, out int capacityId)) continue;

                InventoryContainerConfigSO definition = GetWornDefinition(slot);
                if (definition == null)
                {
                    //常驻容器不因卸下型号而消失; 没配基础网格就维持现状, 配置缺失由装配处 ApplyInnateContainer 报错
                    if (InventoryContainerIds.IsPersistentCapacityContainer(capacityId))
                    {
                        if (TryResolvePersistentFallback(capacityId, out InventoryContainerSpec fallback))
                            ApplyWornCapacity(capacityId, fallback);
                        continue;
                    }

                    UnregisterContainer(capacityId);
                    continue;
                }

                ApplyWornCapacity(capacityId, new InventoryContainerSpec(definition.Width, definition.Height,
                    float.MaxValue, definition.Mask, definition.DisplayName));
            }
        }

        //装备槽撑起容量容器: 从没注册过就按规格建起来, 已经存在才走换尺寸, 让首次穿戴与卸下回退都落得了地
        private void ApplyWornCapacity(int capacityId, InventoryContainerSpec spec)
        {
            if (!IsContainerRegistered(capacityId))
            {
                RegisterContainer(capacityId, spec);
                return;
            }

            TryResizeContainer(capacityId, spec);
        }

        //装备槽按单格收纳, 取槽里第一件装备挂着的容器定义; 槽空或装备不带容量返回 null
        private static InventoryContainerConfigSO GetWornDefinition(InventoryContainer slot)
        {
            foreach (InventoryItemPlacement placement in slot.Placements)
                return placement.Instance.Config.Container;

            return null;
        }

        //常驻容器卸下型号后的回落规格; 总配置没有穿戴表或未配基础网格时静默返回 false, 这里不是配置校验的出口
        private bool TryResolvePersistentFallback(int capacityId, out InventoryContainerSpec fallback)
        {
            fallback = default;

            InventoryLoadoutConfigSO loadout = m_config != null ? m_config.Loadout : null;
            if (loadout == null) return false;
            if (!loadout.TryGetPersistentContainer(capacityId, out InventoryContainerConfigSO baseContainer)) return false;

            fallback = new InventoryContainerSpec(baseContainer.Width, baseContainer.Height, float.MaxValue,
                baseContainer.Mask, baseContainer.DisplayName);
            return true;
        }

        //把已注册容器的当前拓扑读成规格, 供换尺寸失败时按原样重建
        private static InventoryContainerSpec ToSpec(InventoryContainer container)
        {
            return new InventoryContainerSpec(container.Grid.Width, container.Grid.Height, container.WeightLimit,
                container.Grid.Mask, container.DisplayName, container.EquipSlot);
        }

        //现有拓扑与目标规格是否完全一致, 一致就没必要重建
        private static bool IsSameShape(InventoryContainer container, InventoryContainerSpec spec)
        {
            if (container.Grid.Width != spec.Width) return false;
            if (container.Grid.Height != spec.Height) return false;
            if (container.EquipSlot != spec.EquipSlot) return false;
            if (container.DisplayName != spec.DisplayName) return false;

            return IsSameMask(container.Grid.Mask, spec.Mask);
        }

        //逐格比对实心掩码, 规格侧为空或长度不符时按满格处理, 与 InventoryGrid 的容错口径一致
        private static bool IsSameMask(IReadOnlyList<bool> gridMask, IReadOnlyList<bool> specMask)
        {
            bool specUsable = specMask != null && specMask.Count == gridMask.Count;

            for (int i = 0; i < gridMask.Count; i++)
            {
                bool solid = !specUsable || specMask[i];
                if (gridMask[i] != solid) return false;
            }

            return true;
        }

        //抄下容器内容并按行优先排序, Placements 是 HashSet 枚举顺序不稳定, 排序后回滚结果才可复现
        private static List<InventoryItemPlacement> CollectOrdered(InventoryContainer container)
        {
            List<InventoryItemPlacement> contents = new List<InventoryItemPlacement>(container.Placements);
            contents.Sort(m_placementOrder);
            return contents;
        }

        //逐件复位到新网格: 原位放得下就原位, 放不下再找空位, 有一件彻底放不下即整体失败
        private static bool RestorePlacements(InventoryContainer container, List<InventoryItemPlacement> contents)
        {
            for (int i = 0; i < contents.Count; i++)
            {
                InventoryItemPlacement origin = contents[i];
                if (container.TryAdd(origin.Instance, origin.Position, origin.Rotation) != null) continue;

                Vector2Int footprint = container.ResolveFootprint(origin.Instance, origin.Rotation);
                if (!TryFindFreeCell(container, footprint, out Vector2Int cell)) return false;
                if (container.TryAdd(origin.Instance, cell, origin.Rotation) == null) return false;
            }

            return true;
        }

        //行优先比较, 让内容复位与回滚的顺序稳定
        private static int ComparePlacementRowFirst(InventoryItemPlacement left, InventoryItemPlacement right)
        {
            int byRow = left.Position.y.CompareTo(right.Position.y);
            return byRow != 0 ? byRow : left.Position.x.CompareTo(right.Position.x);
        }

        //装备槽表头显示名, 展示穿戴部位而不是装备名
        private static string SlotDisplayName(InventoryEquipSlot role)
        {
            switch (role)
            {
                case InventoryEquipSlot.Helmet: return "头盔";
                case InventoryEquipSlot.ChestRig: return "胸挂";
                case InventoryEquipSlot.Backpack: return "背包";
                case InventoryEquipSlot.PrimaryWeapon: return "主武器";
                case InventoryEquipSlot.SecondaryWeapon: return "副武器";
                case InventoryEquipSlot.Melee: return "近战";
                case InventoryEquipSlot.Pistol: return "手枪";
                case InventoryEquipSlot.Armor: return "护甲";
                case InventoryEquipSlot.SafeBox: return "安全箱";
                default: return string.Empty;
            }
        }

        //物资箱配置校验, 缺网格的箱子开不出来
        private static bool TryGetLootGrid(LootContainerConfigSO loot, out InventoryContainerConfigSO grid)
        {
            grid = loot != null ? loot.Container : null;

            if (loot == null) QLog.Error("物资箱配置为空, 无法打开");
            else if (grid == null) QLog.Error($"物资箱 {loot.DisplayName} 缺少网格定义, 无法打开");

            return grid != null;
        }

        //箱名优先取物资箱自身的配置, 没配就沿用网格定义里的容器名
        private static string ResolveLootName(LootContainerConfigSO loot, InventoryContainerConfigSO grid)
        {
            return string.IsNullOrEmpty(loot.DisplayName) ? grid.DisplayName : loot.DisplayName;
        }

        //运行时容器号从 LOOT 起顺序分配, 跳过已被占用的号
        private int AllocateRuntimeContainerId()
        {
            int containerId = InventoryContainerIds.LOOT;
            while (m_containers.ContainsKey(containerId)) containerId++;

            return containerId;
        }

        //把抽取结果逐件塞进箱子, 塞不下的静默丢弃, 异形空洞由网格的空位判定挡掉
        private void FillLootContainer(InventoryContainer container, LootContainerConfigSO loot, int seed)
        {
            IReadOnlyList<LootDrop> drops = LootRoller.Roll(loot, seed);

            for (int i = 0; i < drops.Count; i++)
            {
                InventoryItemConfigSO item = drops[i].Item;
                if (!TryFindFreeCell(container, item.Size, out Vector2Int cell)) continue;

                container.TryAdd(new InventoryItemInstance(item, drops[i].Count), cell);
            }
        }

        //从左上往右下找第一块放得下指定尺寸的连续空格
        private static bool TryFindFreeCell(InventoryContainer container, Vector2Int size, out Vector2Int cell)
        {
            for (int y = 0; y + size.y <= container.Grid.Height; y++)
            {
                for (int x = 0; x + size.x <= container.Grid.Width; x++)
                {
                    cell = new Vector2Int(x, y);
                    if (container.Grid.IsAreaFree(cell, size)) return true;
                }
            }

            cell = default;
            return false;
        }

        //容器注销后同一只箱子下次打开要重新掉落, 所以连带清掉这一对映射
        private void ForgetLootContainer(int containerId)
        {
            if (!m_lootContainerRecords.TryGetValue(containerId, out LootContainerRecord record)) return;

            m_lootContainerRecords.Remove(containerId);
            m_lootContainers.Remove(record.EntityId);
        }

        //换尺寸走注销重注册时会顺带清掉物资箱映射, 拓扑落定后按新容器号挂回去
        private void RebindLootContainer(ref LootContainerRecord record, int containerId)
        {
            if (string.IsNullOrEmpty(record.EntityId)) return;

            record = new LootContainerRecord(record.EntityId, containerId, record.Loot);
            m_lootContainers[record.EntityId] = record;
            m_lootContainerRecords[containerId] = record;
        }

        //一只开着物资箱的运行时身份
        private readonly struct LootContainerRecord
        {
            public readonly string EntityId;
            public readonly int ContainerId;
            public readonly LootContainerConfigSO Loot;

            public LootContainerRecord(string entityId, int containerId, LootContainerConfigSO loot)
            {
                EntityId = entityId;
                ContainerId = containerId;
                Loot = loot;
            }
        }

        private bool TryGetContainer(int containerId, out InventoryContainer container)
        {
            if(m_containers.TryGetValue(containerId, out container)) return true;
            
            container = null;
            return false;
        }

        private bool TryGetPlacement(InventoryContainer container, int instanceId, out InventoryItemPlacement placement)
        {
            foreach (InventoryItemPlacement item in container.Placements)
            {
                if (item.Instance.InstanceId == instanceId)
                {
                    placement = item;
                    return true;
                }
            }
            
            placement = null;
            return false;
        }
    }
}
