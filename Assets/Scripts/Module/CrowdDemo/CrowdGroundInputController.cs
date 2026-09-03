/*
* ┌────────────────────────────────────────────────────────────┐
* │  描    述: Crowd地面输入控制器，将地面点击转换为群体移动命令
* │  类    名: CrowdGroundInputController.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────────────┘
*/

using Framework.QTower.ECS.Crowd.Core;
using UnityEngine;
using Utils.log;

namespace Module.CrowdDemo
{
    public sealed class CrowdGroundInputController : MonoBehaviour
    {
        [Header("Crowd地面输入")]
        [Tooltip("可被点击的地面层")]
        [SerializeField] private LayerMask GroundLayerMask = -1;

        private Camera m_camera;
        private CrowdClickMarkerPresenter m_clickMarkerPresenter;
        private bool m_isReady;

        // 缓存地面点击所需的主摄像机
        private void Awake()
        {
            m_camera = Camera.main;
            if (m_camera == null)
            {
                QLog.Error("Crowd 地面输入初始化失败：未找到 MainCamera");
                return;
            }
        }

        // 在场景对象完成启用后解析点击标记表现源
        private void Start()
        {
            m_clickMarkerPresenter = CrowdClickMarkerPresenter.Active;

            if (m_clickMarkerPresenter == null)
            {
                QLog.Error("Crowd 地面输入初始化失败：场景中未找到 CrowdClickMarkerPresenter");
                return;
            }

            m_isReady = true;
        }

        // 监听地面点击并发布当前群体的移动命令
        private void Update()
        {
            if (!m_isReady || !Input.GetMouseButtonDown(0))
                return;

            if (m_camera == null || CrowdWorld.Active == null)
                return;

            Ray ray = m_camera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, GroundLayerMask,
                    QueryTriggerInteraction.Ignore))
                return;

            CrowdWorld.Active.EnqueueMoveGroup(CrowdWorld.Active.SelectedGroupId, hit.point);
            m_clickMarkerPresenter.Show(hit.point, hit.normal);
        }
    }
}
