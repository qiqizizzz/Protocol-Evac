/*
* ┌──────────────────────────────────┐
* │  描    述: 容器定义配置自定义面板, 在 Inspector 上直接点格子画异形网格
* │  类    名: InventoryContainerConfigSOEditor.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.Inventory.Core.Config;
using UnityEditor;
using UnityEngine;
using Utils.log;

namespace UI.Inventory.Editor
{
    // 容器定义 Inspector: 背包/胸挂/口袋/野外物资箱这些容器定义资产共用同一块绘制板
    // 基类写全名, 因为本命名空间末尾的 Editor 会盖掉 UnityEditor.Editor 这个类型
    [CustomEditor(typeof(InventoryContainerConfigSO))]
    public sealed class InventoryContainerConfigSOEditor : UnityEditor.Editor
    {
        // ==================== 常量与静态字段 ====================
        private const string PROP_DISPLAY_NAME = "DisplayNameValue";
        private const string PROP_WIDTH = "WidthValue";
        private const string PROP_HEIGHT = "HeightValue";
        private const string PROP_ROWS = "RowsValue";

        // ==================== 字段[私有] ====================
        private readonly InventoryShapeBoard m_board = new InventoryShapeBoard();

        private SerializedProperty m_displayName;
        private SerializedProperty m_width;
        private SerializedProperty m_height;
        private SerializedProperty m_rows;

        // 画板记住的是编辑中的形状, 只有换了资产才回去读一次配置
        private InventoryContainerConfigSO m_loadedAsset;

        // ==================== 生命周期 ====================
        private void OnEnable()
        {
            m_displayName = RequireProperty(PROP_DISPLAY_NAME);
            m_width = RequireProperty(PROP_WIDTH);
            m_height = RequireProperty(PROP_HEIGHT);
            m_rows = RequireProperty(PROP_ROWS);

            // 域重载后画板里是空表, 强制从资产重新恢复
            m_loadedAsset = null;
        }

        // ==================== Public Function ====================
        public override void OnInspectorGUI()
        {
            // 多选时交给默认面板改原始字段, 形状板只服务单个资产, 免得把同一份画布写到多个容器上
            if (targets.Length > 1)
            {
                DrawDefaultInspector();
                EditorGUILayout.HelpBox("多选时只提供原始字段编辑, 形状绘制板请单选一个容器定义", MessageType.Info);
                return;
            }

            serializedObject.Update();

            InventoryContainerConfigSO asset = (InventoryContainerConfigSO)target;

            m_displayName.stringValue = EditorGUILayout.TextField(new GUIContent("显示名", "载体表头那行字"), m_displayName.stringValue);
            int width = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("列数", "网格横向格子数"), m_width.intValue));
            int height = Mathf.Max(1, EditorGUILayout.IntField("行数", m_height.intValue));
            m_width.intValue = width;
            m_height.intValue = height;

            EditorGUILayout.Space(4f);
            bool shapeChanged = DrawShape(asset, width, height);

            serializedObject.ApplyModifiedProperties();
            if (shapeChanged) EditorUtility.SetDirty(asset);
        }

        // ==================== Private Function ====================
        // 画形状板并把改动写回形状行, 整片实心时形状行留空
        private bool DrawShape(InventoryContainerConfigSO asset, int width, int height)
        {
            if (!ReferenceEquals(m_loadedAsset, asset))
            {
                m_board.Load(asset.Mask, width, height);
                m_loadedAsset = asset;
            }
            else
            {
                m_board.Resize(width, height);
            }

            bool changed = m_board.Draw("形状", "点格子切换实心与空洞, 按住可以拖着刷; 整片实心就不用配形状行");
            if (changed) WriteShapeRows(width, height);

            int solid = m_board.SolidCount;
            EditorGUILayout.LabelField("格数", $"{solid} 实心 / {width * height} 外接");

            if (changed) Repaint();
            return changed;
        }

        private void WriteShapeRows(int width, int height)
        {
            string[] rows = InventoryContainerConfigSO.ToShapeRows(m_board.Shape, width, height);

            m_rows.ClearArray();
            m_rows.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++) m_rows.GetArrayElementAtIndex(i).stringValue = rows[i];
        }

        // 字段名对不上说明配置类改过名, 必须报错暴露而不是静默跳过
        private SerializedProperty RequireProperty(string name)
        {
            SerializedProperty property = serializedObject.FindProperty(name);
            if (property == null)
                QLog.Error($"配置类 {target.GetType().Name} 没有字段 {name}, 请同步 Inspector 里的字段名");

            return property;
        }
    }
}
