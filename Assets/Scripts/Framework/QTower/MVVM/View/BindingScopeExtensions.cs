/*
 * ┌──────────────────────────────────┐
 * │  描    述: View层绑定扩展                      
 * │  类    名: BindingScopeExtensions.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.QTower.MVVM.Binding;
using Framework.QTower.MVVM.ViewModel.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils.log;

namespace Framework.QTower.MVVM.View
{
    public static class BindingScopeExtensions
    {
        /// <summary>
        /// 绑定文本: 属性值变化时写入TMP控件
        /// </summary>
        /// <param name="scope">目标绑定作用域</param>
        /// <param name="property">ViewModel的只读响应式属性</param>
        /// <param name="label">文本控件</param>
        public static void BindText(this BindingScope scope, IReadOnlyReactiveProperty<string> property, TMP_Text label)
        {
            if (scope == null)
            {
                QLog.Error("绑定失败：BindText 的 BindingScope 为空");
                return;
            }

            if (label == null)
            {
                QLog.Error("绑定失败：BindText 的 TMP_Text 为空");
                return;
            }
            
            scope.Bind(property, value => label.text = value);
        }
        
        /// <summary>
        /// 绑定图片：属性值变化时替换 Image 的 Sprite
        /// </summary>
        /// <param name="scope">目标绑定作用域</param>
        /// <param name="property">ViewModel侧的只读Sprite属性</param>
        /// <param name="target">接收图片的Image控件</param>
        public static void BindSprite(this BindingScope scope, IReadOnlyReactiveProperty<Sprite> property, Image image)
        {
            if (scope == null)
            {
                QLog.Error("绑定失败：BindSprite 的 BindingScope 为空");
                return;
            }

            if (image == null)
            {
                QLog.Error("绑定失败：BindSprite 的 Image 为空");
                return;
            }
            
            scope.Bind(property, value => image.sprite = value);
        }
    }
}