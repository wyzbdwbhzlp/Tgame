using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TGame.Battle;
using TGame.Data;
using TGame.Core;

[RequireComponent(typeof(LineRenderer))]
public class HexMapView : MonoBehaviour
{
    public static HexMapView Instance { get; private set; }

    [Header("真实美术资产配置")]
    public MapVisualConfigSO visualConfig;

    public enum InteractionMode
    {
        SelectUnit,
        SelectMoveTarget,
        SelectAttackTarget,
        SelectSkillTarget
    }

    public InteractionMode CurrentMode { get; private set; } = InteractionMode.SelectUnit;

    public event Action<RuntimeUnit> OnUnitSelected;
    public event Action OnUnitDeselected;

    private Vector3Int _hoveredCellPos;
    private Vector3Int _lastHoveredPos = new Vector3Int(999, 999, 999);

    private RuntimeUnit _selectedUnit = null;
    public RuntimeUnit SelectedUnit => _selectedUnit;

    private int _currentSkillID = -1;

    private Dictionary<Vector3Int, GameObject> _cellVisuals = new Dictionary<Vector3Int, GameObject>();
    private Dictionary<Vector3Int, SpriteRenderer> _cellHighlights = new Dictionary<Vector3Int, SpriteRenderer>();

    private LineRenderer _previewLineRenderer;
    private Dictionary<int, GameObject> _phantomDict = new Dictionary<int, GameObject>();

    private void Awake() { Instance = this; }

    private void Start()
    {
        _previewLineRenderer = GetComponent<LineRenderer>();
        _previewLineRenderer.positionCount = 0;
        _previewLineRenderer.startWidth = 0.08f;
        _previewLineRenderer.endWidth = 0.08f;
        _previewLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _previewLineRenderer.startColor = Color.yellow;
        _previewLineRenderer.endColor = Color.white;
        _previewLineRenderer.sortingOrder = 3000;
    }

    public void CreateGridVisuals()
    {
        if (GridSystem.Instance == null || visualConfig == null)
        {
            Debug.LogWarning("[HexMapView] 未挂载 MapVisualConfigSO 或 GridSystem 为空！");
            return;
        }

        Sprite solidHexSprite = CreateSolidHexSprite();

        Sprite[] groundSprites = visualConfig.groundSprites;
        Sprite[] obstacleSprites = visualConfig.obstacleSprites;

        foreach (var kvp in GridSystem.Instance.GetAllCells())
        {
            Vector3Int pos = kvp.Key;
            GridCell cellData = kvp.Value;

            GameObject cellObj = new GameObject($"Cell_{pos.x}_{pos.y}");
            cellObj.transform.SetParent(this.transform);
            cellObj.transform.position = GridSystem.Instance.CellToWorld(pos);

            int baseOrder = -pos.y * 10;

            SpriteRenderer groundSr = cellObj.AddComponent<SpriteRenderer>();
            if (groundSprites != null && cellData.GroundVariantID >= 0 && cellData.GroundVariantID < groundSprites.Length && groundSprites[cellData.GroundVariantID] != null)
            {
                groundSr.sprite = groundSprites[cellData.GroundVariantID];
                groundSr.color = Color.white;
            }
            else groundSr.color = Color.clear;

            groundSr.sortingOrder = baseOrder;
            _cellVisuals[pos] = cellObj;

            if (cellData.ObstacleVariantID != -1 && obstacleSprites != null && cellData.ObstacleVariantID >= 0 && cellData.ObstacleVariantID < obstacleSprites.Length && obstacleSprites[cellData.ObstacleVariantID] != null)
            {
                GameObject obsObj = new GameObject("Obstacle");
                obsObj.transform.SetParent(cellObj.transform);
                obsObj.transform.localPosition = Vector3.zero;

                SpriteRenderer obsSr = obsObj.AddComponent<SpriteRenderer>();
                obsSr.sprite = obstacleSprites[cellData.ObstacleVariantID];
                obsSr.color = Color.white;
                obsSr.sortingOrder = baseOrder + 2;
            }

            GameObject highlightObj = new GameObject("HighlightFilter");
            highlightObj.transform.SetParent(cellObj.transform);
            highlightObj.transform.localPosition = Vector3.zero;
            highlightObj.transform.localRotation = Quaternion.Euler(0, 0, 90);

            SpriteRenderer highlightSr = highlightObj.AddComponent<SpriteRenderer>();
            highlightSr.sprite = solidHexSprite;
            highlightSr.color = Color.clear;
            highlightSr.sortingOrder = cellData.IsWalkable ? baseOrder + 1 : baseOrder + 3;

            _cellHighlights[pos] = highlightSr;
        }
    }

    private Sprite CreateSolidHexSprite()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size);
        Color transparent = new Color(0, 0, 0, 0);
        for (int i = 0; i < size * size; i++) tex.SetPixel(i % size, i / size, transparent);

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float r = size * 0.48f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 pos = new Vector2(x, y) - center;
                float absX = Mathf.Abs(pos.x);
                float absY = Mathf.Abs(pos.y);
                float hexDist = Mathf.Max(absX * 0.866025f + absY * 0.5f, absY);

                if (hexDist <= r) tex.SetPixel(x, y, Color.white);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 128f / (GridSystem.Instance.HexSize * 2f));
    }

    private void Update()
    {
        if (GridSystem.Instance == null || TurnManager.Instance == null) return;
        if (TurnManager.Instance.CurrentState != TGame.Battle.BattleState.Planning)
        {
            if (_previewLineRenderer != null) _previewLineRenderer.positionCount = 0;
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        _hoveredCellPos = GridSystem.Instance.WorldToCell(mouseWorldPos);

        if (_hoveredCellPos != _lastHoveredPos)
        {
            _lastHoveredPos = _hoveredCellPos;

            if (CurrentMode == InteractionMode.SelectMoveTarget && _selectedUnit != null)
            {
                UpdatePreviewPath();
            }
            else if (CurrentMode == InteractionMode.SelectSkillTarget && _selectedUnit != null)
            {
                RefreshSkillHighlights(_hoveredCellPos);
                _previewLineRenderer.positionCount = 0;
            }
            else
            {
                _previewLineRenderer.positionCount = 0;
            }
        }

        if (Input.GetMouseButtonDown(0)) HandleLeftClick();

        if (Input.GetMouseButtonUp(1))
        {
            if (CameraController.Instance != null && CameraController.Instance.IsDragging) return;
            HandleRightClick();
        }
    }

    // ==========================================
    // 【🔥核心升级】接入 UI_BattleMain 的错误提示播报
    // ==========================================
    private void HandleLeftClick()
    {
        GridCell clickedCell = GridSystem.Instance.GetCell(_hoveredCellPos);
        if (clickedCell == null) return;

        switch (CurrentMode)
        {
            case InteractionMode.SelectUnit:
                if (clickedCell.OccupantUnitID != -1)
                {
                    _selectedUnit = UnitManager.Instance.GetUnit(clickedCell.OccupantUnitID);
                    if (_selectedUnit != null) OnUnitSelected?.Invoke(_selectedUnit);
                }
                break;

            case InteractionMode.SelectMoveTarget:
                if (_hoveredCellPos == _selectedUnit.GridPosition) return;
                MoveCommand moveCmd = new MoveCommand(_selectedUnit.InstanceID, _selectedUnit.GridPosition, _hoveredCellPos);

                if (moveCmd.Validate())
                {
                    TurnManager.Instance.AddCommand(moveCmd);
                    CancelSelection();
                }
                else
                {
                    // 【🔥提示】移动路径太长或被阻挡
                    UI_BattleMain.Instance?.ShowBroadcastMessage("无法到达！(时素TU不足或被阻挡)");
                }
                break;

            case InteractionMode.SelectAttackTarget:
                if (clickedCell.OccupantUnitID != -1 && clickedCell.OccupantUnitID != _selectedUnit.InstanceID)
                {
                    AttackCommand atkCmd = new AttackCommand(_selectedUnit.InstanceID, clickedCell.OccupantUnitID);

                    if (atkCmd.Validate())
                    {
                        TurnManager.Instance.AddCommand(atkCmd);
                        CancelSelection();
                    }
                    else
                    {
                        // 【🔥提示】超过攻击距离
                        UI_BattleMain.Instance?.ShowBroadcastMessage("无法攻击！(超出攻击距离或TU不足)");
                    }
                }
                else
                {
                    UI_BattleMain.Instance?.ShowBroadcastMessage("该位置没有可攻击的目标！");
                }
                break;

            case InteractionMode.SelectSkillTarget:
                SkillData skillData = DataManager.Instance.GetSkillData(_currentSkillID);
                if (skillData == null) return;

                int dist = GetHexDistance(_selectedUnit.GridPosition, _hoveredCellPos);
                if (dist <= 0 || dist > skillData.castRange)
                {
                    // 【🔥提示】将 Debug.Log 转换为 UI 播报
                    UI_BattleMain.Instance?.ShowBroadcastMessage("超出施法距离！");
                    return;
                }

                RuntimeUnit targetUnit = clickedCell.OccupantUnitID != -1 ? UnitManager.Instance.GetUnit(clickedCell.OccupantUnitID) : null;
                bool isValidTarget = false;

                if (targetUnit == null)
                {
                    if (skillData.targetMask.HasFlag(SkillTargetMask.Empty)) isValidTarget = true;
                }
                else if (targetUnit.Side == _selectedUnit.Side)
                {
                    if (skillData.targetMask.HasFlag(SkillTargetMask.Ally)) isValidTarget = true;
                }
                else
                {
                    if (skillData.targetMask.HasFlag(SkillTargetMask.Enemy)) isValidTarget = true;
                }

                if (!isValidTarget)
                {
                    // 【🔥提示】技能无法对这种实体生效
                    UI_BattleMain.Instance?.ShowBroadcastMessage("当前技能无法指定该目标！");
                    return;
                }

                SkillCommand skillCmd = new SkillCommand(_selectedUnit.InstanceID, _hoveredCellPos, _currentSkillID);
                if (skillCmd.Validate())
                {
                    TurnManager.Instance.AddCommand(skillCmd);
                    CancelSelection();
                }
                else
                {
                    // 【🔥提示】MP或TU资源不足
                    UI_BattleMain.Instance?.ShowBroadcastMessage($"资源不足！需 {skillData.tuCost}TU / {skillData.mpCost}MP");
                }
                break;
        }
    }

    private void HandleRightClick()
    {
        if (_selectedUnit == null) return;

        if (CurrentMode != InteractionMode.SelectUnit)
        {
            CurrentMode = InteractionMode.SelectUnit;
            _previewLineRenderer.positionCount = 0;
            ResetGridVisuals();
            OnUnitSelected?.Invoke(_selectedUnit);
        }
        else CancelSelection();
    }

    public void EnterMoveMode() { CurrentMode = InteractionMode.SelectMoveTarget; }

    public void EnterAttackMode()
    {
        CurrentMode = InteractionMode.SelectAttackTarget;
        if (_selectedUnit != null) ShowHighlightRange(_selectedUnit.GridPosition, _selectedUnit.ConfigData.attackRange, new Color(1f, 0.2f, 0.2f, 0.5f));
    }

    public void EnterSkillMode(int skillID)
    {
        CurrentMode = InteractionMode.SelectSkillTarget;
        _currentSkillID = skillID;
        RefreshSkillHighlights(_lastHoveredPos);
    }

    private void RefreshSkillHighlights(Vector3Int hoverPos)
    {
        if (_selectedUnit == null) return;
        SkillData skillData = DataManager.Instance.GetSkillData(_currentSkillID);
        if (skillData == null) return;

        ResetGridVisuals();

        bool isHoverValid = GetHexDistance(_selectedUnit.GridPosition, hoverPos) <= skillData.castRange && GetHexDistance(_selectedUnit.GridPosition, hoverPos) > 0;

        foreach (var kvp in _cellHighlights)
        {
            Vector3Int pos = kvp.Key;
            int distToCaster = GetHexDistance(_selectedUnit.GridPosition, pos);
            int distToHover = GetHexDistance(hoverPos, pos);

            bool inCastRange = distToCaster > 0 && distToCaster <= skillData.castRange;
            bool inAoe = distToHover <= skillData.aoeRadius;

            if (isHoverValid && inAoe)
            {
                if (kvp.Value != null) kvp.Value.color = new Color(1f, 0.4f, 0f, 0.65f);
            }
            else if (inCastRange)
            {
                if (kvp.Value != null) kvp.Value.color = new Color(0.2f, 0.6f, 1f, 0.35f);
            }
        }
    }

    public void ShowHighlightRange(Vector3Int centerPos, int range, Color highlightColor)
    {
        ResetGridVisuals();
        foreach (var kvp in _cellHighlights)
        {
            Vector3Int pos = kvp.Key;
            int dist = GetHexDistance(centerPos, pos);
            if (dist > 0 && dist <= range)
            {
                if (kvp.Value != null) kvp.Value.color = highlightColor;
            }
        }
    }

    public void ResetGridVisuals()
    {
        foreach (var highlightSr in _cellHighlights.Values)
        {
            if (highlightSr != null) highlightSr.color = Color.clear;
        }
    }

    public void CancelSelection()
    {
        _selectedUnit = null;
        _currentSkillID = -1;
        CurrentMode = InteractionMode.SelectUnit;
        _previewLineRenderer.positionCount = 0;
        ResetGridVisuals();
        OnUnitDeselected?.Invoke();
    }

    private void UpdatePreviewPath()
    {
        if (_selectedUnit == null) return;
        List<GridCell> path = PathfindingService.GetPath(GridSystem.Instance, _selectedUnit.GridPosition, _hoveredCellPos);

        if (path == null || path.Count == 0)
        {
            _previewLineRenderer.positionCount = 0;
            return;
        }

        _previewLineRenderer.positionCount = path.Count + 1;
        _previewLineRenderer.SetPosition(0, GridSystem.Instance.CellToWorld(_selectedUnit.GridPosition));

        for (int i = 0; i < path.Count; i++)
        {
            _previewLineRenderer.SetPosition(i + 1, GridSystem.Instance.CellToWorld(path[i].Position));
        }
    }

    public void UpdatePhantom(int unitID, Vector3Int targetPos)
    {
        if (_phantomDict.TryGetValue(unitID, out GameObject phantom))
        {
            phantom.transform.position = GridSystem.Instance.CellToWorld(targetPos);
            int newOrder = -targetPos.y * 10 + 5;
            foreach (var sr in phantom.GetComponentsInChildren<SpriteRenderer>()) sr.sortingOrder = newOrder;
            return;
        }

        var unit = UnitManager.Instance.GetUnit(unitID);
        if (unit == null || unit.ConfigData.characterPrefab == null) return;

        phantom = Instantiate(unit.ConfigData.characterPrefab);
        phantom.name = $"[Phantom] {unit.ConfigData.characterName}";
        phantom.transform.position = GridSystem.Instance.CellToWorld(targetPos);

        var unitView = phantom.GetComponent<UnitView>();
        if (unitView != null) Destroy(unitView);

        var animator = phantom.GetComponentInChildren<Animator>();
        if (animator != null) animator.speed = 0;

        int phantomOrder = -targetPos.y * 10 + 5;
        SpriteRenderer[] renderers = phantom.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in renderers)
        {
            Color c = sr.color;
            c.a = 0.5f;
            sr.color = c;
            sr.sortingOrder = phantomOrder;
        }

        _phantomDict[unitID] = phantom;
    }

    public LineRenderer CreatePathLineSegment(int unitID, Vector3Int startPos, List<GridCell> path)
    {
        if (path == null || path.Count == 0) return null;

        var unit = UnitManager.Instance.GetUnit(unitID);
        string uName = unit != null ? unit.ConfigData.characterName : unitID.ToString();

        GameObject lineObj = new GameObject($"[PathLineSegment] {uName}");
        lineObj.transform.SetParent(this.transform);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.startWidth = 0.08f;
        lr.endWidth = 0.08f;
        lr.material = _previewLineRenderer.material;
        lr.startColor = new Color(0f, 1f, 1f, 0.6f);
        lr.endColor = new Color(1f, 1f, 1f, 0.6f);
        lr.sortingOrder = 2999;

        lr.positionCount = path.Count + 1;
        lr.SetPosition(0, GridSystem.Instance.CellToWorld(startPos));
        for (int i = 0; i < path.Count; i++)
        {
            lr.SetPosition(i + 1, GridSystem.Instance.CellToWorld(path[i].Position));
        }

        return lr;
    }

    public void ForceSelectUnit(RuntimeUnit unit)
    {
        if (unit == null) return;

        CancelSelection();

        _selectedUnit = unit;
        CurrentMode = InteractionMode.SelectUnit;

        OnUnitSelected?.Invoke(_selectedUnit);
    }

    public void ClearPhantom(int unitID)
    {
        if (_phantomDict.TryGetValue(unitID, out GameObject phantom))
        {
            Destroy(phantom);
            _phantomDict.Remove(unitID);
        }
    }

    public void ClearPhantom()
    {
        foreach (var phantom in _phantomDict.Values) Destroy(phantom);
        _phantomDict.Clear();
    }

    public Transform GetPhantomTransform(int unitID)
    {
        if (_phantomDict.TryGetValue(unitID, out GameObject phantom)) return phantom.transform;
        return null;
    }

    private int GetHexDistance(Vector3Int a, Vector3Int b)
    {
        return (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z)) / 2;
    }
}