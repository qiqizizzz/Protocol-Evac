/*
 * ┌──────────────────────────────────┐
 * │  描    述: 单条绑定登记项                      
 * │  类    名: BindingEntry.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;

namespace Framework.QTower.MVVM.Binding
{
    public class BindingEntry
    {
        public Action Start { get; }
        public Action Stop { get; }
        
        public BindingEntry(Action start, Action stop)
        {
            Start = start;
            Stop = stop;
        }
    }
}