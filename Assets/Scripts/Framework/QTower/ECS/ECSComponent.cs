/*
 * ┌──────────────────────────────────┐
 * │  描    述: ECS组件类(数据载体)                      
 * │  类    名: ECSComponent.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace Framework.QTower.Event.ECS
{
    public class ECSComponent
    {
        public int EntityId;

        public virtual int ComType
        {
            get { return 0; }
        }

        public virtual void Recycle()
        {
            EntityId = -1;
        }
    }
}