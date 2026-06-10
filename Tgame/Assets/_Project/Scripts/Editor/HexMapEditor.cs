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

        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        RebuildPreview();
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoRedo;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

        if (target == null)
        {
            ClearPreview();
        }
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
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

        // 根据不同图层，显示不同的样式下拉菜单
        if (_currentBrush == BrushLayer.Ground && _data.groundVariantNames.Length > 0)
            _currentVariant = EditorGUILayout.Popup("地块样式:", _currentVariant, _data.groundVariantNames);
        else if (_currentBrush == BrushLayer.Obstacle && _data.obstacleVariantNames.Length > 0)
            _currentVariant = EditorGUILayout.Popup("障碍物样式:", _currentVariant, _data.obstacleVariantNames);
        else if (_currentBrush == BrushLayer.Player && _data.playerVariantNames.Length > 0)
            _currentVariant = EditorGUILayout.Popup("玩家角色:", _currentVariant, _data.playerVariantNames);
        else if (_currentBrush == BrushLayer.Enemy && _data.enemyVariantNames.Length > 0)
            _currentVariant = EditorGUILayout.Popup("敌方角色:", _currentVariant, _data.enemyVariantNames);

        GUILayout.Space(15);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("💣 清空画布", GUILayout.Height(30)))
        {
            Undo.RecordObject(_data, "Clear Map");
            _data.cells.Clear();
            _data.playerSpawns.Clear();
            _data.enemySpawns.Clear();
            RebuildPreview();
            EditorUtility.SetDirty(_data);
        }

        if (GUILayout.Button("🔄 从目标关卡读取", GUILayout.Height(30))) LoadFromLevelAsset();
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button("📦 将地图数据覆盖至【Target Level】", GUILayout.Height(40))) SaveAsLevelAsset();
        GUI.backgroundColor = Color.white;

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("【画笔逻辑说明】\n- 地板/障碍物：左键刷，右键挖。\n- 角色/敌人：左键放置部署点，右键取消部署点。", MessageType.Info);
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

        Handles.color = _currentBrush == BrushLayer.Ground ? Color.green : (_currentBrush == BrushLayer.Obstacle ? Color.red : Color.yellow);
        DrawHexagon(snapPos, _data.hexSize);

        GUIStyle hoverStyle = new GUIStyle();
        hoverStyle.normal.textColor = Color.yellow;
        hoverStyle.fontSize = 14;
        hoverStyle.fontStyle = FontStyle.Bold;
        hoverStyle.alignment = TextAnchor.LowerCenter;
        Handles.Label(snapPos + Vector3.up * (_data.hexSize * 0.9f), $"({cellPos.x}, {cellPos.y}, {cellPos.z})", hoverStyle);

        // ==========================================
        // 在场景中直接画出角色出生点的预览 (蓝盘=玩家，红盘=敌人)
        // ==========================================
        DrawSpawnPreviews();

        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
        {
            Undo.RecordObject(_data, "Paint Hex");

            if (_currentBrush == BrushLayer.Ground) _data.SetGround(cellPos, _currentVariant);
            else if (_currentBrush == BrushLayer.Obstacle) _data.SetObstacle(cellPos, _currentVariant);
            else if (_currentBrush == BrushLayer.Player) _data.SetPlayer(cellPos, _data.playerVariantIDs[_currentVariant]);
            else if (_currentBrush == BrushLayer.Enemy) _data.SetEnemy(cellPos, _data.enemyVariantIDs[_currentVariant]);

            if (_currentBrush == BrushLayer.Ground || _currentBrush == BrushLayer.Obstacle)
            {
                var newCell = _data.cells.Find(c => c.position == cellPos);
                UpdateCellPreview(newCell);
            }

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
            else if (_currentBrush == BrushLayer.Obstacle)
            {
                _data.RemoveObstacle(cellPos);
                var cell = _data.cells.Find(c => c.position == cellPos);
                if (cell != null) UpdateCellPreview(cell);
            }
            else
            {
                _data.RemoveUnitSpawn(cellPos); // 擦除角色
            }

            EditorUtility.SetDirty(_data);
            e.Use();
        }

        SceneView.RepaintAll();
    }

    private void DrawSpawnPreviews()
    {
        GUIStyle textStyle = new GUIStyle();
        textStyle.normal.textColor = Color.white;
        textStyle.alignment = TextAnchor.MiddleCenter;
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.fontSize = 14;

        foreach (var p in _data.playerSpawns)
        {
            Vector3 pos = CellToWorld(p.position, _data.hexSize);
            Handles.color = new Color(0.2f, 0.6f, 1f, 0.8f);
            Handles.DrawSolidDisc(pos, Vector3.forward, _data.hexSize * 0.45f);
            Handles.Label(pos, $"P {p.unitID}", textStyle);
        }

        foreach (var e in _data.enemySpawns)
        {
            Vector3 pos = CellToWorld(e.position, _data.hexSize);
            Handles.color = new Color(1f, 0.2f, 0.2f, 0.8f);
            Handles.DrawSolidDisc(pos, Vector3.forward, _data.hexSize * 0.45f);
            Handles.Label(pos, $"E {e.unitID}", textStyle);
        }
    }

    private void RebuildPreview()
    {
        ClearPreview();
        _previewRoot = new GameObject("[HexMap_LivePreview]");
        _previewRoot.hideFlags = HideFlags.HideAndDontSave;
        foreach (var cell in _data.cells) UpdateCellPreview(cell);
    }

    private void ClearPreview()
    {
        var allGOs = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var g in allGOs)
        {
            if (g != null && g.name == "[HexMap_LivePreview]") DestroyImmediate(g);
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
        if (_data.visualConfig != null && _data.visualConfig.groundSprites != null && cell.groundVariantID >= 0 && cell.groundVariantID < _data.visualConfig.groundSprites.Length)
        {
            groundSr.sprite = _data.visualConfig.groundSprites[cell.groundVariantID];
            groundSr.sortingOrder = baseOrder;
        }
        else groundSr.sprite = null;

        SpriteRenderer obsSr = go.transform.Find("ObstaclePreview").GetComponent<SpriteRenderer>();
        if (_data.visualConfig != null && cell.obstacleVariantID != -1 && _data.visualConfig.obstacleSprites != null && cell.obstacleVariantID >= 0 && cell.obstacleVariantID < _data.visualConfig.obstacleSprites.Length)
        {
            obsSr.sprite = _data.visualConfig.obstacleSprites[cell.obstacleVariantID];
            obsSr.sortingOrder = baseOrder + 2;
        }
        else obsSr.sprite = null;
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

        // 1. 存地图地形
        _data.targetLevel.cells.Clear();
        foreach (var c in _data.cells)
        {
            _data.targetLevel.cells.Add(new HexEditorCell { position = c.position, groundVariantID = c.groundVariantID, obstacleVariantID = c.obstacleVariantID });
        }

        // 2. 存玩家出生点
        _data.targetLevel.playerSpawns.Clear();
        foreach (var p in _data.playerSpawns)
        {
            // 【🔥核心修复】使用 UnitSpawnInfo！
            _data.targetLevel.playerSpawns.Add(new UnitSpawnInfo { characterID = p.unitID, spawnPos = p.position });
        }

        // 3. 存敌人出生点
        _data.targetLevel.enemySpawns.Clear();
        foreach (var e in _data.enemySpawns)
        {
            _data.targetLevel.enemySpawns.Add(new EnemySpawnInfo { enemyID = e.unitID, spawnPos = e.position });
        }

        EditorUtility.SetDirty(_data.targetLevel);
        AssetDatabase.SaveAssets();
        Debug.Log($"<color=green>🎉 地图及出生点已全部保存！</color>");
    }

    private void LoadFromLevelAsset()
    {
        if (_data.targetLevel == null) return;
        Undo.RecordObject(_data, "Load Map");

        _data.cells.Clear();
        foreach (var c in _data.targetLevel.cells)
            _data.cells.Add(new HexEditorCell { position = c.position, groundVariantID = c.groundVariantID, obstacleVariantID = c.obstacleVariantID });

        _data.playerSpawns.Clear();
        foreach (var p in _data.targetLevel.playerSpawns)
            _data.playerSpawns.Add(new EditorUnitSpawn { position = p.spawnPos, unitID = p.characterID });

        _data.enemySpawns.Clear();
        foreach (var e in _data.targetLevel.enemySpawns)
            _data.enemySpawns.Add(new EditorUnitSpawn { position = e.spawnPos, unitID = e.enemyID });

        RebuildPreview();
        EditorUtility.SetDirty(_data);
    }
}
#endif