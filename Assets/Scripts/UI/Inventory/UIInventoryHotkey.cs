/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包快捷键宿主, 运行期自建常驻, B 开关面板, F 通用交互, H 开关垃圾堆, Esc 逐层返回
 * │  类    名: UIInventoryHotkey.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Cysharp.Threading.Tasks;
using Framework.QTower.MVVM.View;
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

        private static UIInventoryHotkey m_instance;
        private ShootingPlayerInputActions m_inputActions;
        private LootBoxInteraction m_lootInteraction;
        private GroundDropInteraction m_dropInteraction;
        private Transform m_playerTransform;
        private bool m_hasReportedMissingPlayer;
        private PanelOpenSource m_panelOpenSource;

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
        }

        private void OnEnable()
        {
            m_inputActions.UI.ToggleInventory.performed += OnToggleInventory;
            m_inputActions.UI.Interact.performed += OnInteract;
            m_inputActions.UI.InteractGroundPile.performed += OnInteractGroundPile;
            m_inputActions.UI.Cancel.performed += OnCancel;
            m_inputActions.UI.Enable();
        }

        private void OnDisable()
        {
            m_inputActions.UI.ToggleInventory.performed -= OnToggleInventory;
            m_inputActions.UI.Interact.performed -= OnInteract;
            m_inputActions.UI.InteractGroundPile.performed -= OnInteractGroundPile;
            m_inputActions.UI.Cancel.performed -= OnCancel;
            m_inputActions.UI.Disable();
        }

        private void OnDestroy()
        {
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
            Vector3 origin = playerPos + ResolveDropForward(m_instance.m_playerTransform) * DROP_FORWARD_DISTANCE;

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

        // 响应背包开关快捷键
        private void OnToggleInventory(InputAction.CallbackContext context)
        {
            TogglePanel();
        }

        // 响应通用交互快捷键, 原点用玩家位置
        private void OnInteract(InputAction.CallbackContext context)
        {
            if (!TryResolvePlayerPosition(out Vector3 origin)) return;

            if (m_lootInteraction.TryInteract(origin))
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

            if (!TryResolvePlayerPosition(out Vector3 origin)) return;

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

        // 所有完整关闭入口统一清掉来源, 避免下次 B 打开时沿用旧的垃圾堆身份
        private void HideEquipmentPanel(MvvmRoot root)
        {
            m_panelOpenSource = PanelOpenSource.None;
            root.Windows.Hide<UIEquipmentPanel>();
        }

        // 缓存玩家变换, 换射击玩家后只改这一处来源
        private bool TryResolvePlayerPosition(out Vector3 position)
        {
            if (m_playerTransform == null)
            {
                PlayerController player = Object.FindObjectOfType<PlayerController>();
                if (player == null)
                {
                    if (!m_hasReportedMissingPlayer)
                    {
                        m_hasReportedMissingPlayer = true;
                        QLog.Error("场景内找不到玩家, 搜刮交互无法定位原点");
                    }

                    position = default;
                    return false;
                }

                m_playerTransform = player.transform;
            }

            position = m_playerTransform.position;
            return true;
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
