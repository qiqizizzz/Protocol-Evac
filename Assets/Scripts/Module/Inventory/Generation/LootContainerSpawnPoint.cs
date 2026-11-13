/*
* ┌──────────────────────────────────┐
* │  描    述: 物资箱生成点标记, 声明稳定编号、数量范围与出生朝向
* │  类    名: LootContainerSpawnPoint.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using UnityEngine;
using Utils.log;

namespace Module.Inventory.Generation
{
    /// <summary>
    /// 物资箱生成点：只保存生成数据, 实例化与配置由生成控制器负责
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LootContainerSpawnPoint : MonoBehaviour
    {
        [Header("标识")]
        [Tooltip("稳定短标识, 例如 checkpoint_a_01, 用于生成实体号与随机种子")]
        [SerializeField] private string SpawnIdValue = string.Empty;

        [Header("数量")]
        [Tooltip("本生成点最少生成的物资箱数量")]
        [SerializeField, Min(0)] private int MinCountValue = 1;

        [Tooltip("本生成点最多生成的物资箱数量")]
        [SerializeField, Min(0)] private int MaxCountValue = 1;

        public string SpawnId => SpawnIdValue;
        public int MinCount => MinCountValue;
        public int MaxCount => MaxCountValue;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(SpawnIdValue))
                QLog.Error($"物资箱生成点编号为空：{name}");

            if (MaxCountValue < MinCountValue)
                QLog.Error($"物资箱生成点数量范围错误：{name}, {MinCountValue} - {MaxCountValue}");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            Gizmos.DrawRay(transform.position, transform.forward);
        }

        /// <summary>
        /// 判断生成点数量范围是否有效
        /// </summary>
        public bool HasValidCountRange()
        {
            if (MaxCountValue >= MinCountValue) return true;

            QLog.Error($"物资箱生成点数量范围无效：{name}, {MinCountValue} - {MaxCountValue}");
            return false;
        }
    }
}
