/*
 * ┌──────────────────────────────────┐
 * │  描    述: Ability Composer 主窗口，负责加载工具布局与配置静态编辑控件
 * │  类    名: AbilityComposerWindow.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Utils.log;
using UiFontAsset = UnityEngine.TextCore.Text.FontAsset;

namespace Tools.Editor.AbilityComposer
{
    public sealed class AbilityComposerWindow : EditorWindow
    {
        private const string MENU_PATH = "工具/Ability/Ability Composer";
        private const string WINDOW_TITLE = "Ability Composer";
        private const string WINDOW_UXML_PATH = "Assets/Scripts/Tools/Editor/AbilityComposer/UI/Uxml/AbilityComposerWindow.uxml";
        private const string WINDOW_USS_PATH = "Assets/Scripts/Tools/Editor/AbilityComposer/UI/Uss/AbilityComposerWindow.uss";
        private const string MI_SANS_FONT_ASSET_PATH = "Assets/Fonts/miSans/MiSans-Regular-UI Toolkit.asset";

        // 打开 Ability Composer 主窗口
        [MenuItem(MENU_PATH)]
        private static void OpenWindow()
        {
            AbilityComposerWindow window = GetWindow<AbilityComposerWindow>();
            window.titleContent = new GUIContent(WINDOW_TITLE);
            window.minSize = new Vector2(1024f, 640f);
            window.Show();
        }

        private void CreateGUI()
        {
            rootVisualElement.Clear();

            VisualTreeAsset windowVisualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(WINDOW_UXML_PATH);
            if (windowVisualTree == null)
            {
                QLog.Error($"未找到窗口布局资源：{WINDOW_UXML_PATH}");
                CreateMissingAssetView("未找到 Ability Composer 的 UXML 布局资源");
                return;
            }

            windowVisualTree.CloneTree(rootVisualElement);

            StyleSheet windowStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(WINDOW_USS_PATH);
            if (windowStyleSheet == null)
            {
                QLog.Error($"未找到窗口样式资源：{WINDOW_USS_PATH}");
                CreateMissingAssetView("未找到 Ability Composer 的 USS 样式资源");
                return;
            }

            rootVisualElement.styleSheets.Add(windowStyleSheet);
            ConfigureStaticControls();
            ApplyMiSansFont();
        }

        // 配置 P0 阶段的预览对象与动画资源输入控件
        private void ConfigureStaticControls()
        {
            ObjectField previewSourceField = rootVisualElement.Q<ObjectField>("preview-source-field");
            if (previewSourceField == null)
            {
                QLog.Error("未找到预览对象输入控件：preview-source-field");
                return;
            }

            previewSourceField.objectType = typeof(GameObject);
            previewSourceField.allowSceneObjects = true;

            ObjectField animationClipField = rootVisualElement.Q<ObjectField>("animation-clip-field");
            if (animationClipField == null)
            {
                QLog.Error("未找到动画片段输入控件：animation-clip-field");
                return;
            }

            animationClipField.objectType = typeof(AnimationClip);
            animationClipField.allowSceneObjects = false;
        }

        // 为窗口文字应用 MiSans，避免中文回退到系统粗体字体
        private void ApplyMiSansFont()
        {
            UiFontAsset miSansFontAsset = AssetDatabase.LoadAssetAtPath<UiFontAsset>(MI_SANS_FONT_ASSET_PATH);
            if (miSansFontAsset == null)
            {
                QLog.Error($"未找到 Ability Composer 的 MiSans 字体资源：{MI_SANS_FONT_ASSET_PATH}");
                return;
            }

            StyleFontDefinition fontDefinition = new StyleFontDefinition(miSansFontAsset);
            List<TextElement> textElements = rootVisualElement.Query<TextElement>().ToList();
            foreach (TextElement textElement in textElements)
                textElement.style.unityFontDefinition = fontDefinition;
        }

        // 在窗口中显示缺失 UXML 或 USS 时的明确错误信息
        private void CreateMissingAssetView(string message)
        {
            Label errorLabel = new Label(message);
            errorLabel.style.paddingLeft = 12f;
            errorLabel.style.paddingRight = 12f;
            errorLabel.style.paddingTop = 12f;
            errorLabel.style.paddingBottom = 12f;
            errorLabel.style.color = new Color(1f, 0.45f, 0.45f, 1f);
            rootVisualElement.Add(errorLabel);
        }
    }
}
