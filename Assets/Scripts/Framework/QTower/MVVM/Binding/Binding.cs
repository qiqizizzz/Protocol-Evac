/*
 * ┌──────────────────────────────────┐
 * │  描    述: MVVM绑定订阅循环
 * │  类    名: Binding.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Framework.QTower.MVVM.Binding
{
    internal static class Binding
    {
        public static async UniTaskVoid Run<T>(IUniTaskAsyncEnumerable<T> source, Action<T> apply,
            CancellationToken token, Action onEnded)
        {
            var enumerator = source.GetAsyncEnumerator(token);

            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    if (token.IsCancellationRequested) break;

                    apply(enumerator.Current);
                }
            }
            catch (OperationCanceledException)
            {
                //静默
            }
            finally
            {
                try{await enumerator.DisposeAsync();}
                catch(OperationCanceledException){}
                
                onEnded?.Invoke();
            }
        }
    }
}