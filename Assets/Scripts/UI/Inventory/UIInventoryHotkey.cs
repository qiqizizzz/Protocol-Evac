/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包快捷键宿主, 运行期自建常驻, 统一转发开关面板, 搜刮交互与拖拽控制输入
 * │  类    名: UIInventoryHotkey.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Cysharp.Threading.Tasks;
using Framework.QTower.Common.Defines;
using Framework.QTower.Event;
using Framework.QTower.MVVM.View;
using Module.Inventory.Interaction;
using Module.Inventory.Presentation;
using Module.Player.Core;
using Module.ShootingPlayer.Input;
using UI.Inventory.View;
using UnityEngine;
using UnityEngine.InputSystem;
using Utils.log;

namespace UI.Inventory
{
    /// <summary>
    /// 背包快捷键宿主：场景加载后自动常驻，按 B 在显示与隐藏之间切换战备面板
    /// 同时提供静态丢弃入口，让右键菜单与左键双击共用同一条落地链路
    /// </summary>
    public sealed class UIInventoryHotkey : MonoBehaviour
    {
        // 当前战备面板由哪个入口打开, 供 H 只关闭自己打开的垃圾堆
        private enum PanelOpenSource
        {
            None,
            Inventory,
            Interact,
            GroundPile
        }

        // 丢弃落点沿玩家身前推进的距离(米), 相机就在头顶, 丢在脚下的堆不在视野里
        private const float DROP_FORWARD_DISTANCE = 0.9f;

        // 高亮扫描不跟着渲染帧跑, 远处容器状态用低频刷新已足够
        private const float LOOT_HIGHLIGHT_SCAN_INTERVAL = 0.18f;

        [SerializeField] private InteractionQueryConfig LootBoxQuery =
            new InteractionQueryConfig(2.5f, 45f, 1.2f, 0.4f, 0.6f);

        [Tooltip("物资箱可开启描边提示半径(米), 只影响视觉高亮, 不改变 F 交互范围")]
        [SerializeField, Min(0f)] private float LootBoxHighlightDistance = 12f;

        private static UIInventoryHotkey m_instance;
        private ShootingPlayerInputActions m_inputActions;
        private LootBoxInteraction m_lootInteraction;
        private LootBoxHighlightController m_lootHighlight;
        private GroundDropInteraction m_dropInteraction;
        private PlayerController m_playerController;
        private bool m_hasReportedMissingPlayer;
        private bool m_hasLootFocus;
        private PanelOpenSource m_panelOpenSource;
        private float m_nextLootHighlightScanTime;

        /// <summary>
        /// 场景加载后自建常驻宿主
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (m_instance != null) return;

            GameObject host = new GameObject(nameof(UIInventoryHotkey));
            Object.DontDestroyOnLoad(host);
            m_instance = host.AddComponent<UIInventoryHotkey>();
        }

        private void Awake()
        {
            m_inputActions = new ShootingPlayerInputActions();
            m_lootInteraction = new LootBoxInteraction();
            m_lootHighlight = new LootBoxHighlightController();
        }

        private void OnEnable()
        {
            m_inputActions.UI.ToggleInventory.performed += OnToggleInventory;
            m_inputActions.UI.Interact.performed += OnInteract;
            m_inputActions.UI.InteractGroundPile.performed += OnInteractGroundPile;
            m_inputActions.UI.Cancel.performed += OnCancel;
            m_inputActions.UI.Rotate.performed += OnRotateDraggedItem;
            m_inputActions.UI.CancelDrag.performed += OnCancelDraggedItemBySecondaryButton;
            m_inputActions.UI.Enable();
        }

        private void Update()
        {
            if (Time.unscaledTime < m_nextLootHighlightScanTime) return;

            m_nextLootHighlightScanTime = Time.unscaledTime + LOOT_HIGHLIGHT_SCAN_INTERVAL;
            if (IsEquipmentPanelVisible())
            {
                m_lootHighlight.HideAll();
                SetLootFocus(null);
                return;
            }

            if (!TryResolvePlayer(out PlayerController player))
            {
                m_lootHighlight.HideAll();
                SetLootFocus(null);
                return;
            }

            player.ResolveInteractionQuery(out Vector3 origin, out Vector3 forward, out LayerMask obstacleMask);
            LootBoxController focused =
                m_lootHighlight.Refresh(origin, forward, LootBoxQuery, obstacleMask, LootBoxHighlightDistance);
            SetLootFocus(focused);
        }

        private void OnDisable()
        {
            m_lootHighlight.HideAll();
            SetLootFocus(null);
            m_inputActions.UI.ToggleInventory.performed -= OnToggleInventory;
            m_inputActions.UI.Interact.performed -= OnInteract;
            m_inputActions.UI.InteractGroundPile.performed -= OnInteractGroundPile;
            m_inputActions.UI.Cancel.performed -= OnCancel;
            m_inputActions.UI.Rotate.performed -= OnRotateDraggedItem;
            m_inputActions.UI.CancelDrag.performed -= OnCancelDraggedItemBySecondaryButton;
            m_inputActions.UI.Disable();
        }

        private void OnDestroy()
        {
            m_lootHighlight.HideAll();
            SetLootFocus(null);
            m_inputActions.Dispose();
            if (m_instance == this) m_instance = null;
        }

        // ==================== Public Function ====================
        /// <summary>
        /// 把玩家背包或物资箱里的物品丢到脚边：落点取玩家位置, 菜单与双击都走这一个入口
        /// </summary>
        /// <param name="containerId">物品当前所在容器号</param>
        /// <param name="instanceId">要丢弃的物品实例号</param>
        /// <returns>是否丢弃成功</returns>
        public static bool TryDropItemAtPlayer(int containerId, int instanceId)
        {
            if (m_instance == null)
            {
                QLog.Error("丢弃宿主尚未常驻, 无法定位落点");
                return false;
            }

            if (!m_instance.TryResolvePlayerPosition(out Vector3 playerPos)) return false;

            // 统一往前推一段再落地, 关掉面板后转身就能看到自己丢的东西
            Vector3 origin = playerPos + ResolveDropForward(m_instance.m_playerController.transform) *
                DROP_FORWARD_DISTANCE;

            m_instance.m_dropInteraction ??= new GroundDropInteraction();

            if (!m_instance.m_dropInteraction.TryDrop(containerId, instanceId, origin)) return false;

            // 从地面堆上丢东西会把堆掏空, 顺手收掉右侧那张空壳; 物资箱搬空仍然保留载体
            CloseLootCarrierIfRemoved(containerId);
            return true;
        }

        /// <summary>
        /// 切换战备面板：正在显示则隐藏(视图模型状态保留)，否则请求打开
        /// </summary>
        public void TogglePanel()
        {
            MvvmRoot root = MvvmRoot.Instance;
            if (root == null)
            {
                QLog.Error($"场景缺少 {nameof(MvvmHost)}, 战备面板无法打开");
                return;
            }

            if (root.Windows.TryGet<UIEquipmentPanel>(out UIEquipmentPanel panel) && panel.IsVisible)
            {
                HideEquipmentPanel(root);
                return;
            }

            m_panelOpenSource = PanelOpenSource.Inventory;
            root.Windows.ShowAsync<UIEquipmentPanel>().Forget();
        }

        // ==================== Private Function ====================
        // 面板开着且右侧正显示这只容器时, 让它复核容器是否已被收回
        private static void CloseLootCarrierIfRemoved(int containerId)
        {
            MvvmRoot root = MvvmRoot.Instance;
            if (root == null) return;
            if (!root.Windows.TryGet(out UIEquipmentPanel panel) || !panel.IsVisible) return;

            panel.ViewModel.CloseLootContainerIfRemoved(containerId);
        }

        // 背包面板与独立交互面板都会遮住世界, 打开期间不再刷新描边
        private static bool IsEquipmentPanelVisible()
        {
            MvvmRoot root = MvvmRoot.Instance;
            return root != null && root.Windows.TryGet(out UIEquipmentPanel panel) && panel.IsVisible;
        }

        // 响应背包开关快捷键
        private void OnToggleInventory(InputAction.CallbackContext context)
        {
            TogglePanel();
        }

        // 响应通用交互快捷键, 原点用玩家胸口, 朝向用相机水平前方
        private void OnInteract(InputAction.CallbackContext context)
        {
            if (!TryResolvePlayer(out PlayerController player)) return;

            player.ResolveInteractionQuery(out Vector3 origin, out Vector3 forward, out LayerMask obstacleMask);

            if (m_lootInteraction.TryInteract(origin, forward, LootBoxQuery, obstacleMask))
                m_panelOpenSource = PanelOpenSource.Interact;
        }

        // 响应地面垃圾堆快捷键, 先亮标签再打开最近的一座堆
        private void OnInteractGroundPile(InputAction.CallbackContext context)
        {
            MvvmRoot root = MvvmRoot.Instance;
            if (root != null && root.Windows.TryGet(out UIEquipmentPanel openPanel) && openPanel.IsVisible &&
                m_panelOpenSource == PanelOpenSource.GroundPile)
            {
                HideEquipmentPanel(root);
                return;
            }

            if (!TryResolvePlayer(out PlayerController player)) return;

            Vector3 origin = player.transform.position;
            GroundPilePresenter.ReportNearby(origin);
            if (m_lootInteraction.TryInteractGroundPile(origin))
                m_panelOpenSource = PanelOpenSource.GroundPile;
        }

        // Esc 只退一层: 先收拖拽或弹窗, 没有上层时才关闭战备面板
        private void OnCancel(InputAction.CallbackContext context)
        {
            MvvmRoot root = MvvmRoot.Instance;
            if (root == null || !root.Windows.TryGet(out UIEquipmentPanel panel) || !panel.IsVisible) return;

            if (panel.TryCancelTopLayer()) return;

            HideEquipmentPanel(root);
        }

        // 背包面板可见时, 把旋转意图交给当前拖拽载体
        private void OnRotateDraggedItem(InputAction.CallbackContext context)
        {
            MvvmRoot root = MvvmRoot.Instance;
            if (root == null || !root.Windows.TryGet(out UIEquipmentPanel panel) || !panel.IsVisible) return;

            panel.TryRotateDraggedItem();
        }

        // 右键只取消非右键发起的拖拽, 未被拖拽消费时继续交给物品菜单
        private void OnCancelDraggedItemBySecondaryButton(InputAction.CallbackContext context)
        {
            MvvmRoot root = MvvmRoot.Instance;
            if (root == null || !root.Windows.TryGet(out UIEquipmentPanel panel) || !panel.IsVisible) return;

            panel.TryCancelDraggedItemBySecondaryButton();
        }

        // 所有完整关闭入口统一清掉来源, 避免下次 B 打开时沿用旧的垃圾堆身份
        private void HideEquipmentPanel(MvvmRoot root)
        {
            m_panelOpenSource = PanelOpenSource.None;
            root.Windows.Hide<UIEquipmentPanel>();
        }

        // 缓存玩家变换, 换射击玩家后只改这一处来源
        private bool TryResolvePlayerPosition(out Vector3 position)
        {
            if (!TryResolvePlayer(out PlayerController player))
            {
                position = default;
                return false;
            }

            position = player.transform.position;
            return true;
        }

        // 缓存玩家控制器, 交互原点、相机朝向与丢弃方向都从同一实例取
        private bool TryResolvePlayer(out PlayerController player)
        {
            if (m_playerController != null)
            {
                player = m_playerController;
                return true;
            }

            m_playerController = Object.FindObjectOfType<PlayerController>();
            if (m_playerController != null)
            {
                player = m_playerController;
                return true;
            }

            if (!m_hasReportedMissingPlayer)
            {
                m_hasReportedMissingPlayer = true;
                QLog.Error("场景内找不到玩家, 搜刮交互无法定位原点");
            }

            player = null;
            return false;
        }

        // 仅在焦点布尔状态变化时发布, 避免准星每轮扫描都刷新事件
        private void SetLootFocus(LootBoxController focused)
        {
            bool hasFocus = focused != null;
            if (m_hasLootFocus == hasFocus) return;

            m_hasLootFocus = hasFocus;
            EventManager.PublishEvent(EventDefines.LootFocusStateChanged, hasFocus);
        }

        // 取玩家水平朝向, 朝向退化到竖直时用世界前方兜底
        private static Vector3 ResolveDropForward(Transform playerTransform)
        {
            Vector3 forward = playerTransform.forward;
            forward.y = 0f;

            return forward.sqrMagnitude < 0.0001f ? Vector3.forward : forward.normalized;
        }
    }
}
