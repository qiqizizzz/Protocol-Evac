/*
* ┌──────────────────────────────────┐
* │  描    述: 地面物品登记表契约
* │  类    名: IGroundItemModel.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Framework.QTower.MVVM.Model.Interfaces;
using UnityEngine;

namespace UI.Inventory
{
    public interface IGroundItemModel : IModel
    {
        event Action OnChanged;

        //登记一件落在世界里的物品, 返回分配到的登记号
        string Register(int containerId, InventoryItemVO item, Vector3 worldPosition);

        //按登记号取地面物品快照
        bool TryGet(string groundItemId, out GroundItemVO item);

        //查指定半径内的地面物品, 结果按由近到远排列
        IReadOnlyList<GroundItemVO> QueryNearby(Vector3 center, float radius);

        //注销一件地面物品, 物品被捡走或系统销毁时调用
        void Unregister(string groundItemId);
    }
}
