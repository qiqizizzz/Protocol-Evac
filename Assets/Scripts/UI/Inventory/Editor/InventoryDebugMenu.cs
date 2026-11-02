/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包 7a 编辑器调试菜单(AI 自留测试载具, 非生产代码)                    
 * │  类    名: InventoryDebugMenu.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Reflection;
using Framework.QTower.MVVM.View;
using Module.Inventory.Core.Config;
using Module.Inventory.Core.Defines;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Inventory.Editor
{
    // 7a 验收载具: 只依赖已定稿的 Model/Registry/框架, 面板与视图用反射取, 敲完 6~9 之前不会破编译
    public static class InventoryDebugMenu
    {
        private const string LOG = "[InventoryDebugMenu] ";
        private const string PANEL_VIEW_TYPE_NAME = "UI.Inventory.View.InventoryPanelView";
        private const string MODEL_TYPE_NAME = "UI.Inventory.IInventoryModel";
        private const string MISSING = "(缺失)";

        // 一键搭好 7a 验收场景: Canvas + EventSystem + MvvmHost
        [MenuItem("Tools/背包7a/1.搭建测试场景")]
        public static void SetupTestScene()
        {
            if (UnityEngine.Object.FindObjectOfType<Canvas>() == null)
            {
                GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Canvas canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            if (UnityEngine.Object.FindObjectOfType<MvvmHost>() == null)
                new GameObject("MvvmHost", typeof(MvvmHost));

            Debug.Log(LOG + "测试场景就绪(Canvas/EventSystem/MvvmHost), 进入 Play 后依次点 2/3/4(先点物品格再点 5,0)/5");
        }

        // 打开背包面板: 反射取 InventoryPanelView, 未敲完时给可读提示
        [MenuItem("Tools/背包7a/2.打开背包面板")]
        public static void OpenPanel()
        {
            MvvmRoot root = EnsureRoot();
            if (root == null) return;

            Type panelType = FindType(PANEL_VIEW_TYPE_NAME);
            if (panelType == null)
            {
                Debug.LogWarning(LOG + "InventoryPanelView 还不存在, 请先敲完文件 6~9");
                return;
            }

            MethodInfo showAsync = root.Windows.GetType().GetMethod("ShowAsync").MakeGenericMethod(panelType);
            object task = showAsync.Invoke(root.Windows, new object[] { new object[0] });
            AttachNoOp(task);

            Debug.Log(LOG + "已请求打开背包面板(地址 UIInventoryPanel, 需已入 Addressables)");
        }

        // 探针放三件物品: 5x2 步枪 / 3x3 护甲 / 30 发子弹, 校验 Version 驱动的自动重绘
        [MenuItem("Tools/背包7a/3.放入测试物品")]
        public static void AddTestItems()
        {
            RegisterAllConfigs();
            object model = ResolveModel();
            if (model == null)
            {
                Debug.LogWarning(LOG + "IInventoryModel 尚未注册, 先打开面板或确认文件 9 已敲完");
                return;
            }

            Debug.Log(LOG + "步枪 -> " + InvokeAdd(model, 1000, 1, new Vector2Int(0, 0)));
            Debug.Log(LOG + "护甲 -> " + InvokeAdd(model, 3000, 1, new Vector2Int(0, 3)));
            Debug.Log(LOG + "子弹x30 -> " + InvokeAdd(model, 2000, 30, new Vector2Int(5, 0)));
        }

        // 链路探针: 直接调用面板 VM 的格子交互入口, 等价于玩家点 (5,0)
        [MenuItem("Tools/背包7a/4.点击格子(5,0)")]
        public static void ClickSlotProbe()
        {
            object vm = ResolvePanelViewModel();
            if (vm == null) return;

            MethodInfo click = vm.GetType().GetMethod("InteractAt");
            if (click == null)
            {
                Debug.LogWarning(LOG + "InventoryViewModel 还没有 InteractAt, 请先敲文件 5");
                return;
            }

            click.Invoke(vm, new object[] { new Vector2Int(5, 0) });
            Debug.Log(LOG + "已在格子 (5,0) 发起交互, 观察高亮与提示变化");
        }

        // 读取面板 VM 的提示/选中/高亮/重量统计, 全部走反射, 缺成员报 "(缺失)" 而不抛异常
        [MenuItem("Tools/背包7a/5.打印快照状态")]
        public static void DumpState()
        {
            object vm = ResolvePanelViewModel();
            if (vm == null) return;

            Type vmType = vm.GetType();
            Debug.Log(LOG + "HintKind = " + ReadValue(vmType, vm, "HintKind")
                + ", LastMoveResult = " + ReadPlain(vmType, vm, "LastMoveResult")
                + ", SelectedInstanceId = " + ReadValue(vmType, vm, "SelectedInstanceId")
                + ", CanRotate = " + ReadValue(vmType, vm, "CanRotate"));

            object snapshot = ReadValue(vmType, vm, "Snapshot");
            if (!MISSING.Equals(snapshot))
            {
                Type snapType = snapshot.GetType();
                object items = ReadPlain(snapType, snapshot, "Items");
                Debug.Log(LOG + "Weight = " + ReadPlain(snapType, snapshot, "TotalWeight")
                    + "/" + ReadPlain(snapType, snapshot, "WeightLimit")
                    + ", Version = " + ReadPlain(snapType, snapshot, "Version")
                    + ", ItemCount = " + (MISSING.Equals(items) ? MISSING : ReadPlain(items.GetType(), items, "Count")));
            }

            if (ReadPlain(vmType, vm, "SlotCount") is not int slotCount)
            {
                Debug.LogWarning(LOG + "InventoryViewModel 还是空壳, 请先敲文件 5");
                return;
            }

            MethodInfo getSlot = vmType.GetMethod("GetSlot");
            int highlightedCount = 0;
            for (int i = 0; getSlot != null && i < slotCount; i++)
            {
                object slotVm = getSlot.Invoke(vm, new object[] { i });
                if (ReadValue(slotVm.GetType(), slotVm, "IsSelected") is true) highlightedCount++;
            }

            Debug.Log(LOG + "格子子VM数量 = " + slotCount + ", 高亮格子数 = " + highlightedCount);
        }

        // 链路探针: 旋转当前选中物品, 等价于玩家点旋转按钮
        [MenuItem("Tools/背包7a/6.旋转选中物品")]
        public static void RotateSelectedProbe()
        {
            object vm = ResolvePanelViewModel();
            if (vm == null) return;

            MethodInfo rotate = vm.GetType().GetMethod("RotateSelectedItem");
            if (rotate == null)
            {
                Debug.LogWarning(LOG + "InventoryViewModel 还没有 RotateSelectedItem, 请先敲文件 5");
                return;
            }

            rotate.Invoke(vm, null);
            Debug.Log(LOG + "已请求旋转选中物品, 观察提示语义与结果码");
        }

        // 把全部物品配置登记进 ItemRegistry(正式加载路径等配置表系统, 这里只是编辑器捷径)
        private static void RegisterAllConfigs()
        {
            string[] guids = AssetDatabase.FindAssets("t:InventoryItemConfigSO", new[] { "Assets/Config/Inventory/Item" });
            for (int i = 0; i < guids.Length; i++)
            {
                InventoryItemConfigSO config = AssetDatabase.LoadAssetAtPath<InventoryItemConfigSO>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (config == null) continue;

                ItemRegistry.Register(config);
            }
        }

        // 取面板上的 ViewModel, 任一环节缺失都给可读提示
        private static object ResolvePanelViewModel()
        {
            MvvmRoot root = EnsureRoot();
            if (root == null) return null;

            Type panelType = FindType(PANEL_VIEW_TYPE_NAME);
            if (panelType == null)
            {
                Debug.LogWarning(LOG + "InventoryPanelView 还不存在, 请先敲完文件 6~9");
                return null;
            }

            Component[] components = root.ViewRoot.GetComponentsInChildren(panelType, true);
            if (components.Length == 0)
            {
                Debug.LogWarning(LOG + "面板未打开, 先点 2");
                return null;
            }

            object viewModel = panelType.GetProperty("ViewModel").GetValue(components[0]);
            if (viewModel == null) Debug.LogWarning(LOG + "面板 ViewModel 为空");
            return viewModel;
        }

        // 取运行根, 没有就现场跑一次装配
        private static MvvmRoot EnsureRoot()
        {
            if (MvvmRoot.Instance != null) return MvvmRoot.Instance;

            MvvmHost host = UnityEngine.Object.FindObjectOfType<MvvmHost>();
            if (host == null)
            {
                Debug.LogWarning(LOG + "场景里没有 MvvmHost, 先点 1 并进 Play");
                return null;
            }

            return host.Run();
        }

        // 反射解析已注册的背包模型
        private static object ResolveModel()
        {
            MvvmRoot root = EnsureRoot();
            if (root == null) return null;

            Type modelType = FindType(MODEL_TYPE_NAME);
            if (modelType == null) return null;

            MethodInfo tryGet = root.Models.GetType().GetMethod("TryGet").MakeGenericMethod(modelType);
            object[] args = { null };
            return (bool)tryGet.Invoke(root.Models, args) ? args[0] : null;
        }

        // 反射调用 TryAddItem(containerId, itemId, amount, cell)
        private static object InvokeAdd(object model, int itemId, int amount, Vector2Int cell)
        {
            MethodInfo add = model.GetType().GetMethod("TryAddItem");
            return add.Invoke(model, new object[] { InventoryContainerIds.BACKPACK, itemId, amount, cell });
        }
        // 读取 ReactiveProperty 形态属性的 Value, 反射取不到时返回 "(缺失)"
        private static object ReadValue(Type type, object target, string propertyName)
        {
            object holder = ReadPlain(type, target, propertyName);
            if (holder == null || MISSING.Equals(holder)) return MISSING;

            PropertyInfo value = holder.GetType().GetProperty("Value");
            return value == null ? MISSING : value.GetValue(holder) ?? "null";
        }

        // 读取普通属性值, 反射取不到时返回 "(缺失)"
        private static object ReadPlain(Type type, object target, string propertyName)
        {
            PropertyInfo prop = type.GetProperty(propertyName);
            return prop == null ? MISSING : prop.GetValue(target) ?? "null";
        }


        // 全程序集精确找类型, 未敲完的文件不在时返回 null 而不是抛异常
        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null) return type;
            }

            return null;
        }

        // 给返回的 UniTask 挂一个空延续, 防止编辑器菜单里被丢弃
        private static void AttachNoOp(object task)
        {
            MethodInfo continueWith = task.GetType().GetMethod("ContinueWith",
                BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Action) }, null);
            continueWith?.Invoke(task, new object[] { (Action)(() => { }) });
        }
    }
}
