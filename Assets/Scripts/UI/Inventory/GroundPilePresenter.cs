/*
* ┌──────────────────────────────────┐
* │  描    述: 地面垃圾堆呈现层, 把堆登记表同步成场景堆实例并驱动 H 键物品标签
* │  类    名: GroundPilePresenter.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections;
using System.Collections.Generic;
using System.Text;
using Common.Res;
using Framework.QTower.MVVM.View;
using Module.Inventory.Presentation;
using TMPro;
using UnityEngine;
using Utils.log;

namespace UI.Inventory
{
    /// <summary>
    /// 地面垃圾堆呈现层：订阅堆登记表与容器变化, 负责堆实例的生成销毁与标签文字
    /// GroundPileView 本身不听模型, 所有读数都由这里算好再递进去
    /// </summary>
    public sealed class GroundPilePresenter : MonoBehaviour
    {
        // 地面堆预制体的资源地址, 与 Addressables 里的条目同名
        private const string PILE_PREFAB_ADDRESS = "GroundPile";

        // 按 H 时能点亮多远范围内的堆标签(米)
        private const float LABEL_RADIUS = 12f;

        // 标签亮起后自动收起的秒数
        private const float LABEL_HOLD_SECONDS = 3f;

        // 标签里最多列几件物品, 超出用省略号收尾
        private const int LABEL_ITEM_LIMIT = 4;

        // 模型表装配前不逐帧硬撞, 隔这么久再试一次(秒)
        private const float BIND_RETRY_INTERVAL = 0.25f;

        private static readonly WaitForSeconds m_labelHold = new WaitForSeconds(LABEL_HOLD_SECONDS);

        private static GroundPilePresenter m_instance;

        private IGroundPileModel m_pileModel;
        private IInventoryModel m_inventoryModel;
        private GameObject m_pilePrefab;
        private Camera m_mainCamera;
        private Coroutine m_labelRoutine;

        private readonly Dictionary<string, GroundPileView> m_views = new Dictionary<string, GroundPileView>();
        private readonly List<string> m_stalePileIds = new List<string>();
        private readonly StringBuilder m_builder = new StringBuilder(96);

        private bool m_labelsShown;
        private float m_nextBindTime;
        private bool m_hasReportedPrefab;

        /// <summary>
        /// 场景加载后自建常驻宿主, 与背包快捷键宿主各自独立
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (m_instance != null) return;

            GameObject host = new GameObject(nameof(GroundPilePresenter));
            Object.DontDestroyOnLoad(host);
            m_instance = host.AddComponent<GroundPilePresenter>();
        }

        // ==================== 生命周期 ====================
        private void Update()
        {
            if (m_pileModel != null) return;

            if (Time.unscaledTime < m_nextBindTime) return;

            m_nextBindTime = Time.unscaledTime + BIND_RETRY_INTERVAL;
            TryBind();
        }

        // 标签亮起时才转向相机, 平时这条一帧活都不干
        private void LateUpdate()
        {
            if (!m_labelsShown) return;

            if (m_mainCamera == null) m_mainCamera = Camera.main;
            if (m_mainCamera == null) return;

            Vector3 cameraForward = m_mainCamera.transform.forward;
            foreach (KeyValuePair<string, GroundPileView> pair in m_views)
            {
                GroundPileView view = pair.Value;
                if (view == null) continue;

                Transform label = view.LabelTransform;
                if (label == null || !label.gameObject.activeSelf) continue;

                label.rotation = Quaternion.LookRotation(label.position - m_mainCamera.transform.position);
            }
        }

        private void OnDestroy()
        {
            Unbind();
            if (m_instance == this) m_instance = null;
        }

        // ==================== Public Function ====================
        /// <summary>
        /// 按 H：点亮落在 origin 附近的所有堆标签, 并把清单同步打进日志
        /// </summary>
        /// <param name="origin">查看原点, 一般是玩家位置</param>
        public static void ReportNearby(Vector3 origin)
        {
            if (m_instance == null)
            {
                QLog.Error("地面堆呈现层尚未常驻, 无法查看地面物品");
                return;
            }

            m_instance.ShowNearbyLabels(origin);
        }

        // ==================== Private Function ====================
        // 绑定模型契约并铺一遍存量堆, 背包模块装配完成前模型表是空的, 绑不上就等下一轮
        private bool TryBind()
        {
            MvvmRoot root = MvvmRoot.Instance;
            if (root == null) return false;

            if (!root.Models.TryGet(out IGroundPileModel pileModel)) return false;
            if (!root.Models.TryGet(out IInventoryModel inventoryModel)) return false;

            m_pilePrefab = ResManager.LoadAsset<GameObject>(PILE_PREFAB_ADDRESS);
            if (m_pilePrefab == null)
            {
                if (!m_hasReportedPrefab)
                {
                    m_hasReportedPrefab = true;
                    QLog.Error($"地面堆预制体未加载：{PILE_PREFAB_ADDRESS}, 请先把它登记进 Addressables");
                }

                return false;
            }

            m_pileModel = pileModel;
            m_inventoryModel = inventoryModel;
            m_pileModel.OnChanged += SyncViews;
            m_inventoryModel.OnChanged += OnContainerChanged;

            SyncViews();
            return true;
        }

        // 解绑事件并清掉场景里所有堆实例, 换场景或宿主销毁时走这里
        private void Unbind()
        {
            if (m_pileModel != null) m_pileModel.OnChanged -= SyncViews;
            if (m_inventoryModel != null) m_inventoryModel.OnChanged -= OnContainerChanged;

            foreach (GroundPileView view in m_views.Values)
            {
                if (view != null) Destroy(view.gameObject);
            }

            m_views.Clear();
            m_pileModel = null;
            m_inventoryModel = null;
            m_labelsShown = false;
        }

        // 堆登记表对齐场景实例：新堆生成, 消失的堆销毁, 存量的堆按最新宽高刷新
        private void SyncViews()
        {
            if (m_pileModel == null) return;

            m_stalePileIds.Clear();
            foreach (KeyValuePair<string, GroundPileView> pair in m_views)
            {
                // 场景重载会把堆实例连带销毁, 视图没了就直接当旧堆清掉
                if (pair.Value == null || !m_pileModel.TryGet(pair.Key, out _)) m_stalePileIds.Add(pair.Key);
            }

            for (int i = 0; i < m_stalePileIds.Count; i++) RemoveView(m_stalePileIds[i]);

            IReadOnlyList<GroundPileVO> piles = m_pileModel.Piles;
            for (int i = 0; i < piles.Count; i++) RefreshView(piles[i]);
        }

        // 容器变化只关心落在堆上的那部分：撑大堆体、刷标签, 搬空了就把堆收掉
        private void OnContainerChanged(int containerId)
        {
            if (m_pileModel == null) return;
            if (!m_pileModel.TryGetByContainer(containerId, out GroundPileVO pile)) return;

            if (!m_inventoryModel.IsContainerRegistered(containerId))
            {
                m_pileModel.RemovePile(pile.PileId);
                return;
            }

            InventoryContainerVO container = m_inventoryModel.GetContainerVO(containerId);
            if (container.Items.Count == 0)
            {
                m_pileModel.RemovePile(pile.PileId);
                return;
            }

            RefreshView(pile);
        }

        // 单座堆的生成与刷新, 宽高与标签文字都取自容器快照
        private void RefreshView(GroundPileVO pile)
        {
            if (!m_inventoryModel.IsContainerRegistered(pile.ContainerId)) return;

            InventoryContainerVO container = m_inventoryModel.GetContainerVO(pile.ContainerId);

            if (!m_views.TryGetValue(pile.PileId, out GroundPileView view) || view == null)
            {
                if (view != null) m_views.Remove(pile.PileId);

                GameObject instance = Instantiate(m_pilePrefab, pile.AnchorPosition, Quaternion.identity);
                view = instance.GetComponent<GroundPileView>();
                if (view == null) view = instance.AddComponent<GroundPileView>();

                m_views[pile.PileId] = view;
            }

            view.Configure(pile.ContainerId, container.Width, container.Height);
            WriteLabel(view, container);
        }

        private void RemoveView(string pileId)
        {
            if (!m_views.TryGetValue(pileId, out GroundPileView view)) return;

            m_views.Remove(pileId);
            if (view != null) Destroy(view.gameObject);
        }

        // 标签文字：件数 + 前几件物品名, 呈现层唯一的字符串出口
        private void WriteLabel(GroundPileView view, InventoryContainerVO container)
        {
            Transform labelTransform = view.LabelTransform;
            if (labelTransform == null) return;

            labelTransform.localPosition = new Vector3(0f, view.LabelHeight, 0f);

            TextMeshPro label = labelTransform.GetComponent<TextMeshPro>();
            if (label == null)
            {
                QLog.Error($"地面堆标签节点缺少 TextMeshPro：{labelTransform.name}");
                return;
            }

            m_builder.Clear();
            m_builder.Append(container.Items.Count).Append(" 件：");

            int shown = Mathf.Min(container.Items.Count, LABEL_ITEM_LIMIT);
            for (int i = 0; i < shown; i++)
            {
                if (i > 0) m_builder.Append(" · ");
                m_builder.Append(container.Items[i].DisplayName);

                if (container.Items[i].StackCount > 1) m_builder.Append('x').Append(container.Items[i].StackCount);
            }

            if (container.Items.Count > shown) m_builder.Append(" …");

            label.text = m_builder.ToString();
        }

        // 点亮原点附近的堆标签, 一段时间后统一收起
        private void ShowNearbyLabels(Vector3 origin)
        {
            if (m_pileModel == null)
            {
                QLog.Warning("地面堆模型尚未装配, 按 H 看不到地面物品");
                return;
            }

            float sqrRadius = LABEL_RADIUS * LABEL_RADIUS;
            int nearbyCount = 0;
            foreach (GroundPileView view in m_views.Values)
            {
                if (view == null) continue;

                bool nearby = (view.transform.position - origin).sqrMagnitude <= sqrRadius;
                if (nearby) nearbyCount++;

                view.ShowLabel(nearby);
            }

            if (nearbyCount == 0)
            {
                QLog.Info("附近没有丢弃物");
                return;
            }

            m_labelsShown = true;
            if (m_labelRoutine != null) StopCoroutine(m_labelRoutine);
            m_labelRoutine = StartCoroutine(HideLabelsRoutine());
        }

        // 收起全部标签的定时出口
        private IEnumerator HideLabelsRoutine()
        {
            yield return m_labelHold;

            foreach (GroundPileView view in m_views.Values)
            {
                if (view != null) view.ShowLabel(false);
            }

            m_labelsShown = false;
            m_labelRoutine = null;
        }
    }
}
