/*
* ┌──────────────────────────────────┐
* │  描    述: 容器配置工具, 按槽位与等级生成容器定义和容器装备并写回穿戴表
* │  类    名: InventoryContainerTool.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using System.IO;
using Module.Inventory.Core.Config;
using Module.Inventory.Core.Config.Type;
using Module.Inventory.Core.Defines;
using UnityEditor;
using UnityEngine;
using Utils.log;

namespace UI.Inventory.Editor
{
    // 容器配置工具: 背包/胸挂/安全箱的格数与装备属性在这里定, 全程走 SerializedObject, 不碰反射也不碰底色
    public sealed class InventoryContainerTool : EditorWindow
    {
        // ==================== 常量与静态字段 ====================
        private const string CONTAINER_DIR = "Assets/Config/Inventory/Container";
        private const string ITEM_DIR = "Assets/Config/Inventory/Item/Container";
        private const string ITEM_ROOT_DIR = "Assets/Config/Inventory/Item";
        private const string LOADOUT_PATH = "Assets/Config/Inventory/LoadoutConfig_默认穿戴.asset";
        private const string MASTER_CONFIG_PATH = "Assets/Config/Inventory/InventoryConfig_背包总配置.asset";

        private const int MAX_LEVEL = 6;
        private const int BACKPACK_ITEM_BASE = 6000;
        private const int RIG_ITEM_BASE = 6100;
        private const int SAFEBOX_ITEM_BASE = 6200;

        // 容器定义与容器装备共用的显示名字段, 抽出来防止两边写歪
        private const string PROP_DISPLAY_NAME = "DisplayNameValue";
        private const string PROP_MASTER_ITEMS = "ItemsValue";
        private const string PROP_SHAPE_ROWS = "RowsValue";

        // 工具只管可穿戴的容器装备, 口袋是角色固有容量, 要改直接编辑它的容器定义资产
        private static readonly int[] m_slotIds =
        {
            InventoryContainerIds.BACKPACK,
            InventoryContainerIds.RIG,
            InventoryContainerIds.SAFEBOX
        };

        // ==================== 字段[私有] ====================
        private int m_slotIndex;
        private int m_level = 1;

        private string m_containerName = string.Empty;
        private int m_containerWidth = 8;
        private int m_containerHeight = 5;

        // 异形网格绘制板, 行列与掩码都归它管
        private readonly InventoryShapeBoard m_board = new InventoryShapeBoard();

        private string m_itemName = string.Empty;
        private int m_itemGridWidth = 3;
        private int m_itemGridHeight = 3;
        private InventoryItemRarity m_rarity = InventoryItemRarity.Common;
        private int m_unitValue = 1000;
        private float m_unitWeight = 1f;
        private Sprite m_icon;

        private bool m_writeLoadout = true;
        private string m_message = "选槽位与等级, 调好网格和装备属性后点生成";

        // ==================== 属性 ====================
        private int SelectedSlot => m_slotIds[m_slotIndex];

        // ==================== 生命周期 ====================
        private void OnEnable()
        {
            LoadForm();
        }

        private void OnGUI()
        {
            int previousSlotIndex = m_slotIndex;
            int previousLevel = m_level;

            EditorGUILayout.Space(4f);
            m_slotIndex = EditorGUILayout.Popup("容器槽位", m_slotIndex, SlotNames());
            m_level = EditorGUILayout.IntSlider(new GUIContent("等级", "同一槽位按等级区分不同格数的容器装备"), m_level, 1, MAX_LEVEL);

            // 换槽位或换等级时重新回填一次, 免得把上一档的数值写到这一档
            if (previousSlotIndex != m_slotIndex || previousLevel != m_level)
                LoadForm();

            EditorGUILayout.LabelField("容器网格", EditorStyles.boldLabel);
            m_containerName = EditorGUILayout.TextField(new GUIContent("容器显示名", "载体表头那行字, 留空则按槽位与等级自动生成"), m_containerName);
            m_containerWidth = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("列数", "网格横向格子数"), m_containerWidth));
            m_containerHeight = Mathf.Max(1, EditorGUILayout.IntField("行数", m_containerHeight));

            m_board.Resize(m_containerWidth, m_containerHeight);
            if (m_board.Draw("形状", "点格子切换实心与空洞, 按住可以拖着刷")) Repaint();

            int outlineCount = m_containerWidth * m_containerHeight;
            int solidCount = m_board.SolidCount;
            EditorGUILayout.LabelField("总格数", $"{solidCount} 实心 / {outlineCount} 外接");
            EditorGUILayout.LabelField(new GUIContent("扩容比", "容器实心格数除以装备自身占格面积, 越大越能装"),
                (solidCount / (double)(m_itemGridWidth * m_itemGridHeight)).ToString("F2"));

            EditorGUILayout.LabelField("容器装备", EditorStyles.boldLabel);
            m_itemName = EditorGUILayout.TextField(new GUIContent("装备显示名", "留空则按槽位与等级自动生成"), m_itemName);

            EditorGUILayout.BeginHorizontal();
            m_itemGridWidth = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("占格宽", "这件装备摆进别的网格时占几列"), m_itemGridWidth));
            m_itemGridHeight = Mathf.Max(1, EditorGUILayout.IntField("占格高", m_itemGridHeight));
            EditorGUILayout.EndHorizontal();

            m_rarity = (InventoryItemRarity)EditorGUILayout.EnumPopup(new GUIContent("稀有度", "只决定底板色档位, 具体颜色在背包总配置里配"), m_rarity);

            EditorGUILayout.BeginHorizontal();
            m_unitValue = Mathf.Max(0, EditorGUILayout.IntField("单件估价", m_unitValue));
            m_unitWeight = Mathf.Max(0f, EditorGUILayout.FloatField("单件重量", m_unitWeight));
            EditorGUILayout.EndHorizontal();

            m_icon = (Sprite)EditorGUILayout.ObjectField(new GUIContent("图标", "装备在网格里显示的图, 留空则没有图标"), m_icon, typeof(Sprite), false);
            m_writeLoadout = EditorGUILayout.Toggle(new GUIContent("写入穿戴表", "勾上生成后直接穿到角色身上, 不勾只落配置"), m_writeLoadout);

            EditorGUILayout.Space(6f);
            if (GUILayout.Button("生成 / 更新", GUILayout.Height(28f)))
                Generate();
            if (GUILayout.Button(new GUIContent("按 ItemId 全量刷新总配置物品表", "扫盘上所有物品配置, 排序后整批覆盖物品表")))
                RebuildMasterItemTable();

            EditorGUILayout.Space(6f);
            DrawCurrentState();

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(m_message, MessageType.Info);
        }

        // ==================== Public Function ====================
        [MenuItem("Tools/背包/容器配置工具")]
        public static void Open()
        {
            InventoryContainerTool window = GetWindow<InventoryContainerTool>();
            window.titleContent = new GUIContent("容器配置工具");
            window.minSize = new Vector2(360f, 560f);
            window.Focus();
        }

        // ==================== Private Function ====================
        // 生成这一档容器: 容器定义 → 容器装备 → 穿戴表 → 总配置物品表
        private void Generate()
        {
            m_board.Resize(m_containerWidth, m_containerHeight);

            int slot = SelectedSlot;
            int level = m_level;
            EnsureFolder(CONTAINER_DIR);
            EnsureFolder(ITEM_DIR);

            bool containerCreated = false;
            InventoryContainerConfigSO container = LoadOrCreate<InventoryContainerConfigSO>(ContainerPath(slot, level), ref containerCreated);
            ApplyToContainer(container, slot, level);

            bool itemCreated = false;
            InventoryItemConfigSO item = LoadOrCreate<InventoryItemConfigSO>(ItemPath(slot, level), ref itemCreated);
            bool isNew = containerCreated || itemCreated;
            ApplyToItem(item, slot, level, container, isNew);

            string wornNote = m_writeLoadout ? WriteLoadout(slot, item) : "穿戴表未改动";
            int appended = AppendToMaster(item);

            AssetDatabase.SaveAssets();

            m_message = $"{(isNew ? "新建" : "更新")} {SlotNameOf(slot)} Lv{level}: " +
                        $"装备 {item.ItemId}「{item.DisplayName}」撑起 {container.Width}x{container.Height} = {InventoryShapeBoard.CountSolid(container.Mask)} 实心格, " +
                        $"{wornNote}{(appended > 0 ? ", 并已加入总配置物品表" : ", 已在总配置物品表")}";
            QLog.Info(m_message);
        }

        // 写容器定义: 这里只管名字和网格, 底色等全局参数归总配置
        private void ApplyToContainer(InventoryContainerConfigSO container, int slot, int level)
        {
            SerializedObject target = new SerializedObject(container);
            SetString(target, PROP_DISPLAY_NAME, ResolveName(m_containerName, AutoContainerName(slot, level)));
            SetInt(target, "WidthValue", m_containerWidth);
            SetInt(target, "HeightValue", m_containerHeight);
            SetStringArray(target, PROP_SHAPE_ROWS, InventoryContainerConfigSO.ToShapeRows(m_board.Shape, m_containerWidth, m_containerHeight));
            target.ApplyModifiedProperties();
            EditorUtility.SetDirty(container);
        }

        // 写容器装备: 编号按号段加等级推出来, 网格靠引用容器定义派生, 不存尺寸副本
        private void ApplyToItem(InventoryItemConfigSO item, int slot, int level,
            InventoryContainerConfigSO container, bool isNew)
        {
            SerializedObject target = new SerializedObject(item);
            SetInt(target, "ItemIdValue", ItemBaseOf(slot) + level - 1);
            SetString(target, PROP_DISPLAY_NAME, ResolveName(m_itemName, AutoItemName(slot, level)));
            SetEnum(target, "RarityValue", m_rarity);
            SetInt(target, "GridWidthValue", m_itemGridWidth);
            SetInt(target, "GridHeightValue", m_itemGridHeight);
            SetInt(target, "UnitValueValue", m_unitValue);
            SetFloat(target, "UnitWeightValue", m_unitWeight);
            SetObject(target, "IconValue", m_icon);
            SetObject(target, "ContainerValue", container);

            // 新建的容器装备默认不可堆叠不可旋转, 已有资产不覆盖手工调过的值
            if (isNew)
            {
                SetBool(target, "CanRotateValue", false);
                SetInt(target, "MaxStackCountValue", 1);
            }

            target.ApplyModifiedProperties();
            EditorUtility.SetDirty(item);
        }

        // 把生成的装备写到穿戴表对应槽位, 返回一句结论给界面显示
        private string WriteLoadout(int slot, InventoryItemConfigSO item)
        {
            InventoryLoadoutConfigSO loadout = AssetDatabase.LoadAssetAtPath<InventoryLoadoutConfigSO>(LOADOUT_PATH);
            if (loadout == null)
            {
                QLog.Error($"找不到穿戴表, 槽位 {slot} 没写进去: {LOADOUT_PATH}");
                return "穿戴表缺失, 未写入";
            }

            SerializedObject target = new SerializedObject(loadout);
            SetObject(target, LoadoutProp(slot), item);
            target.ApplyModifiedProperties();
            EditorUtility.SetDirty(loadout);
            return $"已穿到槽位 {slot}";
        }

        // 物品表里缺这件才追加, 已存在就不动顺序, 免得每次生成都重排一遍
        private int AppendToMaster(InventoryItemConfigSO item)
        {
            SerializedObject target = LoadMasterSerialized();
            if (target == null) return 0;

            SerializedProperty items = target.FindProperty(PROP_MASTER_ITEMS);
            if (items == null)
            {
                MissingField(PROP_MASTER_ITEMS, target);
                return 0;
            }

            for (int i = 0; i < items.arraySize; i++)
                if (items.GetArrayElementAtIndex(i).objectReferenceValue == item)
                    return 0;

            items.InsertArrayElementAtIndex(items.arraySize);
            items.GetArrayElementAtIndex(items.arraySize - 1).objectReferenceValue = item;
            target.ApplyModifiedProperties();
            EditorUtility.SetDirty(target.targetObject);
            return 1;
        }

        // 扫盘上的全部物品配置, 按 ItemId 排序整批刷进总配置物品表
        private void RebuildMasterItemTable()
        {
            SerializedObject target = LoadMasterSerialized();
            if (target == null) return;

            SerializedProperty items = target.FindProperty(PROP_MASTER_ITEMS);
            if (items == null)
            {
                MissingField(PROP_MASTER_ITEMS, target);
                return;
            }

            List<InventoryItemConfigSO> scanned = ScanItemConfigs();
            scanned.Sort((left, right) => left.ItemId.CompareTo(right.ItemId));
            ReportDuplicatedItemIds(scanned);

            items.arraySize = scanned.Count;
            for (int i = 0; i < scanned.Count; i++)
                items.GetArrayElementAtIndex(i).objectReferenceValue = scanned[i];

            target.ApplyModifiedProperties();
            EditorUtility.SetDirty(target.targetObject);
            AssetDatabase.SaveAssets();

            m_message = $"总配置物品表已按 ItemId 刷新, 共 {scanned.Count} 件";
            QLog.Info(m_message);
        }

        // 把表单回填成当前档位的真实配置, 该档不存在时顺着低一级抄, 都没有才用出厂值
        private void LoadForm()
        {
            InventoryContainerConfigSO container = LoadContainer(SelectedSlot, m_level);
            InventoryItemConfigSO item = LoadItem(SelectedSlot, m_level);

            if (container == null && item == null)
            {
                for (int level = m_level - 1; level >= 1 && container == null; level--)
                    container = LoadContainer(SelectedSlot, level);
                for (int level = m_level - 1; level >= 1 && item == null; level--)
                    item = LoadItem(SelectedSlot, level);

                // 参考低一级时不继承名字, 否则 Lv3 会顶着 Lv2 的显示名
                m_containerName = string.Empty;
                m_itemName = string.Empty;
            }
            else
            {
                m_containerName = container != null ? container.DisplayName : string.Empty;
                m_itemName = item != null ? item.DisplayName : string.Empty;
            }

            m_containerWidth = container != null ? container.Width : 8;
            m_containerHeight = container != null ? container.Height : 5;

            m_board.Load(container != null ? container.Mask : null, m_containerWidth, m_containerHeight);
            m_itemGridWidth = item != null ? item.Size.x : 3;
            m_itemGridHeight = item != null ? item.Size.y : 3;
            m_rarity = item != null ? item.Rarity : InventoryItemRarity.Common;
            m_unitValue = item != null ? item.UnitValue : 1000;
            m_unitWeight = item != null ? item.UnitWeight : 1f;
            m_icon = item != null ? item.Icon : null;
        }

        // 显示当前档位三份资产的现状, 包括是否会新建与绑定是否完整
        private void DrawCurrentState()
        {
            int slot = SelectedSlot;
            InventoryContainerConfigSO container = LoadContainer(slot, m_level);
            InventoryItemConfigSO item = LoadItem(slot, m_level);

            EditorGUILayout.LabelField($"当前资产 Lv{m_level}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("容器定义", container == null
                ? "不存在, 点生成会新建"
                : $"{container.DisplayName}  {container.Width}x{container.Height}  实心{InventoryShapeBoard.CountSolid(container.Mask)}");
            EditorGUILayout.LabelField("容器装备", item == null
                ? "不存在, 点生成会新建"
                : $"{item.ItemId}  {item.Rarity}  估价{item.UnitValue}  占格{item.Size.x}x{item.Size.y}");
            EditorGUILayout.LabelField("穿戴表", DescribeWorn(slot));
        }

        // 该槽位现在穿着哪个容器, 读穿戴表解析出来的定义而不是装备名
        private string DescribeWorn(int slot)
        {
            InventoryLoadoutConfigSO loadout = AssetDatabase.LoadAssetAtPath<InventoryLoadoutConfigSO>(LOADOUT_PATH);
            if (loadout == null) return $"缺少 {LOADOUT_PATH}";
            if (!loadout.TryGetContainer(slot, out InventoryContainerConfigSO worn)) return "空槽";

            return $"{worn.DisplayName}  {worn.Width}x{worn.Height}  实心{InventoryShapeBoard.CountSolid(worn.Mask)}";
        }

        // 读总配置并包成可序列化对象, 读不到时报错暴露而不是静默返回
        private SerializedObject LoadMasterSerialized()
        {
            InventoryConfigSO master = AssetDatabase.LoadAssetAtPath<InventoryConfigSO>(MASTER_CONFIG_PATH);
            if (master == null)
            {
                QLog.Error($"找不到背包总配置: {MASTER_CONFIG_PATH}");
                return null;
            }

            return new SerializedObject(master);
        }

        // 扫出物品目录下的全部物品配置, 同一份资产只保留一次
        private static List<InventoryItemConfigSO> ScanItemConfigs()
        {
            string[] guids = AssetDatabase.FindAssets("t:InventoryItemConfigSO", new[] { ITEM_ROOT_DIR });
            List<InventoryItemConfigSO> scanned = new List<InventoryItemConfigSO>(guids.Length);

            for (int i = 0; i < guids.Length; i++)
            {
                InventoryItemConfigSO item = AssetDatabase.LoadAssetAtPath<InventoryItemConfigSO>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (item != null && !scanned.Contains(item))
                    scanned.Add(item);
            }

            return scanned;
        }

        // 重复编号会让查表随机拿到另一件物品, 排序后相邻比对最省事
        private static void ReportDuplicatedItemIds(List<InventoryItemConfigSO> sortedItems)
        {
            for (int i = 1; i < sortedItems.Count; i++)
                if (sortedItems[i].ItemId == sortedItems[i - 1].ItemId)
                    QLog.Warning($"ItemId 重复：{sortedItems[i].ItemId} → {sortedItems[i].name} / {sortedItems[i - 1].name}");
        }

        // 资产存在就复用, 不存在先落盘再改, 保证拿到的是带 GUID 的正式对象
        private static T LoadOrCreate<T>(string assetPath, ref bool created) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null) return asset;

            asset = CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            created = true;
            return asset;
        }

        // 目录不存在时创建, 换台机器首次运行也不会卡在这
        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            string parent = folderPath.Substring(0, folderPath.LastIndexOf('/'));
            string name = Path.GetFileName(folderPath);
            AssetDatabase.CreateFolder(parent, name);
        }

        // 名字留空就用自动生成的, 避免空串进配置
        private static string ResolveName(string input, string autoName)
            => string.IsNullOrWhiteSpace(input) ? autoName : input.Trim();

        // 容器定义显示名: Lv1 直接用槽位名, 往上带等级
        private static string AutoContainerName(int slot, int level)
            => level <= 1 ? SlotNameOf(slot) : $"{SlotNameOf(slot)} Lv{level}";

        // 装备显示名统一带等级, 和资产文件名对得上
        private static string AutoItemName(int slot, int level) => $"{SlotNameOf(slot)}Lv{level}";

        private static string ContainerPath(int slot, int level)
            => $"{CONTAINER_DIR}/ContainerConfig_{SlotNameOf(slot)}Lv{level}.asset";

        private static string ItemPath(int slot, int level)
            => $"{ITEM_DIR}/ItemConfig_{ItemBaseOf(slot) + level - 1}_{SlotNameOf(slot)}Lv{level}.asset";

        private InventoryContainerConfigSO LoadContainer(int slot, int level)
            => AssetDatabase.LoadAssetAtPath<InventoryContainerConfigSO>(ContainerPath(slot, level));

        private InventoryItemConfigSO LoadItem(int slot, int level)
            => AssetDatabase.LoadAssetAtPath<InventoryItemConfigSO>(ItemPath(slot, level));

        // 槽位号对应号段起点: 背包 6000 / 胸挂 6100 / 安全箱 6200
        private static int ItemBaseOf(int slot)
        {
            return slot switch
            {
                InventoryContainerIds.BACKPACK => BACKPACK_ITEM_BASE,
                InventoryContainerIds.RIG => RIG_ITEM_BASE,
                InventoryContainerIds.SAFEBOX => SAFEBOX_ITEM_BASE,
                _ => 0
            };
        }

        private static string SlotNameOf(int slot)
        {
            return slot switch
            {
                InventoryContainerIds.BACKPACK => "背包",
                InventoryContainerIds.RIG => "胸挂",
                InventoryContainerIds.SAFEBOX => "安全箱",
                _ => "未知槽位"
            };
        }

        // 槽位号对应穿戴表里的引用字段名, 口袋槽引用容器定义所以不在这里出现
        private static string LoadoutProp(int slot)
        {
            return slot switch
            {
                InventoryContainerIds.BACKPACK => "BackpackValue",
                InventoryContainerIds.RIG => "RigValue",
                InventoryContainerIds.SAFEBOX => "SafeBoxValue",
                _ => string.Empty
            };
        }

        // 弹窗用的槽位名列表, 下标与 m_slotIds 对齐
        private static string[] SlotNames()
        {
            string[] names = new string[m_slotIds.Length];
            for (int i = 0; i < m_slotIds.Length; i++)
                names[i] = SlotNameOf(m_slotIds[i]);

            return names;
        }

        private static void SetInt(SerializedObject target, string name, int value)
        {
            SerializedProperty property = target.FindProperty(name);
            if (property == null)
            {
                MissingField(name, target);
                return;
            }

            property.intValue = value;
        }

        private static void SetFloat(SerializedObject target, string name, float value)
        {
            SerializedProperty property = target.FindProperty(name);
            if (property == null)
            {
                MissingField(name, target);
                return;
            }

            property.floatValue = value;
        }

        private static void SetString(SerializedObject target, string name, string value)
        {
            SerializedProperty property = target.FindProperty(name);
            if (property == null)
            {
                MissingField(name, target);
                return;
            }

            property.stringValue = value;
        }


        private static void SetStringArray(SerializedObject target, string name, string[] value)
        {
            SerializedProperty property = target.FindProperty(name);
            if (property == null)
            {
                MissingField(name, target);
                return;
            }

            property.ClearArray();
            property.arraySize = value.Length;

            for (int i = 0; i < value.Length; i++)
                property.GetArrayElementAtIndex(i).stringValue = value[i];
        }

        private static void SetBool(SerializedObject target, string name, bool value)
        {
            SerializedProperty property = target.FindProperty(name);
            if (property == null)
            {
                MissingField(name, target);
                return;
            }

            property.boolValue = value;
        }

        // 枚举序列化存的本来就是档位值, 直接写整数, 档位改名也不会写错
        private static void SetEnum(SerializedObject target, string name, InventoryItemRarity value)
        {
            SerializedProperty property = target.FindProperty(name);
            if (property == null)
            {
                MissingField(name, target);
                return;
            }

            property.intValue = (int)value;
        }

        private static void SetObject(SerializedObject target, string name, Object value)
        {
            if (string.IsNullOrEmpty(name))
            {
                QLog.Error($"写入字段名为空, 目标：{target.targetObject.GetType().Name}");
                return;
            }

            SerializedProperty property = target.FindProperty(name);
            if (property == null)
            {
                MissingField(name, target);
                return;
            }

            property.objectReferenceValue = value;
        }

        // 字段名对不上说明配置类改过名, 必须报错暴露而不是静默跳过
        private static void MissingField(string name, SerializedObject target)
            => QLog.Error($"配置类 {target.targetObject.GetType().Name} 没有字段 {name}, 请同步工具里的字段名");
    }
}