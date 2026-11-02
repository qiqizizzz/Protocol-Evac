/*
 * ┌──────────────────────────────────┐
 * │  描    述: 物品图标转正工具, 逐张手调旋转角, 导出等高且宽度自动补齐的 PNG(AI 自留工具, 非生产代码)
 * │  类    名: IconRotationTool.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UI.Inventory.Editor
{
    // 枪械剪影图标转正: 滑杆定角度, 画布高度恒定, 宽度按内容长宽比自动加长, 长枪不会被裁
    public sealed class IconRotationTool : EditorWindow
    {
        // ==================== 常量与静态字段 ====================
        private const string LOG = "[IconRotationTool] ";
        private const string DEFAULT_SRC = @"E:\Download\gun.png\icons\ffffff\transparent\1x1";
        private const string DEFAULT_DST = "Assets/Art/Inventory/ItemIcon/VectorImageIcon";
        private const string CONFIG_FILE_NAME = "icon_rotation_config.json";
        private const int EXPORT_SUPERSAMPLE = 4;
        private const int PREVIEW_SUPERSAMPLE = 3;
        private const int PREVIEW_HEIGHT = 220;
        private const int FLIP_PROBE_HEIGHT = 128;
        private const int MAX_WORK_PIXELS = 8000000;
        private const int PREVIEW_WORK_PIXELS = 2000000;
        private const int MAX_SCALE = 16;
        private const float ROT_MARGIN = 3f;
        private const float CROP_THRESHOLD = 0.02f;
        private const float POINT_THRESHOLD = 0.5f;
        private const float GAIN_GATE = 1.95f;
        private const float MIN_ROTATE_DEG = 3f;
        private const float FLIP_RATIO = 1.08f;
        private const float CELL_SIZE = 80f;
        private const float CELL_SPACING = 4f;

        // 明显不是枪械的图标, 扫描时默认排除, 界面上可勾选回来
        private static readonly HashSet<string> m_defaultExcluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "arrest", "bullet-impacts", "cowboy-holster", "crosshair", "reticule",
            "gun-stock", "machine-gun-magazine", "reload-gun-barrel", "supersonic-bullet",
            "bullets", "heavy-bullets", "shotgun-rounds", "artillery-shell",
            "cannon-ball", "cannon-shot", "gunshot", "mortar", "field-gun",
            "anti-aircraft-gun", "cannon", "pirate-cannon", "sentry-gun",
            "missile-pod", "crossed-pistols", "water-gun", "zat-gun", "chaingun",
        };

        // 枪口比枪托粗的异形货, 朝向判定要反着来
        private static readonly HashSet<string> m_muzzleIsThick = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "blunderbuss", "ray-gun",
        };

        // ==================== 字段[外部设置] ====================
        [Tooltip("源 PNG 目录, 递归扫描")]
        [SerializeField] private string SourceDir = DEFAULT_SRC;

        [Tooltip("导出目录, 必须在 Assets 下")]
        [SerializeField] private string OutputDir = DEFAULT_DST;

        [Tooltip("导出画布统一高度")]
        [SerializeField, Range(128, 1024)] private int OutputHeight = 512;

        [Tooltip("画布上下留白占高度比例")]
        [SerializeField, Range(0f, 0.2f)] private float PaddingRatio = 0.06f;

        [Tooltip("画布最大宽度, 超过则内容整体等比缩, 别超过导入器的 maxTextureSize")]
        [SerializeField] private int MaxWidth = 4096;

        // ==================== 字段[私有] ====================
        private readonly List<Entry> m_entries = new List<Entry>();
        private Vector2 m_listScroll;
        private int m_selected = -1;
        private string m_configPath;
        private string m_filter = string.Empty;
        private string m_status = string.Empty;
        private bool m_previewDirty;
        private bool m_dirty;
        private Texture2D m_checker;

        // ==================== 菜单入口 ====================
        [MenuItem("Tools/背包7a/图标转正工具")]
        public static void ShowWindow()
        {
            IconRotationTool window = GetWindow<IconRotationTool>();
            window.titleContent = new GUIContent("图标转正");
            window.minSize = new Vector2(940f, 620f);
        }

        // ==================== 生命周期 ====================
        private void OnEnable()
        {
            m_configPath = Path.Combine(OutputDir, CONFIG_FILE_NAME);
            m_checker = MakeChecker();
            ScanSources();
        }

        private void OnDisable()
        {
            if (m_dirty) SaveConfig();
            ReleasePreviews();
        }

        private void OnDestroy()
        {
            ReleasePreviews();
        }

        // ==================== GUI ====================
        private void OnGUI()
        {
            DrawSettings();
            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            DrawEntryList();
            DrawDetailPanel();
            GUILayout.EndHorizontal();
        }

        // 顶部参数与操作按钮
        private void DrawSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            string src = EditorGUILayout.TextField(new GUIContent("源目录", "递归扫描 *.png"), SourceDir);
            if (src != SourceDir) SourceDir = src;

            string dst = EditorGUILayout.TextField(new GUIContent("导出目录", "必须在 Assets 下"), OutputDir);
            if (dst != OutputDir)
            {
                OutputDir = dst;
                m_configPath = Path.Combine(OutputDir, CONFIG_FILE_NAME);
                m_dirty = true;
            }

            int height = EditorGUILayout.IntSlider(new GUIContent("导出高度", "画布统一高度"), OutputHeight, 128, 1024);
            if (height != OutputHeight) { OutputHeight = height; InvalidatePreviews(); }

            float padding = EditorGUILayout.Slider(new GUIContent("留白比例", "上下留白占高度比例"), PaddingRatio, 0f, 0.2f);
            if (!Mathf.Approximately(padding, PaddingRatio)) { PaddingRatio = padding; InvalidatePreviews(); }

            int maxWidth = EditorGUILayout.IntField(new GUIContent("最大宽度", "画布宽度上限, 超了内容整体等比缩"), MaxWidth);
            if (maxWidth != MaxWidth) { MaxWidth = maxWidth; InvalidatePreviews(); }

            GUILayout.BeginHorizontal();
            DrawButton("扫描源目录", 96f, ScanSources);
            DrawButton("重算自动角", 96f, AutoAngleAll);
            DrawButton("保存配置", 76f, SaveConfig);
            DrawButton("载入配置", 76f, LoadConfig);
            DrawButton("清理失效导出", 96f, CleanupStale);
            DrawButton("导出选中", 76f, ExportSelected);
            DrawButton("导出全部", 76f, ExportAll);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorGUILayout.LabelField(m_status, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        // 按钮延迟到 GUI 之外执行, 对话框和 AssetDatabase.Refresh 不能在 OnGUI 里调
        private void DrawButton(string label, float width, Action action)
        {
            if (GUILayout.Button(label, GUILayout.Width(width))) EditorApplication.delayCall += () => action();
        }

        // 左侧条目列表
        private void DrawEntryList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(368f), GUILayout.ExpandHeight(true));

            string filter = EditorGUILayout.TextField(new GUIContent("过滤", "按文件名子串筛选"), m_filter);
            if (filter != m_filter) m_filter = filter;

            m_listScroll = GUILayout.BeginScrollView(m_listScroll);
            for (int i = 0; i < m_entries.Count; i++)
            {
                if (MatchFilter(m_entries[i])) DrawEntryRow(i, m_entries[i]);
            }
            GUILayout.EndScrollView();

            EditorGUILayout.LabelField(string.Format("共 {0}   待导出 {1}   排除 {2}",
                m_entries.Count, CountExport(), CountExcluded()), EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        // 列表单行: 排除勾选 + 文件名 + 角度
        private void DrawEntryRow(int index, Entry entry)
        {
            GUILayout.BeginHorizontal();

            bool excluded = GUILayout.Toggle(entry.Excluded, GUIContent.none, GUILayout.Width(18f));
            if (excluded != entry.Excluded) { entry.Excluded = excluded; MarkChanged(); }

            Color old = GUI.contentColor;
            GUI.contentColor = entry.Excluded ? new Color(0.55f, 0.55f, 0.55f) : Color.white;
            GUILayout.Label(entry.OutputFile, GUILayout.MinWidth(150f), GUILayout.ExpandWidth(true));
            GUILayout.Label(entry.Excluded ? "-" : entry.Angle.ToString("0.##") + "°", GUILayout.Width(52f));
            GUI.contentColor = old;

            GUILayout.EndHorizontal();

            Rect row = GUILayoutUtility.GetLastRect();
            if (index == m_selected) EditorGUI.DrawRect(row, new Color(0.24f, 0.48f, 0.85f, 0.3f));

            if (Event.current.type == EventType.MouseDown && row.Contains(Event.current.mousePosition))
            {
                m_selected = index;
                m_previewDirty = true;
                Event.current.Use();
                Repaint();
            }
        }

        // 右侧预览与调角面板
        private void DrawDetailPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            Entry entry = Selected();
            if (entry == null)
            {
                EditorGUILayout.LabelField("在左侧点一个图标");
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField(entry.OutputFile, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(entry.SourcePath, EditorStyles.miniLabel);
            DrawPreviewArea(entry);
            GUILayout.Space(4f);
            DrawAngleControls(entry);
            DrawReadout(entry);

            EditorGUILayout.EndVertical();
        }

        // 预览区: 深色棋盘底 + 画布边框 + 水平中线, 一眼看出歪不歪
        private void DrawPreviewArea(Entry entry)
        {
            Rect box = GUILayoutUtility.GetRect(120f, 2048f, GUILayout.ExpandWidth(true), GUILayout.Height(268f));
            if (m_checker != null)
            {
                GUI.DrawTextureWithTexCoords(box, m_checker, new Rect(0f, 0f, box.width / 4f, box.height / 4f));
            }
            else
            {
                EditorGUI.DrawRect(box, new Color(0.15f, 0.15f, 0.15f));
            }

            Texture2D tex = GetPreview(entry);
            if (tex == null) return;

            float fit = Mathf.Min(box.width / tex.width, box.height / tex.height);
            float w = Mathf.Max(1f, tex.width * fit);
            float h = Mathf.Max(1f, tex.height * fit);
            Rect inner = new Rect(box.x + (box.width - w) * 0.5f, box.y + (box.height - h) * 0.5f, w, h);
            GUI.DrawTexture(inner, tex, ScaleMode.ScaleToFit, true);

            Color border = new Color(0.35f, 0.75f, 1f, 0.5f);
            EditorGUI.DrawRect(new Rect(inner.x, inner.y, inner.width, 1f), border);
            EditorGUI.DrawRect(new Rect(inner.x, inner.yMax - 1f, inner.width, 1f), border);
            EditorGUI.DrawRect(new Rect(inner.x, inner.y, 1f, inner.height), border);
            EditorGUI.DrawRect(new Rect(inner.xMax - 1f, inner.y, 1f, inner.height), border);
            EditorGUI.DrawRect(new Rect(inner.x, inner.y + inner.height * 0.5f - 0.5f, inner.width, 1f),
                new Color(1f, 0.35f, 0.35f, 0.85f));
        }

        // 滑杆 + 微调按钮 + 朝向翻转
        private void DrawAngleControls(Entry entry)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("角度", GUILayout.Width(38f));

            float slider = GUILayout.HorizontalSlider(entry.Angle, -90f, 90f, GUILayout.ExpandWidth(true));
            if (!Mathf.Approximately(slider, entry.Angle)) { entry.Angle = slider; MarkChanged(); m_previewDirty = true; }

            float typed = EditorGUILayout.FloatField(entry.Angle, GUILayout.Width(58f));
            if (!Mathf.Approximately(typed, entry.Angle)) { entry.Angle = Mathf.Clamp(typed, -180f, 180f); MarkChanged(); m_previewDirty = true; }

            GUILayout.Label("度", GUILayout.Width(16f));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-1")) Nudge(entry, -1f);
            if (GUILayout.Button("-0.25")) Nudge(entry, -0.25f);
            if (GUILayout.Button("+0.25")) Nudge(entry, 0.25f);
            if (GUILayout.Button("+1")) Nudge(entry, 1f);
            if (GUILayout.Button("归零")) { entry.Angle = 0f; MarkChanged(); m_previewDirty = true; }
            if (GUILayout.Button("自动角")) { entry.Angle = AutoSuggest(entry); MarkChanged(); m_previewDirty = true; }
            if (GUILayout.Button("自动朝向")) { entry.Flip = SuggestFlip(entry, entry.Angle); MarkChanged(); m_previewDirty = true; }
            bool flip = GUILayout.Toggle(entry.Flip, "再转180", GUILayout.Width(80f));
            if (flip != entry.Flip) { entry.Flip = flip; MarkChanged(); m_previewDirty = true; }
            GUILayout.EndHorizontal();
        }

        // 微调角度, 越过正负90 时用翻转承接 180
        private void Nudge(Entry entry, float delta)
        {
            float angle = entry.Angle + delta;
            if (angle > 90f) { angle -= 180f; entry.Flip = !entry.Flip; }
            else if (angle < -90f) { angle += 180f; entry.Flip = !entry.Flip; }

            entry.Angle = angle;
            MarkChanged();
            m_previewDirty = true;
        }

        // 显示导出尺寸与格子足迹建议
        private void DrawReadout(Entry entry)
        {
            float aspect = entry.ContentAspect > 0f ? entry.ContentAspect : 1f;
            int pad = Mathf.RoundToInt(OutputHeight * PaddingRatio);
            int contentH = Mathf.Max(1, OutputHeight - pad * 2);
            int contentW = Mathf.Max(1, Mathf.RoundToInt(aspect * contentH));
            int canvasW = contentW + pad * 2;
            string note = string.Empty;

            if (canvasW > MaxWidth)
            {
                float k = (MaxWidth - pad * 2) / (float)contentW;
                contentW = Mathf.Max(1, Mathf.RoundToInt(contentW * k));
                contentH = Mathf.Max(1, Mathf.RoundToInt(contentH * k));
                canvasW = contentW + pad * 2;
                note = "  已达宽度上限";
            }

            EditorGUILayout.LabelField(string.Format("画布 {0} x {1}   内容 {2} x {3}   长宽比 {4:0.00}{5}",
                canvasW, OutputHeight, contentW, contentH, aspect, note), EditorStyles.miniLabel);
            EditorGUILayout.LabelField(string.Format("格子足迹建议 {0}   (格子 {1:0} + 间距 {2:0})",
                FootprintHint(aspect), CELL_SIZE, CELL_SPACING), EditorStyles.miniLabel);
            EditorGUILayout.LabelField("画布高度恒定, 宽度按内容自动加长, 长枪不会被裁", EditorStyles.wordWrappedMiniLabel);
        }

        // ==================== 数据 ====================
        // 单个待处理图标
        private sealed class Entry
        {
            public string Key;
            public string Name;
            public string SourcePath;
            public string OutputFile;
            public float Angle;
            public bool Flip;
            public bool Excluded;
            public float ContentAspect;
            public Texture2D Preview;
        }

        // 角度配置记录, JsonUtility 只认 public 字段, 这里当纯 DTO 用
        [Serializable]
        private sealed class AngleRecord
        {
            public string key;
            public string file;
            public float angle;
            public bool flip;
            public bool excluded;
        }

        // 角度配置文件根对象
        [Serializable]
        private sealed class AngleConfig
        {
            public List<AngleRecord> records = new List<AngleRecord>();
        }

        // ==================== 扫描与配置 ====================
        // 递归扫描源目录, 同名文件按 _02 递增, 已存过配置则回填角度与文件名
        private void ScanSources()
        {
            ReleasePreviews();
            m_entries.Clear();
            m_selected = -1;

            if (!Directory.Exists(SourceDir))
            {
                Debug.LogError(LOG + "源目录不存在: " + SourceDir);
                m_status = "源目录不存在";
                return;
            }

            string[] files = Directory.GetFiles(SourceDir, "*.png", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);

            Dictionary<string, int> used = new Dictionary<string, int>();
            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i];
                string name = Path.GetFileNameWithoutExtension(path);
                string author = new DirectoryInfo(Path.GetDirectoryName(path)).Name;

                int count;
                used.TryGetValue(name, out count);
                count++;
                used[name] = count;

                Entry entry = new Entry();
                entry.Key = author + "/" + name;
                entry.Name = name;
                entry.SourcePath = path;
                entry.OutputFile = count == 1
                    ? "gun_" + name + ".png"
                    : string.Format("gun_{0}_{1:00}.png", name, count);
                entry.Excluded = m_defaultExcluded.Contains(name);
                m_entries.Add(entry);
            }

            ApplyConfig();

            m_selected = FirstSelectable();
            m_previewDirty = true;
            m_status = string.Format("扫描到 {0} 张, 默认排除 {1} 张", m_entries.Count, CountExcluded());
            Repaint();
        }

        // 把配置文件里的角度/朝向/排除/输出名回填到条目
        private void ApplyConfig()
        {
            if (string.IsNullOrEmpty(m_configPath) || !File.Exists(m_configPath)) return;

            AngleConfig config = JsonUtility.FromJson<AngleConfig>(File.ReadAllText(m_configPath));
            if (config == null || config.records == null) return;

            Dictionary<string, AngleRecord> map = new Dictionary<string, AngleRecord>();
            for (int i = 0; i < config.records.Count; i++)
            {
                AngleRecord record = config.records[i];
                if (!string.IsNullOrEmpty(record.key)) map[record.key] = record;
            }

            int hit = 0;
            for (int i = 0; i < m_entries.Count; i++)
            {
                AngleRecord record;
                if (!map.TryGetValue(m_entries[i].Key, out record)) continue;

                m_entries[i].Angle = record.angle;
                m_entries[i].Flip = record.flip;
                m_entries[i].Excluded = record.excluded;
                if (!string.IsNullOrEmpty(record.file)) m_entries[i].OutputFile = record.file;
                hit++;
            }

            m_status = string.Format("已载入配置, 匹配 {0}/{1}", hit, m_entries.Count);
        }

        // 读回配置并刷新列表
        private void LoadConfig()
        {
            ApplyConfig();
            m_previewDirty = true;
            Repaint();
        }

        // 角度写盘, 下次开工具不用重调
        private void SaveConfig()
        {
            string dir = ResolveOutputDir();
            if (string.IsNullOrEmpty(dir)) return;

            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            AngleConfig config = new AngleConfig();
            for (int i = 0; i < m_entries.Count; i++)
            {
                Entry entry = m_entries[i];
                AngleRecord record = new AngleRecord();
                record.key = entry.Key;
                record.file = entry.OutputFile;
                record.angle = Mathf.Round(entry.Angle * 100f) / 100f;
                record.flip = entry.Flip;
                record.excluded = entry.Excluded;
                config.records.Add(record);
            }

            File.WriteAllText(Path.Combine(dir, CONFIG_FILE_NAME), JsonUtility.ToJson(config, true));
            m_dirty = false;
            m_status = "配置已保存: " + CONFIG_FILE_NAME;
            Debug.Log(LOG + m_status);
        }

        // ==================== 导出 ====================
        // 只导当前选中的一张
        private void ExportSelected()
        {
            Entry entry = Selected();
            if (entry == null) return;

            SaveConfig();
            if (!ExportOne(entry)) return;
            AssetDatabase.Refresh();
            m_status = "已导出 " + entry.OutputFile;
            Repaint();
        }

        // 批量导出所有未排除的条目
        private void ExportAll()
        {
            List<Entry> list = new List<Entry>();
            for (int i = 0; i < m_entries.Count; i++)
            {
                if (!m_entries[i].Excluded) list.Add(m_entries[i]);
            }

            if (list.Count == 0)
            {
                Debug.LogWarning(LOG + "没有待导出的条目");
                return;
            }

            SaveConfig();
            int done = 0;
            int failed = 0;
            try
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("图标转正", list[i].OutputFile, (i + 1f) / list.Count)) break;
                    if (!ExportOne(list[i])) failed++;
                    done++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();
            m_status = string.Format("已导出 {0} 张, 失败 {1} 张", done, failed);
            Debug.Log(LOG + m_status);
            Repaint();
        }

        // 生成并写出一张 PNG, 顺手把导入设置拉起来
        private bool ExportOne(Entry entry)
        {
            int width, height;
            Color32[] pixels = BuildCanvas(entry, OutputHeight, MaxWidth, EXPORT_SUPERSAMPLE, MAX_WORK_PIXELS, out width, out height);
            if (pixels == null) return false;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            string full = Path.Combine(ResolveOutputDir(), entry.OutputFile);
            File.WriteAllBytes(full, png);
            ConfigureImporter(entry.OutputFile, width);

            m_previewDirty = true;
            return true;
        }

        // 图标是 UI Sprite, 且长枪宽度会超 2048, 统一把导入设置顶上去
        private void ConfigureImporter(string fileName, int width)
        {
            string asset = ToAssetPath(OutputDir);
            if (asset == null)
            {
                Debug.LogWarning(LOG + "导出目录不在 Assets 下, 跳过导入设置: " + OutputDir);
                return;
            }

            string assetPath = (asset + "/" + fileName).Replace('\\', '/');
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.maxTextureSize = Mathf.Max(2048, Mathf.NextPowerOfTwo(width));
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        // 删掉不再导出的历史 PNG
        private void CleanupStale()
        {
            string asset = ToAssetPath(OutputDir);
            string dir = ResolveOutputDir();
            if (asset == null || !Directory.Exists(dir)) return;

            HashSet<string> keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < m_entries.Count; i++)
            {
                if (!m_entries[i].Excluded) keep.Add(m_entries[i].OutputFile);
            }

            List<string> stale = new List<string>();
            string[] files = Directory.GetFiles(dir, "gun_*.png");
            for (int i = 0; i < files.Length; i++)
            {
                string name = Path.GetFileName(files[i]);
                if (!keep.Contains(name)) stale.Add(asset + "/" + name);
            }

            if (stale.Count == 0)
            {
                m_status = "没有失效文件";
                Repaint();
                return;
            }

            if (!EditorUtility.DisplayDialog("清理失效导出", "将删除 " + stale.Count + " 张不在导出列表里的 gun_*.png, 继续?", "删除", "取消")) return;

            for (int i = 0; i < stale.Count; i++)
            {
                if (!AssetDatabase.DeleteAsset(stale[i])) Debug.LogWarning(LOG + "删除失败: " + stale[i]);
            }

            AssetDatabase.Refresh();
            m_status = "已清理 " + stale.Count + " 张";
            Repaint();
        }

        // ==================== 预览与算法 ====================
        // 取当前条目预览贴图, 角度或参数变了才重算
        private Texture2D GetPreview(Entry entry)
        {
            if (entry.Preview != null && !m_previewDirty) return entry.Preview;
            if (entry.Preview != null)
            {
                UnityEngine.Object.DestroyImmediate(entry.Preview);
                entry.Preview = null;
            }

            int width, height;
            int previewMaxWidth = Mathf.Max(64, Mathf.RoundToInt(MaxWidth * (PREVIEW_HEIGHT / (float)OutputHeight)));
            Color32[] pixels = BuildCanvas(entry, PREVIEW_HEIGHT, previewMaxWidth, PREVIEW_SUPERSAMPLE, PREVIEW_WORK_PIXELS, out width, out height);
            m_previewDirty = false;
            if (pixels == null) return null;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.hideFlags = HideFlags.HideAndDontSave;
            entry.Preview = tex;
            return tex;
        }

        // 全局参数变了, 所有预览作废
        private void InvalidatePreviews()
        {
            ReleasePreviews();
            m_previewDirty = true;
            m_dirty = true;
            Repaint();
        }

        // 全部重算: 自动角 + 朝向, 会覆盖手调值
        private void AutoAngleAll()
        {
            int changed = 0;
            try
            {
                for (int i = 0; i < m_entries.Count; i++)
                {
                    Entry entry = m_entries[i];
                    if (entry.Excluded) continue;
                    if (EditorUtility.DisplayCancelableProgressBar("重算自动角", entry.OutputFile, (i + 1f) / m_entries.Count)) break;

                    float angle = AutoSuggest(entry);
                    bool flip = SuggestFlip(entry, angle);
                    if (!Mathf.Approximately(angle, entry.Angle) || flip != entry.Flip) changed++;

                    entry.Angle = angle;
                    entry.Flip = flip;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            InvalidatePreviews();
            m_status = string.Format("重算完成, 变更 {0} 张", changed);
            Repaint();
        }

        // 自动建议角度: 长宽比增益不够说明画师本来就画平了, 保持 0
        private float AutoSuggest(Entry entry)
        {
            int sw, sh;
            float[] alpha = LoadAlpha(entry.SourcePath, out sw, out sh);
            if (alpha == null) return entry.Angle;

            List<Vector2> points = new List<Vector2>();
            CollectPoints(alpha, sw, sh, Mathf.Max(1, sw / 128), points);
            if (points.Count < 8) return entry.Angle;

            float flat = AspectAt(points, 0f);
            float best = 0f;
            float bestAspect = flat;

            for (float t = -90f; t < 88f; t += 2f)
            {
                float a = AspectAt(points, t);
                if (a > bestAspect) { bestAspect = a; best = t; }
            }
            for (float t = best - 2f; t <= best + 2f; t += 0.25f)
            {
                float a = AspectAt(points, t);
                if (a > bestAspect) { bestAspect = a; best = t; }
            }

            float gain = flat > 0f ? bestAspect / flat : 0f;
            if (gain < GAIN_GATE || Mathf.Abs(best) < MIN_ROTATE_DEG) return 0f;
            return Mathf.Round(best * 4f) / 4f;
        }

        // 按内容两端厚度判朝向, 细的一端当枪口摆右边
        private bool SuggestFlip(Entry entry, float angle)
        {
            int sw, sh;
            float[] alpha = LoadAlpha(entry.SourcePath, out sw, out sh);
            if (alpha == null) return entry.Flip;

            List<Vector2> points = new List<Vector2>();
            CollectPoints(alpha, sw, sh, Mathf.Max(1, sw / 128), points);
            if (points.Count < 8) return entry.Flip;

            float ignored;
            Rect rot = Inflate(RotatedBounds(points, angle, out ignored), ROT_MARGIN);
            float scale = Mathf.Clamp(FLIP_PROBE_HEIGHT / rot.height, 1f, MAX_SCALE);
            int probeW = Mathf.Max(1, Mathf.CeilToInt(rot.width * scale));
            int probeH = Mathf.Max(1, Mathf.CeilToInt(rot.height * scale));

            float[] work = RenderRotated(alpha, sw, sh, rot, angle, scale, probeW, probeH);
            int cw, ch;
            float[] cropped = Crop(work, probeW, probeH, ContentBounds(work, probeW, probeH, CROP_THRESHOLD, 1), out cw, out ch);

            int band = Mathf.Max(1, cw / 5);
            float thin = EndThickness(cropped, cw, ch, 0, band);
            float thick = EndThickness(cropped, cw, ch, cw - band, band);

            bool flip = thick > thin * FLIP_RATIO;
            if (m_muzzleIsThick.Contains(entry.Name)) flip = !flip;
            return flip;
        }

        // 生成转正画布: 高度恒定, 宽度按内容长宽比自动加长, 只写 alpha
        private Color32[] BuildCanvas(Entry entry, int canvasHeight, int canvasMaxWidth, int superSample, int workBudget,
            out int outWidth, out int outHeight)
        {
            outWidth = 0;
            outHeight = 0;

            int sw, sh;
            float[] src = LoadAlpha(entry.SourcePath, out sw, out sh);
            if (src == null) return null;

            List<Vector2> points = new List<Vector2>();
            CollectPoints(src, sw, sh, Mathf.Max(1, sw / 256), points);
            if (points.Count < 8)
            {
                Debug.LogError(LOG + "图标几乎没有内容: " + entry.SourcePath);
                return null;
            }

            float angle = entry.Angle + (entry.Flip ? 180f : 0f);
            float ignored;
            Rect rot = Inflate(RotatedBounds(points, angle, out ignored), ROT_MARGIN);

            int pad = Mathf.Max(0, Mathf.RoundToInt(canvasHeight * PaddingRatio));
            int targetH = Mathf.Max(4, canvasHeight - pad * 2);

            float scale = targetH / rot.height * superSample;
            scale = Mathf.Clamp(scale, 1f, MAX_SCALE);
            if (rot.width * rot.height * scale * scale > workBudget)
                scale = Mathf.Max(0.5f, Mathf.Sqrt(workBudget / (rot.width * rot.height)));

            int workW = Mathf.Max(1, Mathf.CeilToInt(rot.width * scale));
            int workH = Mathf.Max(1, Mathf.CeilToInt(rot.height * scale));
            float[] work = RenderRotated(src, sw, sh, rot, angle, scale, workW, workH);

            int cw, ch;
            float[] cropped = Crop(work, workW, workH, ContentBounds(work, workW, workH, CROP_THRESHOLD, 1), out cw, out ch);
            entry.ContentAspect = cw / (float)ch;

            int finalW = Mathf.Max(1, Mathf.RoundToInt(cw * (targetH / (float)ch)));
            int finalH = targetH;
            int canvasW = finalW + pad * 2;
            if (canvasW > canvasMaxWidth)
            {
                float k = (canvasMaxWidth - pad * 2) / (float)finalW;
                finalW = Mathf.Max(1, Mathf.RoundToInt(finalW * k));
                finalH = Mathf.Max(1, Mathf.RoundToInt(finalH * k));
                canvasW = finalW + pad * 2;
                Debug.Log(LOG + entry.OutputFile + " 超出最大宽度, 内容等比缩到 " + canvasW + " x " + finalH);
            }

            float[] fitted = Resample(cropped, cw, ch, finalW, finalH);

            Color32[] canvas = new Color32[canvasW * canvasHeight];
            Color32 clear = new Color32(255, 255, 255, 0);
            for (int i = 0; i < canvas.Length; i++) canvas[i] = clear;

            int offsetY = (canvasHeight - finalH) / 2;
            for (int y = 0; y < finalH; y++)
            {
                int canvasRow = (offsetY + y) * canvasW + pad;
                int fitRow = y * finalW;
                for (int x = 0; x < finalW; x++)
                {
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(fitted[fitRow + x]) * 255f);
                    canvas[canvasRow + x] = new Color32(255, 255, 255, a);
                }
            }

            outWidth = canvasW;
            outHeight = canvasHeight;
            return canvas;
        }

        // 逆映射把源图旋到工作画布上
        private static float[] RenderRotated(float[] src, int sw, int sh, Rect rot, float angle, float scale, int workW, int workH)
        {
            float[] work = new float[workW * workH];
            float rad = angle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            for (int y = 0; y < workH; y++)
            {
                float ry = rot.yMin + (y + 0.5f) / scale;
                int row = y * workW;
                for (int x = 0; x < workW; x++)
                {
                    float rx = rot.xMin + (x + 0.5f) / scale;
                    work[row + x] = SampleAlpha(src, sw, sh, rx * cos + ry * sin - 0.5f, -rx * sin + ry * cos - 0.5f);
                }
            }
            return work;
        }

        // 双线性采样 alpha, 越界当透明
        private static float SampleAlpha(float[] alpha, int w, int h, float x, float y)
        {
            if (x < 0f || y < 0f || x > w - 1f || y > h - 1f) return 0f;

            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            int x1 = Mathf.Min(x0 + 1, w - 1);
            int y1 = Mathf.Min(y0 + 1, h - 1);
            float tx = x - x0;
            float ty = y - y0;

            return Mathf.Lerp(
                Mathf.Lerp(alpha[y0 * w + x0], alpha[y0 * w + x1], tx),
                Mathf.Lerp(alpha[y1 * w + x0], alpha[y1 * w + x1], tx),
                ty);
        }

        // 扫描不透明范围, 结果用连续坐标表示
        private static Rect ContentBounds(float[] alpha, int w, int h, float threshold, int stride)
        {
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            for (int y = 0; y < h; y += stride)
            {
                int row = y * w;
                for (int x = 0; x < w; x += stride)
                {
                    if (alpha[row + x] <= threshold) continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            if (maxX < minX || maxY < minY) return new Rect(0f, 0f, 0f, 0f);
            return Rect.MinMaxRect(minX, minY, maxX + 1f, maxY + 1f);
        }

        // 收集不透明像素中心点
        private static void CollectPoints(float[] alpha, int w, int h, int stride, List<Vector2> points)
        {
            for (int y = 0; y < h; y += stride)
            {
                int row = y * w;
                for (int x = 0; x < w; x += stride)
                {
                    if (alpha[row + x] > POINT_THRESHOLD) points.Add(new Vector2(x + 0.5f, y + 0.5f));
                }
            }
        }

        // 点集绕原点旋转后的包围盒
        private static Rect RotatedBounds(List<Vector2> points, float angle, out float aspect)
        {
            float rad = angle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 p = points[i];
                float rx = p.x * cos - p.y * sin;
                float ry = p.x * sin + p.y * cos;
                if (rx < minX) minX = rx;
                if (rx > maxX) maxX = rx;
                if (ry < minY) minY = ry;
                if (ry > maxY) maxY = ry;
            }

            aspect = (maxX - minX) / Mathf.Max(0.0001f, maxY - minY);
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        // 指定角度下的内容长宽比
        private static float AspectAt(List<Vector2> points, float angle)
        {
            float aspect;
            RotatedBounds(points, angle, out aspect);
            return aspect;
        }

        // 包围盒四周各扩一点, 防采样误差裁掉边缘
        private static Rect Inflate(Rect rect, float margin)
        {
            return new Rect(rect.xMin - margin, rect.yMin - margin, rect.width + margin * 2f, rect.height + margin * 2f);
        }

        // 裁出一块连续区域
        private static float[] Crop(float[] src, int w, int h, Rect bounds, out int cropW, out int cropH)
        {
            int x0 = Mathf.Clamp(Mathf.FloorToInt(bounds.xMin), 0, w - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(bounds.yMin), 0, h - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(bounds.xMax), x0 + 1, w);
            int y1 = Mathf.Clamp(Mathf.CeilToInt(bounds.yMax), y0 + 1, h);
            cropW = x1 - x0;
            cropH = y1 - y0;

            float[] dst = new float[cropW * cropH];
            for (int y = 0; y < cropH; y++) Array.Copy(src, (y0 + y) * w + x0, dst, y * cropW, cropW);
            return dst;
        }

        // 缩小走面积平均, 放大走双线性
        private static float[] Resample(float[] src, int sw, int sh, int dw, int dh)
        {
            if (sw == dw && sh == dh) return src;
            float[] dst = new float[dw * dh];

            if (sw >= dw && sh >= dh)
            {
                for (int y = 0; y < dh; y++)
                {
                    int y0 = Mathf.FloorToInt(y * sh / (float)dh);
                    int y1 = Mathf.Min(sh, Mathf.Max(y0 + 1, Mathf.CeilToInt((y + 1) * sh / (float)dh)));
                    int countY = y1 - y0;
                    int srcRow = y0 * sw;

                    for (int x = 0; x < dw; x++)
                    {
                        int x0 = Mathf.FloorToInt(x * sw / (float)dw);
                        int x1 = Mathf.Min(sw, Mathf.Max(x0 + 1, Mathf.CeilToInt((x + 1) * sw / (float)dw)));
                        float sum = 0f;
                        for (int sy = 0; sy < countY; sy++)
                        {
                            int row = srcRow + sy * sw;
                            for (int sx = x0; sx < x1; sx++) sum += src[row + sx];
                        }
                        dst[y * dw + x] = sum / (countY * (x1 - x0));
                    }
                }
                return dst;
            }

            for (int y = 0; y < dh; y++)
            {
                float sy = (y + 0.5f) * sh / dh - 0.5f;
                for (int x = 0; x < dw; x++)
                {
                    float sx = (x + 0.5f) * sw / dw - 0.5f;
                    dst[y * dw + x] = SampleAlpha(src, sw, sh, sx, sy);
                }
            }
            return dst;
        }

        // 连续几列的平均内容厚度, 用来分辨枪口端和枪托端
        private static float EndThickness(float[] alpha, int w, int h, int startX, int columns)
        {
            float sum = 0f;
            int count = 0;
            int end = Mathf.Min(startX + columns, w);

            for (int x = startX; x < end; x++)
            {
                int low = -1;
                int high = -1;
                for (int y = 0; y < h; y++)
                {
                    if (alpha[y * w + x] <= CROP_THRESHOLD) continue;
                    if (low < 0) low = y;
                    high = y;
                }
                if (low < 0) continue;

                sum += high - low + 1;
                count++;
            }
            return count > 0 ? sum / count : 0f;
        }

        // 读 PNG 的 alpha 通道, 索引 = y * width + x, 第 0 行在底部
        private static float[] LoadAlpha(string path, out int width, out int height)
        {
            width = 0;
            height = 0;
            if (!File.Exists(path))
            {
                Debug.LogError(LOG + "源文件不存在: " + path);
                return null;
            }

            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            // 注意: Unity 6 里带 transformToGamma 的三参重载会产出不可读贴图, 只有二参重载能 GetPixels32
            if (!ImageConversion.LoadImage(tex, bytes))
            {
                Debug.LogError(LOG + "PNG 解码失败: " + path);
                UnityEngine.Object.DestroyImmediate(tex);
                return null;
            }

            width = tex.width;
            height = tex.height;
            Color32[] pixels = tex.GetPixels32();
            UnityEngine.Object.DestroyImmediate(tex);

            float[] alpha = new float[pixels.Length];
            for (int i = 0; i < pixels.Length; i++) alpha[i] = pixels[i].a / 255f;
            return alpha;
        }

        // 按内容长宽比找最贴合的格子区域, 与运行时格子 80 + 间距 4 对齐
        private static string FootprintHint(float aspect)
        {
            if (aspect <= 0f) return "-";

            int bestW = 1;
            int bestH = 1;
            float bestError = float.MaxValue;
            float unit = CELL_SIZE + CELL_SPACING;

            for (int w = 1; w <= 8; w++)
            {
                for (int h = 1; h <= 4; h++)
                {
                    float region = (w * unit - CELL_SPACING) / (h * unit - CELL_SPACING);
                    float error = Mathf.Abs(Mathf.Log(aspect / region));
                    if (error >= bestError) continue;

                    bestError = error;
                    bestW = w;
                    bestH = h;
                }
            }
            return string.Format("{0} x {1}", bestW, bestH);
        }

        // 预览底纹, 白剪影要深色背景才看得清
        private static Texture2D MakeChecker()
        {
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Color32 light = new Color32(48, 48, 48, 255);
            Color32 dark = new Color32(30, 30, 30, 255);
            tex.SetPixels32(new Color32[] { light, dark, dark, light });
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Point;
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }

        // 释放预览贴图
        private void ReleasePreviews()
        {
            for (int i = 0; i < m_entries.Count; i++)
            {
                if (m_entries[i].Preview == null) continue;
                UnityEngine.Object.DestroyImmediate(m_entries[i].Preview);
                m_entries[i].Preview = null;
            }

            if (m_checker == null) m_checker = MakeChecker();
        }

        // ==================== 小工具 ====================
        private Entry Selected()
        {
            return m_selected >= 0 && m_selected < m_entries.Count ? m_entries[m_selected] : null;
        }

        private bool MatchFilter(Entry entry)
        {
            return string.IsNullOrEmpty(m_filter)
                || entry.OutputFile.IndexOf(m_filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private int CountExport()
        {
            return m_entries.Count - CountExcluded();
        }

        private int CountExcluded()
        {
            int count = 0;
            for (int i = 0; i < m_entries.Count; i++)
            {
                if (m_entries[i].Excluded) count++;
            }
            return count;
        }

        private int FirstSelectable()
        {
            for (int i = 0; i < m_entries.Count; i++)
            {
                if (!m_entries[i].Excluded) return i;
            }
            return m_entries.Count > 0 ? 0 : -1;
        }

        // 记录改动, 关窗口时自动存配置
        private void MarkChanged()
        {
            m_dirty = true;
            EditorUtility.SetDirty(this);
            Repaint();
        }

        // 导出目录转成工程相对路径, 导入器 API 只认 Assets/xxx
        private static string ToAssetPath(string dir)
        {
            string normalized = dir.Replace('\\', '/').TrimEnd('/');
            if (normalized.StartsWith("Assets/")) return normalized;

            string root = Directory.GetParent(Application.dataPath).FullName.Replace('\\', '/');
            if (normalized.StartsWith(root + "/")) return normalized.Substring(root.Length + 1);
            return null;
        }

        // 导出目录的绝对路径
        private string ResolveOutputDir()
        {
            string asset = ToAssetPath(OutputDir);
            if (asset == null) return Path.GetFullPath(OutputDir);
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, asset);
        }
    }
}