/*
 * ┌──────────────────────────────────┐
 * │  描    述: 无数据信号接口                      
 * │  类    名: ISignal.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;

namespace Framework.QTower.MVVM.ViewModel.Interfaces
{
    public interface ISignal : IDisposable
    {
        //触发信号
        void Fire();
        
        //订阅信号
        IDisposable Subscribe(Action handler);
    }
}