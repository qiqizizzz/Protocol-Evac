/*
 * ┌──────────────────────────────────┐
 * │  描    述: 群体避障运行参数, 承载邻居查询与转向修正的可调约束                      
 * │  类    名: CrowdAvoidanceData.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace Framework.QTower.ECS.Crowd.Data
{
    public class CrowdAvoidanceData
    {
        private const float MIN_NEIGHBOR_RADIUS = 0.5f;//最小邻居半径
        private const float MIN_PREDICT_TIME = 0.05f;//最小碰撞时间
        private const int MIN_NEIGHBOR_COUNT = 1;//最小邻居考察数量
        
        //邻居查询半径
        public float NeighborRadius = 4;

        //额外安全间距
        public float SeparationMargin = 0.15f;
        
        //重叠分离修正权重
        public float SeparationStrength = 1f;
        
        //碰撞预测时长
        public float PredictTime = 1.2f;
        
        //速度障碍修正权重
        public float AvoidanceStrength = 1f;
        
        //单个代理每帧最多处理的邻居数量, 用于限制高密度下的开销
        public int MaxNeighborCount = 8;
        
        public void Clamp()
        {
            if (NeighborRadius < MIN_NEIGHBOR_RADIUS)
                NeighborRadius = MIN_NEIGHBOR_RADIUS;

            if (SeparationMargin < 0f)
                SeparationMargin = 0f;
            
            if(SeparationStrength < 0f)
                SeparationStrength = 0f;
            
            if(PredictTime < MIN_PREDICT_TIME)
                PredictTime = MIN_PREDICT_TIME;
            
            if(AvoidanceStrength < 0f)
                AvoidanceStrength = 0f;
            
            if(MaxNeighborCount < MIN_NEIGHBOR_COUNT)
                MaxNeighborCount = MIN_NEIGHBOR_COUNT;
        }

    }
}