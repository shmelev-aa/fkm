// Палитра префабов для сборки 2D-уровня.
// Положите файл в папку Assets/Editor (папка должна называться именно Editor).
// Открыть: меню Tools → Палитра префабов.
//
// Клик по префабу      — ставит его в центр окна Scene с привязкой к сетке.
// Перетаскивание в Scene — ставит его под мышкой с привязкой к сетке.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PrefabPaletteWindow : EditorWindow
{
    const string PrefabsRoot = "Assets/Prefabs";
    const string DragKey = "PrefabPaletteWindow.Prefab";
    const string AllFolders = "Все";
    const string GridStepPref = "PrefabPaletteWindow.GridStep";
    const string TileSizePref = "PrefabPaletteWindow.TileSize";
    const string FiltersPref = "PrefabPaletteWindow.Filters";

    class Entry
    {
        public GameObject prefab;
        public string name;
        public string folder;
        public string path;
    }

    List<Entry> entries = new List<Entry>();
    List<string> folders = new List<string>();
    Dictionary<string, int> folderCounts = new Dictionary<string, int>();
    // Включённые фильтры. Пусто = показываем всё.
    HashSet<string> activeFolders = new HashSet<string>();
    string search = "";
    float gridStep = 0.5f;
    float tileSize = 96f;
    Vector2 scroll;

    Entry pressedEntry;
    Vector2 pressPosition;

    // «Призрак» в окне Scene во время перетаскивания
    bool dragGhostVisible;
    Vector3 dragGhostPosition;
    GameObject dragGhostPrefab;

    GUIStyle nameStyle;
    GUIStyle folderStyle;

    [MenuItem("Tools/Палитра префабов")]
    static void Open()
    {
        var window = GetWindow<PrefabPaletteWindow>("Палитра префабов");
        window.minSize = new Vector2(260, 220);
    }

    void OnEnable()
    {
        gridStep = EditorPrefs.GetFloat(GridStepPref, 0.5f);
        tileSize = EditorPrefs.GetFloat(TileSizePref, 96f);
        string savedFilters = EditorPrefs.GetString(FiltersPref, "");
        activeFolders = new HashSet<string>(savedFilters.Split('|').Where(s => s.Length > 0));
        Reload();
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    // Unity вызывает это, когда в проекте что-то поменялось (добавили/удалили/переименовали префаб)
    void OnProjectChange()
    {
        Reload();
        Repaint();
    }

    // ---------- Загрузка префабов ----------

    void Reload()
    {
        entries.Clear();

        if (AssetDatabase.IsValidFolder(PrefabsRoot))
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabsRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                string directory = Path.GetDirectoryName(path).Replace('\\', '/');
                entries.Add(new Entry
                {
                    prefab = prefab,
                    name = prefab.name,
                    folder = Path.GetFileName(directory), // Assets/Prefabs/Собираемое → «Собираемое»
                    path = path
                });
            }
        }

        entries = entries.OrderBy(e => e.folder).ThenBy(e => e.name).ToList();

        folders = entries.Select(e => e.folder).Distinct().ToList();
        folderCounts = entries.GroupBy(e => e.folder).ToDictionary(g => g.Key, g => g.Count());

        // Убираем фильтры папок, которых больше нет
        activeFolders.RemoveWhere(f => !folders.Contains(f));
    }

    void SaveFilters()
    {
        EditorPrefs.SetString(FiltersPref, string.Join("|", activeFolders));
    }

    // ---------- Отрисовка окна ----------

    void InitStyles()
    {
        if (nameStyle != null) return;

        nameStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            clipping = TextClipping.Clip
        };

        folderStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            clipping = TextClipping.Clip,
            normal = { textColor = Color.white }
        };
    }

    void OnGUI()
    {
        InitStyles();
        DrawToolbar();
        DrawFolderFilters();
        DrawSettings();

        if (!AssetDatabase.IsValidFolder(PrefabsRoot))
        {
            EditorGUILayout.HelpBox($"Папка {PrefabsRoot} не найдена. Создайте её и положите туда префабы.", MessageType.Warning);
            return;
        }

        List<Entry> visible = GetVisibleEntries();
        if (visible.Count == 0)
        {
            EditorGUILayout.HelpBox("Префабы не найдены.", MessageType.Info);
            return;
        }

        DrawGrid(visible);

        EditorGUILayout.HelpBox(
            "Клик — поставить в центр окна Scene.\nПеретащить в Scene — поставить под мышкой.\nПозиция всегда привязывается к сетке. Отмена — Ctrl+Z.",
            MessageType.None);

        // Превью грузятся асинхронно — перерисовываем, пока не загрузятся
        if (AssetPreview.IsLoadingAssetPreviews())
            Repaint();
    }

    void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            search = GUILayout.TextField(search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(60));
            if (GUILayout.Button("Обновить", EditorStyles.toolbarButton, GUILayout.Width(70)))
                Reload();
        }
    }

    // Переключаемые фильтры-кнопки по папкам. Переносятся на новую строку, если не влезают.
    void DrawFolderFilters()
    {
        if (folders.Count == 0) return;

        const float height = 20f;
        const float spacing = 4f;
        const float margin = 4f;
        GUIStyle style = EditorStyles.miniButton;

        // Подписи кнопок: «Все» + каждая папка с количеством префабов
        var labels = new List<string> { AllFolders };
        labels.AddRange(folders.Select(f => $"{f} ({folderCounts[f]})"));
        var widths = labels.Select(l => style.CalcSize(new GUIContent(l)).x + 12f).ToList();

        // Считаем раскладку по строкам
        float maxWidth = position.width - margin * 2;
        var positions = new List<Vector2>();
        float x = 0f, y = 0f;
        for (int i = 0; i < labels.Count; i++)
        {
            if (x > 0f && x + widths[i] > maxWidth)
            {
                x = 0f;
                y += height + spacing;
            }
            positions.Add(new Vector2(x, y));
            x += widths[i] + spacing;
        }

        Rect area = GUILayoutUtility.GetRect(maxWidth, y + height + margin * 2);
        Color oldBackground = GUI.backgroundColor;

        for (int i = 0; i < labels.Count; i++)
        {
            var rect = new Rect(area.x + margin + positions[i].x, area.y + margin + positions[i].y, widths[i], height);

            if (i == 0)
            {
                // «Все» активна, когда не выбран ни один фильтр; клик сбрасывает фильтры
                bool allOn = activeFolders.Count == 0;
                GUI.backgroundColor = allOn ? new Color(0.6f, 0.8f, 1f) : oldBackground;
                if (GUI.Toggle(rect, allOn, labels[i], style) && !allOn)
                {
                    activeFolders.Clear();
                    SaveFilters();
                }
                continue;
            }

            string folder = folders[i - 1];
            bool on = activeFolders.Contains(folder);
            GUI.backgroundColor = on ? FolderColor(folder) * 1.6f : oldBackground;

            bool newOn = GUI.Toggle(rect, on, labels[i], style);
            if (newOn != on)
            {
                if (newOn) activeFolders.Add(folder);
                else activeFolders.Remove(folder);

                // Если включили все папки — это то же самое, что «Все»
                if (activeFolders.Count == folders.Count)
                    activeFolders.Clear();

                SaveFilters();
            }
        }

        GUI.backgroundColor = oldBackground;
    }

    void DrawSettings()
    {
        EditorGUI.BeginChangeCheck();
        float oldLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 90;

        gridStep = Mathf.Max(0.01f, EditorGUILayout.FloatField("Шаг сетки", gridStep));
        tileSize = EditorGUILayout.Slider("Размер плиток", tileSize, 64f, 160f);

        EditorGUIUtility.labelWidth = oldLabelWidth;
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetFloat(GridStepPref, gridStep);
            EditorPrefs.SetFloat(TileSizePref, tileSize);
        }
    }

    List<Entry> GetVisibleEntries()
    {
        IEnumerable<Entry> result = entries.Where(e => e.prefab != null);

        if (activeFolders.Count > 0)
            result = result.Where(e => activeFolders.Contains(e.folder));

        if (!string.IsNullOrWhiteSpace(search))
            result = result.Where(e => e.name.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0);

        return result.ToList();
    }

    void DrawGrid(List<Entry> visible)
    {
        const float padding = 6f;
        const float captionHeight = 36f;

        float width = position.width - 20f; // место под полосу прокрутки
        int columns = Mathf.Max(1, Mathf.FloorToInt((width + padding) / (tileSize + padding)));
        int rows = Mathf.CeilToInt(visible.Count / (float)columns);
        float cellHeight = tileSize + captionHeight + padding;

        scroll = EditorGUILayout.BeginScrollView(scroll);
        Rect area = GUILayoutUtility.GetRect(width, rows * cellHeight + padding);

        for (int i = 0; i < visible.Count; i++)
        {
            int column = i % columns;
            int row = i / columns;
            var rect = new Rect(
                area.x + padding + column * (tileSize + padding),
                area.y + padding + row * cellHeight,
                tileSize,
                tileSize + captionHeight);

            DrawTile(rect, visible[i]);
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawTile(Rect rect, Entry entry)
    {
        Event evt = Event.current;
        bool hover = rect.Contains(evt.mousePosition);

        var imageRect = new Rect(rect.x + 4, rect.y + 4, tileSize - 8, tileSize - 8);
        var nameRect = new Rect(rect.x, rect.y + tileSize, tileSize, 18);
        var folderRect = new Rect(rect.x + 6, rect.y + tileSize + 18, tileSize - 12, 15);

        if (evt.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(rect, hover ? new Color(1f, 1f, 1f, 0.12f) : new Color(0f, 0f, 0f, 0.15f));

            Texture preview = GetPreview(entry.prefab);
            if (preview != null)
                GUI.DrawTexture(imageRect, preview, ScaleMode.ScaleToFit);

            // Цветной лейбл папки: у каждой папки свой цвет
            EditorGUI.DrawRect(folderRect, FolderColor(entry.folder));
        }

        GUI.Label(nameRect, entry.name, nameStyle);
        GUI.Label(folderRect, entry.folder, folderStyle);
        GUI.Label(rect, new GUIContent("", entry.path)); // подсказка с путём при наведении

        switch (evt.type)
        {
            case EventType.MouseDown when evt.button == 0 && hover:
                pressedEntry = entry;
                pressPosition = evt.mousePosition;
                evt.Use();
                break;

            case EventType.MouseDrag when pressedEntry == entry && Vector2.Distance(evt.mousePosition, pressPosition) > 6f:
                // Не кладём объект в objectReferences — иначе Unity сам создаст префаб под мышкой без привязки
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new Object[0];
                DragAndDrop.SetGenericData(DragKey, entry.prefab);
                DragAndDrop.StartDrag(entry.name);
                pressedEntry = null;
                evt.Use();
                break;

            case EventType.MouseUp when pressedEntry == entry:
                if (hover)
                    PlaceAtSceneCenter(entry.prefab);
                pressedEntry = null;
                evt.Use();
                break;
        }
    }

    static Texture GetPreview(GameObject prefab)
    {
        // Для 2D чаще всего красивее всего превью самого спрайта
        var spriteRenderer = prefab.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Texture spritePreview = AssetPreview.GetAssetPreview(spriteRenderer.sprite);
            if (spritePreview != null) return spritePreview;
        }

        Texture prefabPreview = AssetPreview.GetAssetPreview(prefab);
        if (prefabPreview != null) return prefabPreview;

        return AssetPreview.GetMiniThumbnail(prefab);
    }

    static Color FolderColor(string folder)
    {
        int hash = 0;
        foreach (char c in folder) hash = hash * 31 + c;
        float hue = Mathf.Abs(hash % 360) / 360f;
        return Color.HSVToRGB(hue, 0.5f, 0.6f);
    }

    // ---------- Установка на сцену ----------

    Vector3 Snap(Vector3 position)
    {
        return new Vector3(
            Mathf.Round(position.x / gridStep) * gridStep,
            Mathf.Round(position.y / gridStep) * gridStep,
            position.z);
    }

    void PlaceAtSceneCenter(GameObject prefab)
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        Vector3 center = sceneView != null ? sceneView.pivot : Vector3.zero;
        Place(prefab, center);
    }

    void Place(GameObject prefab, Vector3 position)
    {
        // InstantiatePrefab сохраняет связь с префабом (объект синий в Hierarchy)
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene());

        Vector3 snapped = Snap(position);
        snapped.z = prefab.transform.position.z; // глубину берём из префаба
        instance.transform.position = snapped;

        Undo.RegisterCreatedObjectUndo(instance, "Поставить " + prefab.name);
        Selection.activeGameObject = instance;
    }

    static bool MouseToWorld(Vector2 mousePosition, out Vector3 world)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        var plane = new Plane(Vector3.forward, Vector3.zero); // плоскость Z = 0
        if (plane.Raycast(ray, out float distance))
        {
            world = ray.GetPoint(distance);
            return true;
        }

        world = ray.origin;
        return false;
    }

    // ---------- Перетаскивание в окно Scene ----------

    void OnSceneGUI(SceneView sceneView)
    {
        Event evt = Event.current;

        if (evt.type == EventType.DragExited)
        {
            dragGhostVisible = false;
            sceneView.Repaint();
            return;
        }

        if (evt.type == EventType.Repaint && dragGhostVisible && dragGhostPrefab != null)
        {
            DrawGhost();
            return;
        }

        if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)
            return;

        var prefab = DragAndDrop.GetGenericData(DragKey) as GameObject;
        if (prefab == null) return;

        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        MouseToWorld(evt.mousePosition, out Vector3 world);

        if (evt.type == EventType.DragUpdated)
        {
            dragGhostVisible = true;
            dragGhostPrefab = prefab;
            dragGhostPosition = Snap(world);
            sceneView.Repaint();
        }
        else
        {
            DragAndDrop.AcceptDrag();
            Place(prefab, world);
            DragAndDrop.SetGenericData(DragKey, null);
            dragGhostVisible = false;
        }

        evt.Use();
    }

    void DrawGhost()
    {
        Vector3 size = new Vector3(gridStep, gridStep, 0f);
        Vector3 offset = Vector3.zero;

        var spriteRenderer = dragGhostPrefab.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Bounds bounds = spriteRenderer.sprite.bounds;
            Vector3 scale = spriteRenderer.transform.lossyScale;
            size = Vector3.Scale(bounds.size, scale);
            offset = Vector3.Scale(bounds.center, scale);
        }

        Handles.color = new Color(0.3f, 0.9f, 1f, 1f);
        Handles.DrawWireCube(dragGhostPosition + offset, size);
        Handles.DrawSolidDisc(dragGhostPosition, Vector3.forward, gridStep * 0.08f);
    }
}