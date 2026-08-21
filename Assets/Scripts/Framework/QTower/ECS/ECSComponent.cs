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

        // 组件挂到实体时的初始化回调
        public virtual void OnAdd()
        {
        }

        // 组件从实体移除时的清理回调
        public virtual void OnRemove()
        {
        }

        // 组件回收时重置运行时标记
        public virtual void Recycle()
        {
            EntityId = -1;
        }
    }
}
