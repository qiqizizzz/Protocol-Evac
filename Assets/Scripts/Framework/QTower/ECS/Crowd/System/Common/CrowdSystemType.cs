/*
* ┌────────────────────────────────────────────────────────────┐
* │  描    述: 群体系统类型定义，集中管理群体ECS系统编号
* │  类    名: CrowdSystemType.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────────────┘
*/

namespace Framework.QTower.ECS.Crowd.System.Common
{
    public enum CrowdSystemType
    {
        CommandApply = 1001,
        SpatialHash = 1002, //空间哈希
        Avoidance = 1003, //动态避障
        Movement = 1004,
        OverlapResolve = 1005, //重叠模型解决
        ViewSync = 1006
    }
}
