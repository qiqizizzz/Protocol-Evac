/*
* ┌──────────────────────────────────┐
* │  描    述: 地面垃圾堆登记表契约, 只记堆在哪、由哪只容器承载
* │  类    名: IGroundPileModel.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Framework.QTower.MVVM.Model.Interfaces;
using UnityEngine;

namespace UI.Inventory
{
    public interface IGroundPileModel : IModel
    {
        event Action OnChanged;

        //全部在册的堆, 只读视图, 呈现层按它对齐场景实例
        IReadOnlyList<GroundPileVO> Piles { get; }

        //起一堆, 容器由调用方备好, 返回分配到的堆号
        string CreatePile(int containerId, Vector3 anchorPosition);

        //按堆号取堆快照
        bool TryGet(string pileId, out GroundPileVO pile);

        //按容器号取堆快照, 面板侧只有容器号时用它反查
        bool TryGetByContainer(int containerId, out GroundPileVO pile);

        //找半径内最近的一座堆, 丢弃收拢与按 H 查看都走它
        bool TryFindNearest(Vector3 center, float radius, out GroundPileVO pile);

        //查指定半径内的堆, 结果按由近到远排列
        IReadOnlyList<GroundPileVO> QueryNearby(Vector3 center, float radius);

        //销堆, 堆被搬空或整体拾走时调用
        void RemovePile(string pileId);
    }
}
