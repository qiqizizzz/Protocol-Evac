/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包快捷键宿主, 运行期自建常驻, 按 B 开关战备面板
 * │  类    名: UIInventoryHotkey.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Cysharp.Threading.Tasks;
using Framework.QTower.MVVM.View;
using UI.Inventory.View;
using UnityEngine;
using UnityEngine.InputSystem;
using Utils.log;

namespace UI.Inventory
{
    /// <summary>
    /// 背包快捷键宿主：场景加载后自动常驻，按 B 在显示与隐藏之间切换战备面板
    /// </summary>
    public sealed class UIInventoryHotkey : MonoBehaviour
    {
        private static UIInventoryHotkey m_instance;

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

        private void OnDestroy()
        {
            if (m_instance == this) m_instance = null;
        }

        // ==================== Public Function ====================
        /// <summary>
        /// 切换战备面板：正在显示则隐藏(视图模型状态保留)，否则请求打开
        /// </summary>
        public void TogglePanel()
        {
            MvvmRoot root = MvvmRoot.Instance;
            if (root == null)
            {
                QLog.Error($"[{nameof(UIInventoryHotkey)}] 场景缺少 {nameof(MvvmHost)}, 战备面板无法打开");
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
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            // 无键盘设备时不响应
            if (keyboard == null) return;
            if (!keyboard.bKey.wasPressedThisFrame) return;

            TogglePanel();
        }
    }
}