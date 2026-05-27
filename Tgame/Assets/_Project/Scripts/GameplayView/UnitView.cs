using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TGame.Battle;
using DG.Tweening;
using TGame.Data;
using TGame.UI; // 引入 UI 命名空间

public class UnitView : MonoBehaviour
{
    public RuntimeUnit LogicUnit { get; private set; }
    private UnitAnimator _unitAnimator;
    private Vector3 _lastPos;
    private Sequence _moveSequence;

    private SpriteRenderer[] _renderers;
    private Material[] _originalMaterials;
    private Material _whiteFlashMat;
    private Coroutine _flashCoroutine;

    // 【🔥新增】头顶血条的引用
    private UnitHUD _hud;

    private void Awake()
    {
        _unitAnimator = GetComponentInChildren<UnitAnimator>();
        _renderers = GetComponentsInChildren<SpriteRenderer>();

        if (_renderers != null && _renderers.Length > 0)
        {
            _originalMaterials = new Material[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _originalMaterials[i] = _renderers[i].material;
            }
            _whiteFlashMat = new Material(Shader.Find("GUI/Text Shader"));
            _whiteFlashMat.color = Color.white;
        }
    }

    public void Init(RuntimeUnit unit)
    {
        LogicUnit = unit;
        if (GridSystem.Instance != null)
        {
            transform.position = GridSystem.Instance.CellToWorld(unit.GridPosition);
            _lastPos = transform.position;
        }
        if (_unitAnimator != null) _unitAnimator.PlayIdle();

        // ==========================================
        // 【🔥核心新增】自动加载并生成头顶血条
        // ==========================================
        if (_hud == null)
        {
            GameObject hudPrefab = Resources.Load<GameObject>("UI/UnitHUD");
            if (hudPrefab != null)
            {
                GameObject hudObj = Instantiate(hudPrefab, this.transform);
                // 默认抬高 1.5 个单位，刚好在角色头顶 (如果觉得太高/太低，改这里的 Y 值)
                hudObj.transform.localPosition = new Vector3(0, 1.5f, 0);
                _hud = hudObj.GetComponent<UnitHUD>();
            }
            else
            {
                Debug.LogWarning("[UnitView] 找不到头顶血条预制体！请确保制作了 Assets/Resources/UI/UnitHUD.prefab");
            }
        }

        if (_hud != null) _hud.Init(unit);
    }

    private void Update()
    {
        if (_unitAnimator != null)
        {
            float deltaX = transform.position.x - _lastPos.x;
            if (Mathf.Abs(deltaX) > 0.005f)
            {
                _unitAnimator.SetFacingDirection(deltaX > 0);
            }
            _lastPos = transform.position;
        }

        if (GridSystem.Instance != null && _renderers != null)
        {
            int currentGridY = GridSystem.Instance.WorldToCell(transform.position).y;
            int dynamicOrder = -currentGridY * 10 + 5;

            foreach (var r in _renderers)
            {
                if (r != null) r.sortingOrder = dynamicOrder;
            }
        }
    }

    public void PlayHitFlash()
    {
        if (_renderers == null || _renderers.Length == 0) return;
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        foreach (var r in _renderers) if (r != null) r.material = _whiteFlashMat;
        yield return new WaitForSeconds(0.12f);
        for (int i = 0; i < _renderers.Length; i++) if (_renderers[i] != null) _renderers[i].material = _originalMaterials[i];
    }

    public void MoveAlongPath(List<GridCell> path)
    {
        if (path == null || path.Count == 0) return;

        Vector3[] waypoints = new Vector3[path.Count];
        for (int i = 0; i < path.Count; i++) waypoints[i] = GridSystem.Instance.CellToWorld(path[i].Position);

        float moveDuration = path.Count * 0.25f;

        if (_moveSequence == null || !_moveSequence.IsActive())
        {
            _moveSequence = DOTween.Sequence();
            _moveSequence.OnStart(() => { if (_unitAnimator != null) _unitAnimator.PlayMove(); });
            _moveSequence.OnComplete(() => { if (_unitAnimator != null) _unitAnimator.PlayIdle(); });
        }

        _moveSequence.Append(transform.DOPath(waypoints, moveDuration, PathType.Linear).SetEase(Ease.Linear));
    }

    public void PlayAttackAnimation(Vector3 targetWorldPos)
    {
        KillMoveSequence();
        if (_unitAnimator != null)
        {
            float dirX = targetWorldPos.x - transform.position.x;
            if (Mathf.Abs(dirX) > 0.005f) _unitAnimator.SetFacingDirection(dirX > 0);
            _unitAnimator.PlayAttack();
        }
    }

    public void PlaySkillAnimation(Vector3 targetWorldPos)
    {
        KillMoveSequence();
        if (_unitAnimator != null)
        {
            float dirX = targetWorldPos.x - transform.position.x;
            if (Mathf.Abs(dirX) > 0.005f) _unitAnimator.SetFacingDirection(dirX > 0);
            _unitAnimator.PlaySkill();
        }
    }

    private void KillMoveSequence()
    {
        if (_moveSequence != null && _moveSequence.IsActive()) _moveSequence.Kill();
    }
}