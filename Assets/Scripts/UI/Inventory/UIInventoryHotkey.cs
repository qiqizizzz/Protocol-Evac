/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包快捷键宿主, 运行期自建常驻, 按 B 开关面板, 兼管搜刮、丢弃与查看地面物
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
        private static UIInventoryHotkey m_instance;
        private ShootingPlayerInputActions m_inputActions;
        private LootBoxInteraction m_lootInteraction;
        private GroundDropInteraction m_dropInteraction;
        private Transform m_playerTransform;
        private bool m_hasReportedMissingPlayer;

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
            m_inputActions.UI.InspectGround.performed += OnInspectGround;
            m_inputActions.UI.Enable();
        }

        private void OnDisable()
        {
            m_inputActions.UI.ToggleInventory.performed -= OnToggleInventory;
            m_inputActions.UI.Interact.performed -= OnInteract;
            m_inputActions.UI.InspectGround.performed -= OnInspectGround;
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

            if (!m_instance.TryResolvePlayerPosition(out Vector3 origin)) return false;

            m_instance.m_dropInteraction ??= new GroundDropInteraction();
            return m_instance.m_dropInteraction.TryDrop(containerId, instanceId, origin);
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
                root.Windows.Hide<UIEquipmentPanel>();
                return;
            }

            root.Windows.ShowAsync<UIEquipmentPanel>().Forget();
        }

        // ==================== Private Function ====================
        // 响应背包开关快捷键
        private void OnToggleInventory(InputAction.CallbackContext context)
        {
            TogglePanel();
        }

        // 响应交互快捷键, 原点用玩家位置
        private void OnInteract(InputAction.CallbackContext context)
        {
            if (!TryResolvePlayerPosition(out Vector3 origin)) return;

            m_lootInteraction.TryInteract(origin);
        }

        // 响应查看地面丢弃物快捷键, 原点同样用玩家位置
        private void OnInspectGround(InputAction.CallbackContext context)
        {
            if (!TryResolvePlayerPosition(out Vector3 origin)) return;

            GroundPilePresenter.ReportNearby(origin);
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
                        QLog.Error("场景内找不到玩家, 物资箱交互无法定位原点");
                    }

                    position = default;
                    return false;
                }

                m_playerTransform = player.transform;
            }

            position = m_playerTransform.position;
            return true;
        }
    }
}
