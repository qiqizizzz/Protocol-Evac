/*
 * ┌──────────────────────────────────┐
 * │  描    述: 可写响应式属性接口                      
 * │  类    名: IReactiveProperty.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;

namespace Framework.QTower.MVVM.ViewModel.Interfaces
{
    public interface IReactiveProperty<T> : IReadOnlyReactiveProperty<T>, IDisposable
    {
        new T Value { get; set; }
        
        //强制发布
        void Set(T value);
        
        //去重赋值
        bool TrySet(T value);
        
        //重新手动发布
        void Refresh();

        //获取只读视图
        IReadOnlyReactiveProperty<T> AsReadOnly();
    }
}