using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using TGame.Battle;
using System.Linq;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("摄像机引用与参数")]
    public Camera cam;
    public float zoomSpeed = 2f;
    public float minZoom = 2f;
    public float maxZoom = 8f;
    public float scrollZoomDuration = 0.25f;

    [Header("聚焦参数")]
    public float focusZoom = 3.5f;
    public float tweenDuration = 0.4f;

    // ==========================================
    // 【🔥新增】战斗特写与震屏系统参数
    // ==========================================
    [Header("💥 战斗镜头表现")]
    [Tooltip("攻击时镜头拉近后的视野大小 (正交模式下越小越近)")]
    public float attackZoomSize = 3.5f;
    [Tooltip("镜头拉近和还原的动画耗时")]
    public float actionZoomDuration = 0.3f;
    [Tooltip("震屏时长")]
    public float shakeDuration = 0.2f;
    [Tooltip("震屏力度 (数值越大画面晃得越凶)")]
    public float shakeStrength = 0.4f;

    private float _defaultOrthoSize = 5f;
    private Sequence _cameraSequence;

    private Vector3 _manualPosition;
    private float _manualZoom;
    private bool _isFocusing = false;
    private bool _isHardLocked = false;

    private Transform _trackingTarget;

    private Vector3 _dragOrigin;
    private Vector3 _rightClickDownPos;
    public bool IsDragging { get; private set; }

    private void Awake()
    {
        Instance = this;
        if (cam == null) cam = Camera.main;
        if (cam != null) _defaultOrthoSize = cam.orthographicSize;
    }

    private void Start()
    {
        _manualPosition = transform.position;
        _manualZoom = cam.orthographicSize;

        if (HexMapView.Instance != null)
        {
            HexMapView.Instance.OnUnitSelected += FocusOnUnit;
            HexMapView.Instance.OnUnitDeselected += Unfocus;
        }
    }

    private void OnDestroy()
    {
        if (HexMapView.Instance != null)
        {
            HexMapView.Instance.OnUnitSelected -= FocusOnUnit;
            HexMapView.Instance.OnUnitDeselected -= Unfocus;
        }
    }

    private void Update()
    {
        bool isPlanning = TurnManager.Instance == null || TurnManager.Instance.CurrentState == TGame.Battle.BattleState.Planning;

        if (isPlanning && _isHardLocked)
        {
            _isHardLocked = false;
            Unfocus();
        }

        if (isPlanning && !_isHardLocked)
        {
            HandleZoom();
            HandlePan();
        }
    }

    private void LateUpdate()
    {
        if (_isFocusing && _trackingTarget != null)
        {
            Vector3 targetPos = _trackingTarget.position;
            targetPos.z = transform.position.z;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 10f);
        }
    }

    private void HandleZoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            BreakFocus();

            _manualZoom = Mathf.Clamp(_manualZoom - scroll * zoomSpeed, minZoom, maxZoom);

            cam.DOKill();
            cam.DOOrthoSize(_manualZoom, scrollZoomDuration).SetEase(Ease.OutQuad);
        }
    }

    private void HandlePan()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            _dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
            _rightClickDownPos = Input.mousePosition;
            IsDragging = false;
        }

        if (Input.GetMouseButton(1))
        {
            if (!IsDragging && Vector3.Distance(Input.mousePosition, _rightClickDownPos) > 5f)
            {
                IsDragging = true;
                BreakFocus();
            }

            if (IsDragging)
            {
                Vector3 difference = _dragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
                transform.position += difference;
                _manualPosition = transform.position;
            }
        }

        if (Input.GetMouseButtonUp(1))
        {
            Invoke(nameof(ResetDragState), 0.05f);
        }
    }

    private void ResetDragState() { IsDragging = false; }

    private void BreakFocus()
    {
        if (_isFocusing && !_isHardLocked)
        {
            _isFocusing = false;
            _trackingTarget = null;
            cam.DOKill();
            transform.DOKill();
            KillCameraTween(); // 清理特写动画

            _manualZoom = cam.orthographicSize;
        }
    }

    private void FocusOnUnit(RuntimeUnit unit)
    {
        if (GridSystem.Instance == null) return;

        _isFocusing = true;
        cam.DOKill();
        transform.DOKill();
        KillCameraTween();

        Transform phantomTransform = HexMapView.Instance != null ? HexMapView.Instance.GetPhantomTransform(unit.InstanceID) : null;

        if (phantomTransform != null)
        {
            _trackingTarget = phantomTransform;
        }
        else
        {
            UnitView view = FindObjectsByType<UnitView>(FindObjectsSortMode.None).FirstOrDefault(v => v.LogicUnit != null && v.LogicUnit.InstanceID == unit.InstanceID);
            if (view != null)
            {
                _trackingTarget = view.transform;
            }
            else
            {
                Vector3 targetPos = GridSystem.Instance.CellToWorld(unit.GridPosition);
                targetPos.z = transform.position.z;
                transform.DOMove(targetPos, tweenDuration).SetEase(Ease.OutCubic);
                cam.DOOrthoSize(focusZoom, tweenDuration).SetEase(Ease.OutCubic);
                return;
            }
        }

        cam.DOOrthoSize(focusZoom, tweenDuration).SetEase(Ease.OutCubic);
    }

    private void Unfocus()
    {
        if (!_isFocusing) return;
        _isFocusing = false;
        _trackingTarget = null;

        cam.DOKill();
        transform.DOKill();
        KillCameraTween();

        transform.DOMove(_manualPosition, tweenDuration).SetEase(Ease.OutCubic);
        cam.DOOrthoSize(_manualZoom, tweenDuration).SetEase(Ease.OutCubic);
    }

    public void FocusOnExecution(int unitID)
    {
        if (GridSystem.Instance == null) return;

        _isHardLocked = true;
        _isFocusing = true;

        cam.DOKill();
        transform.DOKill();
        KillCameraTween();

        UnitView view = FindObjectsByType<UnitView>(FindObjectsSortMode.None).FirstOrDefault(v => v.LogicUnit != null && v.LogicUnit.InstanceID == unitID);
        if (view != null)
        {
            _trackingTarget = view.transform;
        }

        float executeZoom = focusZoom * 0.9f;
        cam.DOOrthoSize(executeZoom, tweenDuration).SetEase(Ease.OutCubic);
    }

    // ==========================================
    // 【🔥核心新增】镜头战斗演出 API
    // ==========================================

    public void ActionZoomIn(Vector3 targetPos)
    {
        if (cam == null) return;

        KillCameraTween();
        // 暂时打断普通的跟随，完全接管镜头
        _isFocusing = false;
        _trackingTarget = null;

        _cameraSequence = DOTween.Sequence();
        _cameraSequence.Join(transform.DOMove(new Vector3(targetPos.x, targetPos.y, transform.position.z), actionZoomDuration).SetEase(Ease.OutCubic));
        _cameraSequence.Join(cam.DOOrthoSize(attackZoomSize, actionZoomDuration).SetEase(Ease.OutCubic));
    }

    public void ResetCameraZoom()
    {
        if (cam == null) return;
        KillCameraTween();

        // 恢复到玩家上一次设定的镜头缩放大小
        cam.DOOrthoSize(_manualZoom, actionZoomDuration).SetEase(Ease.InOutSine);
    }

    public void TriggerHitShake()
    {
        if (cam == null) return;
        // 震动位置，由于我们用 Z 轴，可以限制只在 X,Y 平面震动防止穿帮
        cam.transform.DOShakePosition(shakeDuration, new Vector3(shakeStrength, shakeStrength, 0), 20);
    }

    private void KillCameraTween()
    {
        if (_cameraSequence != null && _cameraSequence.IsActive())
        {
            _cameraSequence.Kill();
        }
    }
}