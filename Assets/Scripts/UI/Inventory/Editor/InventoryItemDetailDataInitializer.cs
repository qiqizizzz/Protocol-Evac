/*
* ┌──────────────────────────────────┐
* │  描    述: 物品详情示例数据初始化器, 按物品种类补齐描述与专属属性
* │  类    名: InventoryItemDetailDataInitializer.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.Inventory.Core.Config;
using Module.Inventory.Core.Config.Type;
using UnityEditor;
using UnityEngine;
using Utils.log;

namespace UI.Inventory.Editor
{
    // 只写详情字段, 不改物品编号、价格、重量、图标、容器与穿戴引用
    public static class InventoryItemDetailDataInitializer
    {
        private const string ITEM_ROOT = "Assets/Config/Inventory/Item";
        private const string PROP_DESCRIPTION = "DescriptionValue";
        private const string PROP_DETAIL_ENTRIES = "DetailEntriesValue";
        private const string PROP_ENTRY_LABEL = "LabelValue";
        private const string PROP_ENTRY_VALUE = "ValueValue";

        private static readonly Dictionary<int, DetailSeed> m_seedData = CreateSeedData();

        /// <summary>
        /// 为缺少详情内容的正式物品补齐示例描述与分类属性
        /// </summary>
        [MenuItem("Tools/背包7a/12.补齐物品详情示例数据")]
        public static void PopulateMissingDetailContent()
        {
            string[] guids = AssetDatabase.FindAssets("t:InventoryItemConfigSO", new[] { ITEM_ROOT });
            int updatedCount = 0;
            int missingSeedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                InventoryItemConfigSO config = AssetDatabase.LoadAssetAtPath<InventoryItemConfigSO>(path);
                if (config == null)
                {
                    QLog.Error("物品详情初始化失败, 配置无法加载: " + path);
                    continue;
                }

                if (!m_seedData.TryGetValue(config.ItemId, out DetailSeed seed))
                {
                    missingSeedCount++;
                    QLog.Warning($"物品详情初始化跳过, 未配置示例数据: {config.ItemId} {config.DisplayName}");
                    continue;
                }

                if (ApplyMissingContent(config, seed))
                    updatedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            QLog.Info($"物品详情示例数据补齐完成: 扫描 {guids.Length} 件, 更新 {updatedCount} 件, 未配置 {missingSeedCount} 件");
        }

        // 逐项补齐空描述与空属性, 已存在的用户内容保持原样
        private static bool ApplyMissingContent(InventoryItemConfigSO config, DetailSeed seed)
        {
            SerializedObject serialized = new SerializedObject(config);
            SerializedProperty description = serialized.FindProperty(PROP_DESCRIPTION);
            SerializedProperty entries = serialized.FindProperty(PROP_DETAIL_ENTRIES);

            if (description == null || entries == null)
            {
                QLog.Error($"物品详情初始化失败, 配置字段缺失: {config.ItemId} {config.DisplayName}");
                return false;
            }

            bool changed = false;
            if (string.IsNullOrWhiteSpace(description.stringValue) &&
                !string.IsNullOrWhiteSpace(seed.Description))
            {
                description.stringValue = seed.Description;
                changed = true;
            }

            bool shouldHaveEntries = config.ItemType != InventoryItemType.Valuable;
            if ((shouldHaveEntries && entries.arraySize == 0) ||
                (!shouldHaveEntries && entries.arraySize > 0))
            {
                entries.arraySize = shouldHaveEntries ? seed.Entries.Length : 0;
                for (int i = 0; i < entries.arraySize; i++)
                {
                    SerializedProperty element = entries.GetArrayElementAtIndex(i);
                    SerializedProperty label = element.FindPropertyRelative(PROP_ENTRY_LABEL);
                    SerializedProperty value = element.FindPropertyRelative(PROP_ENTRY_VALUE);
                    if (label == null || value == null)
                    {
                        QLog.Error($"物品详情初始化失败, 条目字段缺失: {config.ItemId} {config.DisplayName}");
                        return false;
                    }

                    label.stringValue = seed.Entries[i].Label;
                    value.stringValue = seed.Entries[i].Value;
                }

                changed = true;
            }

            if (!changed) return false;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            return true;
        }

        // 按物品编号整理示例详情, 贵重品只保留描述
        private static Dictionary<int, DetailSeed> CreateSeedData()
        {
            return new Dictionary<int, DetailSeed>
            {
                [1000] = Seed("5.45×39mm 口径的模块化突击步枪，后坐力平稳，适合中近距离持续压制。",
                    Entry("武器类型", "突击步枪"),
                    Entry("口径", "5.45×39mm"),
                    Entry("射击模式", "单发 / 连发"),
                    Entry("有效射程", "400m"),
                    Entry("弹匣容量", "30 发")),
                [1001] = Seed("泵动式霰弹枪，近距离威力极高，适合狭窄空间和破门作战。",
                    Entry("武器类型", "霰弹枪"),
                    Entry("口径", "12 号霰弹"),
                    Entry("射击模式", "泵动单发"),
                    Entry("有效射程", "60m"),
                    Entry("弹匣容量", "8 发")),
                [1002] = Seed("紧凑可靠的半自动手枪，便于快速拔枪，作为主武器的近身补充。",
                    Entry("武器类型", "手枪"),
                    Entry("口径", "9×19mm"),
                    Entry("射击模式", "半自动"),
                    Entry("有效射程", "80m"),
                    Entry("弹匣容量", "17 发")),
                [1003] = Seed("轻便的战术匕首，可用于近战自保和快速处理绳索、织物等障碍。",
                    Entry("武器类型", "近战刀械"),
                    Entry("刀身长度", "18cm"),
                    Entry("攻击方式", "挥砍 / 刺击"),
                    Entry("耐久", "100")),
                [2000] = Seed("标准步枪弹药，性能均衡，适合多数中口径突击步枪和精确射手步枪。",
                    Entry("口径", "5.56×45mm"),
                    Entry("弹头类型", "全金属被甲弹"),
                    Entry("穿透等级", "1 级"),
                    Entry("适配武器", "突击步枪"),
                    Entry("堆叠上限", "60 发")),
                [2001] = Seed("常见手枪弹药，停止作用适中，补给稳定，适合长时间搜索行动。",
                    Entry("口径", "9×19mm"),
                    Entry("弹头类型", "圆头弹"),
                    Entry("穿透等级", "1 级"),
                    Entry("适配武器", "手枪"),
                    Entry("堆叠上限", "30 发")),
                [2002] = Seed("预装填步枪弹匣，可直接装入对应武器，减少交火中的换弹停顿。",
                    Entry("口径", "5.56×45mm"),
                    Entry("弹匣容量", "30 发"),
                    Entry("穿透等级", "2 级"),
                    Entry("适配武器", "突击步枪"),
                    Entry("堆叠上限", "5 个")),
                [3000] = Seed("覆盖胸腹要害的基础防弹衣，能挡住常见手枪弹和低威力破片。",
                    Entry("防护等级", "II 级"),
                    Entry("耐久", "60"),
                    Entry("覆盖部位", "胸部 / 腹部"),
                    Entry("移动惩罚", "-2%")),
                [3001] = Seed("带缓冲内衬的战术头盔，重点保护头部，并预留面罩与耳机接口。",
                    Entry("防护等级", "II 级"),
                    Entry("耐久", "45"),
                    Entry("覆盖部位", "头部"),
                    Entry("移动惩罚", "-1%"),
                    Entry("扩展接口", "面罩 / 耳机")),
                [3002] = Seed("透明防护面罩，可降低破片与粉尘伤害，但会轻微影响周边视野。",
                    Entry("防护等级", "I 级"),
                    Entry("耐久", "25"),
                    Entry("覆盖部位", "面部"),
                    Entry("视野影响", "轻微")),
                [4000] = Seed("压缩血包，可快速补充失血，适合在脱离交火后立即处理伤势。",
                    Entry("恢复量", "45"),
                    Entry("止血", "是"),
                    Entry("使用时间", "2.5 秒"),
                    Entry("使用次数", "1 次")),
                [4001] = Seed("便携绷带，主要处理轻伤和持续出血，占用空间小。",
                    Entry("恢复量", "12"),
                    Entry("止血", "是"),
                    Entry("使用时间", "2 秒"),
                    Entry("使用次数", "1 次")),
                [4002] = Seed("完整急救包，包含止血、消毒与包扎用品，可处理较严重的复合伤。",
                    Entry("恢复量", "80"),
                    Entry("止血", "是"),
                    Entry("使用时间", "5 秒"),
                    Entry("使用次数", "1 次")),
                [5000] = Seed("从废弃电动车中拆出的高密度动力电池，外壳已经老化，但内部电芯仍有回收价值。黑市会为这种稀有储能部件支付高价。"),
                [5001] = Seed("军规手持电台，频道加密模块仍然完好。对收藏家和需要监听旧频段的行动者来说，这件设备比它表面的磨损更值钱。"),
                [5002] = Seed("一份标注了旧城区地下管线和撤离通道的军用地图。纸面已经泛黄，但其中有些路线并未出现在现行数据库中。"),
                [6000] = Seed("轻型双肩背包，容量有限，适合短程搜索行动。",
                    Entry("内部容量", "2x3（5 实心格）"),
                    Entry("布局", "异形口袋"),
                    Entry("收纳类型", "背包"),
                    Entry("移动惩罚", "-3%")),
                [6001] = Seed("中型战术背包，在容量与机动性之间取得了较好的平衡。",
                    Entry("内部容量", "4x3（12 格）"),
                    Entry("布局", "矩形"),
                    Entry("收纳类型", "背包"),
                    Entry("移动惩罚", "-5%")),
                [6002] = Seed("大容量远征背包，可携带大量战利品和补给，但会明显拖慢行动。",
                    Entry("内部容量", "5x5（25 格）"),
                    Entry("布局", "矩形"),
                    Entry("收纳类型", "背包"),
                    Entry("移动惩罚", "-8%")),
                [6100] = Seed("基础弹挂胸挂，分区明确，便于快速取用弹匣和医疗物品。",
                    Entry("内部容量", "4x3（10 实心格）"),
                    Entry("布局", "分区异形"),
                    Entry("收纳类型", "胸挂"),
                    Entry("移动惩罚", "-2%")),
                [6101] = Seed("扩容型弹挂胸挂，增加了一排独立分区，适合长时间作战。",
                    Entry("内部容量", "4x4（14 实心格）"),
                    Entry("布局", "分区异形"),
                    Entry("收纳类型", "胸挂"),
                    Entry("移动惩罚", "-3%")),
                [6200] = Seed("小型安全箱，可保护少量贵重物品，装备后仍可正常使用其他容器。",
                    Entry("内部容量", "2x1（2 格）"),
                    Entry("布局", "矩形"),
                    Entry("收纳类型", "安全箱"),
                    Entry("安全保护", "死亡不掉落")),
                [6201] = Seed("升级型安全箱，在相同携带负担下提供更多受保护空间。",
                    Entry("内部容量", "2x2（4 格）"),
                    Entry("布局", "矩形"),
                    Entry("收纳类型", "安全箱"),
                    Entry("安全保护", "死亡不掉落")),
                [7001] = Seed("密封肉罐头，开罐即食，能提供稳定的能量补给。",
                    Entry("恢复效果", "能量 +35 / 水分 +10"),
                    Entry("使用时间", "3 秒"),
                    Entry("保质期", "24 个月"),
                    Entry("使用次数", "1 次")),
                [7002] = Seed("密封饮用水，适合在长距离移动后快速补充水分。",
                    Entry("恢复效果", "水分 +40"),
                    Entry("容量", "500ml"),
                    Entry("使用时间", "2 秒"),
                    Entry("使用次数", "1 次")),
                [7003] = Seed("自带加热包的口粮，需要短暂等待即可食用，能提供较高的持续能量。",
                    Entry("恢复效果", "能量 +60 / 水分 +15"),
                    Entry("使用时间", "4 秒"),
                    Entry("保质期", "18 个月"),
                    Entry("使用次数", "1 次")),
                [8001] = Seed("便携维修工具组，包含常用扳手、螺丝刀和清洁附件。",
                    Entry("工具类型", "维修工具"),
                    Entry("耐久", "80"),
                    Entry("使用场景", "武器维护 / 简易维修"),
                    Entry("占用双手", "否")),
                [8002] = Seed("耐冲击战术手电，适合室内搜索和夜间行进。",
                    Entry("工具类型", "照明设备"),
                    Entry("亮度", "320 流明"),
                    Entry("续航", "6 小时"),
                    Entry("安装方式", "手持 / 导轨")),
                [8003] = Seed("加固油桶，可储存燃料或作为临时容器使用，桶身带有防静电涂层。",
                    Entry("工具类型", "燃料容器"),
                    Entry("容量", "20L"),
                    Entry("耐久", "100"),
                    Entry("易燃", "是")),
                [9001] = Seed("小型红点瞄具，提供清晰的单点瞄准画面，适合快速接敌。",
                    Entry("适配类型", "突击步枪 / 冲锋枪"),
                    Entry("安装槽位", "顶部导轨"),
                    Entry("瞄准倍率", "1x"),
                    Entry("效果", "提升快速瞄准速度")),
                [9002] = Seed("可调节战术枪托，能改善抵肩稳定性并降低连续射击时的后坐。",
                    Entry("适配类型", "突击步枪 / 霰弹枪"),
                    Entry("安装槽位", "枪托"),
                    Entry("效果", "后坐力 -18%"),
                    Entry("人机工效", "+5")),
                [9003] = Seed("标准枪口消焰器，可减弱枪口火光并略微改善连续射击稳定性。",
                    Entry("适配类型", "步枪 / 手枪"),
                    Entry("安装槽位", "枪口"),
                    Entry("效果", "后坐力 -8% / 抑制枪口火光"),
                    Entry("耐久", "60")),
            };
        }

        // 创建一个物品详情示例
        private static DetailSeed Seed(string description, params DetailEntrySeed[] entries)
            => new DetailSeed(description, entries);

        // 创建一个详情属性条目
        private static DetailEntrySeed Entry(string label, string value)
            => new DetailEntrySeed(label, value);

        private sealed class DetailSeed
        {
            public string Description { get; }
            public DetailEntrySeed[] Entries { get; }

            // 保存一件物品的描述与属性条目
            public DetailSeed(string description, DetailEntrySeed[] entries)
            {
                Description = description;
                Entries = entries;
            }
        }

        private sealed class DetailEntrySeed
        {
            public string Label { get; }
            public string Value { get; }

            // 保存一条详情属性的标签与值
            public DetailEntrySeed(string label, string value)
            {
                Label = label;
                Value = value;
            }
        }
    }
}
