/*
 * ┌──────────────────────────────────┐
 * │  描    述: 只读响应式属性接口                      
 * │  类    名: IReadOnlyReactiveProperty.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Threading;
using Cysharp.Threading.Tasks;

namespace Framework.QTower.MVVM.ViewModel.Interfaces
{
    public interface IReadOnlyReactiveProperty<T> : IUniTaskAsyncEnumerable<T>
    {
        T Value { get; }
        bool HasValue { get; }
        bool IsDisposed { get; }
        
        //获取一个不重放当前值的订阅值, 只接收后续变化
        IUniTaskAsyncEnumerable<T> WithoutCurrent();
        
        //异步等待下一次赋值并拿到该值
        UniTask<T> WaitAsync(CancellationToken cancellationToken = default);
    }
}