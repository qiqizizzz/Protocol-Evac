/*
* ┌────────────────────────────────────────────────────────────┐
* │  描    述: 群体代理运行数据，承载Steering Behavior所需的运动状态与约束
* │  类    名: CrowdAgentData.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────────────┘
*/

using UnityEngine;

namespace Framework.QTower.ECS.Crowd.Data
{
    public struct CrowdAgentData
    {
        public int EntityId;
        public int GroupId;
        public int TargetEntityId;
        public Vector3 TargetPosition;
        public bool HasTarget;

        //运动状态
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 Acceleration;
        public Vector3 Forward;

        //Steering 输出
        public Vector3 SteeringForce;

        //运动约束
        public float Radius;
        public float Mass;
        public float MaxSpeed;
        public float MaxForce;
        public float MaxTurnRate;
        public float SlowingRadius;
    }
}
