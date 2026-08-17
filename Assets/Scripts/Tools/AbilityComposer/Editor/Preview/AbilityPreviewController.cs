/*
 * ┌────────────────────────────────────────────────────────────────────┐
 * │  描    述: Ability Composer 场景预览控制器，负责临时克隆与动画采样
 * │  类    名: AbilityPreviewController.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils.log;

namespace Tools.AbilityComposer.Editor.Preview
{
    public sealed class AbilityPreviewController
    {
        private const string PREVIEW_SCENE_PATH = "Assets/Scenes/Tools/AbilityPreview.unity";

        private GameObject m_previewInstance;
        private GameObject m_animationSampleRoot;
        private readonly List<string> m_previousScenePaths = new List<string>();
        private string m_previousActiveScenePath;
        private Scene m_previewScene;
        private bool m_isAnimationModeActive;

        public bool HasPreview => m_previewInstance != null
                                  && m_previewInstance.scene.IsValid()
                                  && m_previewInstance.scene.isLoaded;
        public GameObject PreviewRoot => m_previewInstance;
        public GameObject AnimationEventReceiver => m_animationSampleRoot;

        /// <summary>
        /// 创建临时克隆并采样动画首帧
        /// </summary>
        /// <param name="previewSource">场景对象或 Prefab 预览来源</param>
        /// <param name="animationClip">待预览的动画片段</param>
        public void CreatePreview(GameObject previewSource, AnimationClip animationClip)
        {
            if (previewSource == null)
            {
                QLog.Error("创建 Ability 预览失败：预览来源为空");
                return;
            }

            if (animationClip == null)
            {
                QLog.Error("创建 Ability 预览失败：动画片段为空");
                return;
            }

            if (!OpenPreviewScene())
                return;

            m_previewInstance = Object.Instantiate(previewSource);
            m_previewInstance.name = previewSource.name;
            SetPreviewInstanceHideFlags(m_previewInstance);
            SceneManager.MoveGameObjectToScene(m_previewInstance, m_previewScene);
            m_animationSampleRoot = ResolveAnimationSampleRoot(animationClip);

            ClosePreviousScenes();

            if (!EnsureAnimationMode())
            {
                ClearPreview();
                return;
            }

            SampleAnimation(animationClip, 0f);
        }

        // 聚焦 Scene View 中的临时预览对象
        public void FocusPreview()
        {
            if (m_previewInstance == null)
            {
                QLog.Error("聚焦 Scene 失败：当前没有可用的预览对象");
                return;
            }

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                QLog.Error("聚焦 Scene 失败：未找到已打开的 Scene View");
                return;
            }

            sceneView.Frame(CalculatePreviewBounds(), false);
            sceneView.Repaint();
        }

        // 采样预览对象到指定动画时间
        public void SampleAnimation(AnimationClip animationClip, float time)
        {
            if (m_previewInstance == null)
            {
                QLog.Error("采样动画失败：当前没有可用的预览对象");
                return;
            }

            if (animationClip == null)
            {
                QLog.Error("采样动画失败：动画片段为空");
                return;
            }

            if (!EnsureAnimationMode())
                return;

            float sampleTime = Mathf.Clamp(time, 0f, animationClip.length);
            AnimationMode.SampleAnimationClip(m_animationSampleRoot, animationClip, sampleTime);
            SceneView.RepaintAll();
        }

        // 清理临时克隆并结束 AnimationMode
        public void ClearPreview()
        {
            StopAnimationMode();
            ClearPreviewObject();
            SceneView.RepaintAll();
        }

        // 关闭预览并返回创建预览前的工作场景
        public void ReturnToPreviousScene()
        {
            ClearPreview();
            RestorePreviousScenes();
        }

        // 确保当前控制器独占 AnimationMode
        private bool EnsureAnimationMode()
        {
            if (m_isAnimationModeActive && AnimationMode.InAnimationMode())
                return true;

            if (AnimationMode.InAnimationMode())
            {
                QLog.Error("创建 Ability 预览失败：AnimationMode 正被其他编辑器工具使用");
                return false;
            }

            AnimationMode.StartAnimationMode();
            m_isAnimationModeActive = true;
            return true;
        }

        // 停止当前控制器开启的 AnimationMode
        private void StopAnimationMode()
        {
            if (!m_isAnimationModeActive)
                return;

            if (AnimationMode.InAnimationMode())
                AnimationMode.StopAnimationMode();

            m_isAnimationModeActive = false;
        }

        // 记录当前工作场景并切换到固定预览场景
        private bool OpenPreviewScene()
        {
            if (m_previewScene.IsValid() && m_previewScene.isLoaded)
            {
                ClearPreview();
                return true;
            }

            Scene openedPreviewScene = SceneManager.GetSceneByPath(PREVIEW_SCENE_PATH);
            if (openedPreviewScene.IsValid() && openedPreviewScene.isLoaded)
            {
                m_previewScene = openedPreviewScene;
                return true;
            }

            RecordPreviousScenes();

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                QLog.Warning("已取消进入 AbilityPreview 场景");
                ClearPreviousSceneRecord();
                return false;
            }

            m_previewScene = EditorSceneManager.OpenScene(PREVIEW_SCENE_PATH, OpenSceneMode.Additive);
            return m_previewScene.IsValid() && m_previewScene.isLoaded;
        }

        // 记录进入预览前已打开的场景与当前活动场景
        private void RecordPreviousScenes()
        {
            m_previousScenePaths.Clear();
            m_previousActiveScenePath = SceneManager.GetActiveScene().path;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!string.IsNullOrEmpty(scene.path))
                    m_previousScenePaths.Add(scene.path);
            }
        }

        // 重新打开进入预览前的场景集合
        private void RestorePreviousScenes()
        {
            if (m_previousScenePaths.Count == 0)
                return;

            EditorSceneManager.OpenScene(m_previousScenePaths[0], OpenSceneMode.Single);
            for (int i = 1; i < m_previousScenePaths.Count; i++)
                EditorSceneManager.OpenScene(m_previousScenePaths[i], OpenSceneMode.Additive);

            Scene previousActiveScene = SceneManager.GetSceneByPath(m_previousActiveScenePath);
            if (previousActiveScene.IsValid())
                SceneManager.SetActiveScene(previousActiveScene);

            m_previewInstance = null;
            m_previewScene = default;
            ClearPreviousSceneRecord();
        }

        // 克隆完成后关闭旧场景，使 Scene View 只显示 AbilityPreview
        private void ClosePreviousScenes()
        {
            SceneManager.SetActiveScene(m_previewScene);

            for (int i = 0; i < m_previousScenePaths.Count; i++)
            {
                Scene previousScene = SceneManager.GetSceneByPath(m_previousScenePaths[i]);
                if (previousScene.IsValid() && previousScene.isLoaded)
                    EditorSceneManager.CloseScene(previousScene, true);
            }
        }

        // 销毁当前预览场景中的角色克隆
        private void ClearPreviewObject()
        {
            m_animationSampleRoot = null;
            if (m_previewInstance != null)
                Object.DestroyImmediate(m_previewInstance);

            m_previewInstance = null;
        }

        // 根据动画曲线绑定路径选择最匹配的预览子层级作为采样根
        private GameObject ResolveAnimationSampleRoot(AnimationClip animationClip)
        {
            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(animationClip);
            Transform[] sampleRootCandidates = m_previewInstance.GetComponentsInChildren<Transform>(true);
            Transform sampleRoot = m_previewInstance.transform;
            int bestMatchCount = -1;

            for (int candidateIndex = 0; candidateIndex < sampleRootCandidates.Length; candidateIndex++)
            {
                Transform candidate = sampleRootCandidates[candidateIndex];
                int matchCount = CalculateBindingMatchCount(candidate, curveBindings);
                if (matchCount <= bestMatchCount)
                    continue;

                sampleRoot = candidate;
                bestMatchCount = matchCount;
            }

            if (bestMatchCount == 0)
                QLog.Warning($"动画预览未找到与 Clip 路径匹配的层级，将使用 Prefab 根节点采样：{animationClip.name}");

            return sampleRoot.gameObject;
        }

        // 计算候选层级能够解析的动画曲线数量
        private int CalculateBindingMatchCount(Transform sampleRoot, EditorCurveBinding[] curveBindings)
        {
            int matchCount = 0;
            for (int bindingIndex = 0; bindingIndex < curveBindings.Length; bindingIndex++)
            {
                string bindingPath = curveBindings[bindingIndex].path;
                if (string.IsNullOrEmpty(bindingPath))
                {
                    if (sampleRoot.GetComponent(curveBindings[bindingIndex].type) != null)
                        matchCount++;

                    continue;
                }

                if (sampleRoot.Find(bindingPath) != null)
                    matchCount++;
            }

            return matchCount;
        }

        // 标记整个预览层级为编辑器临时对象，避免写入 AbilityPreview 场景资源
        private void SetPreviewInstanceHideFlags(GameObject previewInstance)
        {
            Transform[] transforms = previewInstance.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
                transforms[i].gameObject.hideFlags = HideFlags.DontSaveInEditor;
        }

        // 清空已记录的工作场景信息
        private void ClearPreviousSceneRecord()
        {
            m_previousActiveScenePath = string.Empty;
            m_previousScenePaths.Clear();
        }

        // 计算 Scene View 聚焦所需的预览对象包围盒
        private Bounds CalculatePreviewBounds()
        {
            Renderer[] renderers = m_previewInstance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(m_previewInstance.transform.position, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }
    }
}
