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

        private readonly Dictionary<int, InventoryContainer> m_containers = new Dictionary<int, InventoryContainer>();
        
        //背包总配置: 仅在构建快照时查询稀有度底色
        private readonly InventoryConfigSO m_config;

        //已开的物资箱: 配置到容器号, 同一份配置没关掉就重开要沿用旧箱
        private readonly Dictionary<LootContainerConfigSO, int> m_lootContainers =
            new Dictionary<LootContainerConfigSO, int>();

        //注销容器时反查待清理的物资箱键
        private readonly List<LootContainerConfigSO> m_staleLootKeys = new List<LootContainerConfigSO>();

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

        public void UnregisterContainer(int containerId)
        {
            if(!m_containers.TryGetValue(containerId, out InventoryContainer container)) return;
            
            container.OnChanged -= HandleContainerChanged;
            m_containers.Remove(containerId);
            ForgetLootContainer(containerId);
            
            OnChanged?.Invoke(containerId);
        }

        public bool IsContainerRegistered(int containerId) => m_containers.ContainsKey(containerId);

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

        public int OpenLootContainer(LootContainerConfigSO loot, int seed)
        {
            if (!TryGetLootGrid(loot, out InventoryContainerConfigSO grid)) return INVALID_CONTAINER_ID;

            //搜刮到一半的箱子重开是继续翻残货, 不再摇一次掉落
            if (m_lootContainers.TryGetValue(loot, out int openedId) && m_containers.ContainsKey(openedId))
                return openedId;

            int containerId = AllocateLootContainerId();
            RegisterContainer(containerId, new InventoryContainerSpec(grid.Width, grid.Height, LOOT_WEIGHT_LIMIT,
                grid.Mask, ResolveLootName(loot, grid)));
            m_lootContainers[loot] = containerId;

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

            //转移不改朝向, 拖过去是什么姿态就保持什么姿态
            InventoryRotation rotation = placement.Rotation;
            InventoryMoveResult verdict = ResolveTransfer(source, target, placement, cell, rotation,
                out InventoryItemPlacement occupant);

            if (verdict != InventoryMoveResult.Success) return verdict;

            InventoryMoveResult transferResult = occupant == null
                ? source.TryMoveTo(target, placement, cell, rotation)
                : source.TrySwapTo(target, placement, occupant, cell, rotation);

            if (transferResult.IsSuccessOrSame()) SyncWornCapacities();
            return transferResult;
        }

        public InventoryMoveResult PeekTransferItem(int sourceContainerId, int targetContainerId, int instanceId,
            Vector2Int cell, out bool willSwap)
        {
            willSwap = false;

            //悬停每帧都会问, 配错容器由 Try 路径报错, 预判这里静默免得刷屏
            if (!TryGetContainer(sourceContainerId, out InventoryContainer source)) return InventoryMoveResult.None;
            if (!TryGetContainer(targetContainerId, out InventoryContainer target)) return InventoryMoveResult.None;
            if (!TryGetPlacement(source, instanceId, out InventoryItemPlacement placement)) return InventoryMoveResult.None;

            InventoryMoveResult verdict = ResolveTransfer(source, target, placement, cell, placement.Rotation,
                out InventoryItemPlacement occupant);

            //被第三方物品压住才会改判交换, 落回自己身上不算
            willSwap = occupant != null && verdict == InventoryMoveResult.Success;
            return verdict;
        }

        //转移落点判定: 直落被占就改判与原物品交换, 试移与预判共用这一条规则
        private static InventoryMoveResult ResolveTransfer(InventoryContainer source, InventoryContainer target,
            InventoryItemPlacement moving, Vector2Int cell, InventoryRotation rotation, out InventoryItemPlacement occupant)
        {
            occupant = null;

            InventoryMoveResult verdict = source.Rules.CanMove(source, target, moving, cell, rotation);
            if (verdict != InventoryMoveResult.Occupied) return verdict;

            if (!target.Grid.TryGetPlacementAt(cell, out occupant)) return InventoryMoveResult.Occupied;

            return source.Rules.CanSwap(source, target, moving, occupant, cell, rotation);
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

        //把装备槽撑起的容量容器同步一遍: 槽里穿着容器装备就注册其容量, 槽空或装备不带容量就注销
        //本期不换装也不卸载, 装备槽内容写一次定终身, RegisterContainer 对已存在 id 早返回, 不会读到旧尺寸
        private void SyncWornCapacities()
        {
            int[] slotIds = InventoryContainerIds.EQUIP_SLOT_IDS;
            for (int i = 0; i < slotIds.Length; i++)
            {
                if (!m_containers.TryGetValue(slotIds[i], out InventoryContainer slot)) continue;
                if (!InventoryContainerIds.TryGetEquipSlotRole(slotIds[i], out InventoryEquipSlot role)) continue;
                if (!InventoryContainerIds.TryGetCapacityContainerId(role, out int capacityId)) continue;

                if (slot.Count == 0)
                {
                    UnregisterContainer(capacityId);
                    continue;
                }

                foreach (InventoryItemPlacement placement in slot.Placements)
                {
                    InventoryContainerConfigSO definition = placement.Instance.Config.Container;
                    if (definition == null) UnregisterContainer(capacityId);
                    else RegisterContainer(capacityId, new InventoryContainerSpec(definition.Width, definition.Height,
                        float.MaxValue, definition.Mask, definition.DisplayName));
                    break;
                }
            }
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

        //物资箱容器号从 LOOT 起顺序分配, 跳过已被占用的号
        private int AllocateLootContainerId()
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

        //容器注销后同一份物资箱配置下次打开要重新掉落, 所以连带清掉这条反向记录
        private void ForgetLootContainer(int containerId)
        {
            if (m_lootContainers.Count == 0) return;

            foreach (KeyValuePair<LootContainerConfigSO, int> pair in m_lootContainers)
            {
                if (pair.Value == containerId) m_staleLootKeys.Add(pair.Key);
            }

            for (int i = 0; i < m_staleLootKeys.Count; i++) m_lootContainers.Remove(m_staleLootKeys[i]);
            m_staleLootKeys.Clear();
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