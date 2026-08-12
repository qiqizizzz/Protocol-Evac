/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability Composer 右侧视图，承载 Animation Event Inspector
 * │  类    名: AbilityRightView.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using Framework.QTower.Editor.View;
using UnityEngine.UIElements;

namespace Tools.Editor.AbilityComposer.Right
{
    public sealed class AbilityRightView : UIBaseEditor
    {
        private readonly VisualElement m_rootVisualElement;

        // 注入右侧区域根节点
        public AbilityRightView(VisualElement rootVisualElement)
        {
            m_rootVisualElement = rootVisualElement;
        }
    }
}
