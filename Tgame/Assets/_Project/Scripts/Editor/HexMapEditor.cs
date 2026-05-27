#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using TGame.Data;

[CustomEditor(typeof(HexMapEditorData))]
public class HexMapEditor : Editor
{
    private HexMapEditorData _data;
    private BrushLayer _currentBrush = BrushLayer.Ground;
    private int _currentVariant = 0;

    private GameObject _previewRoot;
    private Dictionary<Vector3Int, GameObject> _previewDict = new Dictionary<Vector3Int, GameObject>();

    private void OnEnable()
    {
        _data = (HexMapEditorData)target;
        Undo.undoRedoPerformed += OnUndoRedo;

        // 【🔥核心】监听运行游戏的按钮，点下运行前销毁预览
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        RebuildPreview();
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoRedo;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

        // 【🔥核心】如果 target == null，说明玩家把 MapPainter 删除了，此时才销毁预览图。
        // 如果 target != null，说明只是点到了其他物体(取消选中)，此时【保留预览图】，实现常驻显示！
        if (target == null)
        {
            ClearPreview();
        }
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // 退出编辑模式或进入运行模式的一瞬间，清理掉预览图，把舞台交给游戏系统
        if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredEditMode)
        {
            ClearPreview();
        }
    }

    private void OnUndoRedo()
    {
        RebuildPreview();
        SceneView.RepaintAll();
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck()) RebuildPreview();

        GUILayout.Space(15);
        GUILayout.Label("🖌️ 涂鸦画笔设置", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _currentBrush = (BrushLayer)EditorGUILayout.EnumPopup("当前画笔图层:", _currentBrush);
        if (EditorGUI.EndChangeCheck()) _currentVariant = 0;

        if (_currentBrush == BrushLayer.Ground && _data.groundVariantNames.Length > 0)
            _currentVariant = EditorGUILayout.Popup("地块样式:", _currentVariant, _data.groundVariantNames);
        else if (_currentBrush == BrushLayer.Obstacle && _data.obstacleVariantNames.Length > 0)
            _currentVariant = EditorGUILayout.Popup("障碍物样式:", _currentVariant, _data.obstacleVariantNames);

        GUILayout.Space(15);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("💣 清空画布", GUILayout.Height(30)))
        {
            Undo.RecordObject(_data, "Clear Map");
            _data.cells.Clear();
            RebuildPreview();
            EditorUtility.SetDirty(_data);
        }

        if (GUILayout.Button("🔄 从目标关卡读取", GUILayout.Height(30)))
        {
            LoadFromLevelAsset();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button("📦 将地图数据覆盖至【Target Level】", GUILayout.Height(40)))
        {
            SaveAsLevelAsset();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("【画笔逻辑说明】\n- 地面层：左键刷地板，右键挖空整个格子。\n- 障碍物层：左键放障碍，右键铲除障碍。\n- 现在可以放心点选其他物体了，地图会常驻显示！", MessageType.Info);
    }

    private void OnSceneGUI()
    {
        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controlID);

        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Vector3 worldPos = ray.origin;
        worldPos.z = 0;

        Vector3Int cellPos = WorldToCell(worldPos, _data.hexSize);
        Vector3 snapPos = CellToWorld(cellPos, _data.hexSize);

        Handles.color = _currentBrush == BrushLayer.Ground ? Color.green : Color.red;
        DrawHexagon(snapPos, _data.hexSize);

        GUIStyle hoverStyle = new GUIStyle();
        hoverStyle.normal.textColor = Color.yellow;
        hoverStyle.fontSize = 14;
        hoverStyle.fontStyle = FontStyle.Bold;
        hoverStyle.alignment = TextAnchor.LowerCenter;
        Handles.Label(snapPos + Vector3.up * (_data.hexSize * 0.9f), $"({cellPos.x}, {cellPos.y}, {cellPos.z})", hoverStyle);

        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
        {
            Undo.RecordObject(_data, "Paint Hex");
            if (_currentBrush == BrushLayer.Ground) _data.SetGround(cellPos, _currentVariant);
            else _data.SetObstacle(cellPos, _currentVariant);

            var newCell = _data.cells.Find(c => c.position == cellPos);
            UpdateCellPreview(newCell);

            EditorUtility.SetDirty(_data);
            e.Use();
        }

        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 1)
        {
            Undo.RecordObject(_data, "Erase Hex");
            if (_currentBrush == BrushLayer.Ground)
            {
                _data.RemoveCell(cellPos);
                RemoveCellPreview(cellPos);
            }
            else
            {
                _data.RemoveObstacle(cellPos);
                var cell = _data.cells.Find(c => c.position == cellPos);
                if (cell != null) UpdateCellPreview(cell);
            }

            EditorUtility.SetDirty(_data);
            e.Use();
        }

        SceneView.RepaintAll();
    }

    private void RebuildPreview()
    {
        ClearPreview();

        _previewRoot = new GameObject("[HexMap_LivePreview]");
        // 【🔥核心修复】绝对不能挂载到 _data.transform 下面！保持在根节点，利用 HideAndDontSave 防止被保存
        _previewRoot.hideFlags = HideFlags.HideAndDontSave;

        foreach (var cell in _data.cells)
        {
            UpdateCellPreview(cell);
        }
    }

    private void ClearPreview()
    {
        // 【🔥核心修复】由于物体是隐藏且不保存的，普通的 GameObject.Find 找不到它，必须用底层搜索
        var allGOs = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var g in allGOs)
        {
            if (g != null && g.name == "[HexMap_LivePreview]")
            {
                DestroyImmediate(g);
            }
        }

        _previewDict.Clear();
        _previewRoot = null;
    }

    private void UpdateCellPreview(HexEditorCell cell)
    {
        if (cell == null) return;

        if (!_previewDict.TryGetValue(cell.position, out GameObject go) || go == null)
        {
            go = new GameObject($"Preview_{cell.position}");
            go.transform.SetParent(_previewRoot.transform);
            go.transform.position = CellToWorld(cell.position, _data.hexSize);

            go.AddComponent<SpriteRenderer>();

            GameObject obsGo = new GameObject("ObstaclePreview");
            obsGo.transform.SetParent(go.transform);
            obsGo.transform.localPosition = Vector3.zero;
            obsGo.AddComponent<SpriteRenderer>();

            _previewDict[cell.position] = go;
        }

        int baseOrder = -cell.position.y * 10;

        SpriteRenderer groundSr = go.GetComponent<SpriteRenderer>();
        if (_data.groundSprites != null && cell.groundVariantID >= 0 && cell.groundVariantID < _data.groundSprites.Length)
        {
            groundSr.sprite = _data.groundSprites[cell.groundVariantID];
            groundSr.sortingOrder = baseOrder;
        }
        else
        {
            groundSr.sprite = null;
        }

        SpriteRenderer obsSr = go.transform.Find("ObstaclePreview").GetComponent<SpriteRenderer>();
        if (cell.obstacleVariantID != -1 && _data.obstacleSprites != null && cell.obstacleVariantID >= 0 && cell.obstacleVariantID < _data.obstacleSprites.Length)
        {
            obsSr.sprite = _data.obstacleSprites[cell.obstacleVariantID];
            obsSr.sortingOrder = baseOrder + 2;
        }
        else
        {
            obsSr.sprite = null;
        }
    }

    private void RemoveCellPreview(Vector3Int pos)
    {
        if (_previewDict.TryGetValue(pos, out GameObject go))
        {
            if (go != null) DestroyImmediate(go);
            _previewDict.Remove(pos);
        }
    }

    private Vector3Int WorldToCell(Vector3 pos, float size)
    {
        float q = (Mathf.Sqrt(3f) / 3f * pos.x - 1f / 3f * pos.y) / size;
        float r = (2f / 3f * pos.y) / size;
        return CubeRound(new Vector3(q, r, -q - r));
    }

    private Vector3 CellToWorld(Vector3Int pos, float size)
    {
        float x = size * Mathf.Sqrt(3f) * (pos.x + pos.y / 2f);
        float y = size * 3f / 2f * pos.y;
        return new Vector3(x, y, 0);
    }

    private Vector3Int CubeRound(Vector3 frac)
    {
        int rx = Mathf.RoundToInt(frac.x);
        int ry = Mathf.RoundToInt(frac.y);
        int rz = Mathf.RoundToInt(frac.z);

        float xDiff = Mathf.Abs(rx - frac.x);
        float yDiff = Mathf.Abs(ry - frac.y);
        float zDiff = Mathf.Abs(rz - frac.z);

        if (xDiff > yDiff && xDiff > zDiff) rx = -ry - rz;
        else if (yDiff > zDiff) ry = -rx - rz;
        else rz = -rx - ry;

        return new Vector3Int(rx, ry, rz);
    }

    private void DrawHexagon(Vector3 center, float size)
    {
        Vector3[] corners = new Vector3[7];
        for (int i = 0; i < 6; i++)
        {
            float angle_deg = 60 * i - 30;
            float angle_rad = Mathf.PI / 180f * angle_deg;
            corners[i] = new Vector3(center.x + size * Mathf.Cos(angle_rad), center.y + size * Mathf.Sin(angle_rad), 0);
        }
        corners[6] = corners[0];
        Handles.DrawPolyLine(corners);
    }

    private void SaveAsLevelAsset()
    {
        if (_data.targetLevel == null) return;
        Undo.RecordObject(_data.targetLevel, "Update Level Data");
        _data.targetLevel.cells.Clear();
        foreach (var c in _data.cells)
        {
            _data.targetLevel.cells.Add(new HexEditorCell { position = c.position, groundVariantID = c.groundVariantID, obstacleVariantID = c.obstacleVariantID });
        }
        EditorUtility.SetDirty(_data.targetLevel);
        AssetDatabase.SaveAssets();
        Debug.Log($"<color=green>🎉 地图已保存！</color>");
    }

    private void LoadFromLevelAsset()
    {
        if (_data.targetLevel == null) return;
        Undo.RecordObject(_data, "Load Map");
        _data.cells.Clear();
        foreach (var c in _data.targetLevel.cells)
        {
            _data.cells.Add(new HexEditorCell { position = c.position, groundVariantID = c.groundVariantID, obstacleVariantID = c.obstacleVariantID });
        }
        RebuildPreview();
        EditorUtility.SetDirty(_data);
    }
}
#endif