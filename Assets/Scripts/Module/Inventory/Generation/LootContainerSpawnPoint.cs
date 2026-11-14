/*
* ┌──────────────────────────────────┐
* │  描    述: 物资箱生成点标记, 声明稳定编号、数量范围、散布半径与出生朝向
* │  类    名: LootContainerSpawnPoint.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.Inventory.Core.Config.Loot;
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

        [Header("散布")]
        [Tooltip("同一生成点生成多个物资箱时的环状散布半径, 单箱生成时忽略")]
        [SerializeField, Min(0f)] private float ScatterRadiusValue;

        [Header("生成池")]
        [Tooltip("本生成点可选的物资箱池, 留空时沿用控制器的全局目录")]
        [SerializeField] private LootContainerCatalogConfigSO SpawnPoolValue;

        public string SpawnId => SpawnIdValue;
        public int MinCount => MinCountValue;
        public int MaxCount => MaxCountValue;
        public float ScatterRadius => ScatterRadiusValue;
        public LootContainerCatalogConfigSO SpawnPool => SpawnPoolValue;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(SpawnIdValue))
                QLog.Error($"物资箱生成点编号为空：{name}");

            if (MaxCountValue < MinCountValue)
                QLog.Error($"物资箱生成点数量范围错误：{name}, {MinCountValue} - {MaxCountValue}");

            if (MaxCountValue > 1 && ScatterRadiusValue <= 0f)
                QLog.Error($"物资箱生成点散布半径无效：{name}, 多箱生成需要大于 0");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            Gizmos.DrawRay(transform.position, transform.forward);

            if (ScatterRadiusValue <= 0f) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, ScatterRadiusValue);
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
