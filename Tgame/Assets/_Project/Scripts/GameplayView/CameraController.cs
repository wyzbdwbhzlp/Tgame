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

    // 【🔥修复】记录本次右键点击是否合法（是不是点在了空白处而不是 UI 上）
    private bool _isValidDragStart = false;

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

    // ==========================================
    // 【🔥核心修复】绝对安全的拖拽逻辑，死锁 Z 轴并拦截 UI 透传
    // ==========================================
    private void HandlePan()
    {
        if (Input.GetMouseButtonDown(1))
        {
            // 如果点在 UI 上，标记本次拖拽不合法，直接返回
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                _isValidDragStart = false;
                return;
            }

            _isValidDragStart = true;
            _dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
            _dragOrigin.z = 0f; // 强行把基准点的 Z 轴拍扁到 0
            _rightClickDownPos = Input.mousePosition;
            IsDragging = false;
        }

        // 只有在合法起点的情况下，才允许持续拖拽
        if (Input.GetMouseButton(1) && _isValidDragStart)
        {
            if (!IsDragging && Vector3.Distance(Input.mousePosition, _rightClickDownPos) > 5f)
            {
                IsDragging = true;
                BreakFocus();
            }

            if (IsDragging)
            {
                Vector3 currentWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
                currentWorldPos.z = 0f; // 强行把当前鼠标的 Z 轴拍扁到 0

                Vector3 difference = _dragOrigin - currentWorldPos;
                difference.z = 0f; // 绝对锁定，不让差值里带有一丁点 Z 轴偏移！

                transform.position += difference;
                _manualPosition = transform.position;
            }
        }

        if (Input.GetMouseButtonUp(1))
        {
            _isValidDragStart = false; // 抬起鼠标时重置标记
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
            KillCameraTween();

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

    public void ActionZoomIn(Vector3 targetPos)
    {
        if (cam == null) return;

        KillCameraTween();
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

        cam.DOOrthoSize(_manualZoom, actionZoomDuration).SetEase(Ease.InOutSine);
    }

    public void TriggerHitShake()
    {
        if (cam == null) return;
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