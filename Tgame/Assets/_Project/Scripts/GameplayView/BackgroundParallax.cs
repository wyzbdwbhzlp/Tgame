using UnityEngine;

public class BackgroundParallax : MonoBehaviour
{
    [Header("位置视差 (移动缓冲)")]
    [Tooltip("移动的缓冲平滑度，值越小跟得越慢，缓冲感越强")]
    public float moveSmoothSpeed = 8f;

    [Tooltip("视差移动倍率：0=永远贴在屏幕中间(无限远的星空)，1=固定在地砖层(完全不跟镜头)。推荐 0.1~0.3")]
    [Range(0f, 1f)]
    public float parallaxEffect = 0.15f;

    [Header("缩放视差 (Zoom缓冲)")]
    public bool enableZoomParallax = true;

    [Tooltip("缩放的缓冲平滑度")]
    public float zoomSmoothSpeed = 6f;

    [Tooltip("镜头缩放时，背景的响应倍率：0=背景在屏幕上的大小永远不变，1=和地砖完全同步变大变小")]
    [Range(0f, 1f)]
    public float zoomParallaxEffect = 0.4f;

    private Camera _cam;
    private Transform _camTransform;
    private Vector3 _lastCamPos;
    private Vector3 _targetPosition;

    private float _startOrthoSize;
    private Vector3 _startScale;
    private Vector3 _targetScale;

    private void Start()
    {
        _cam = Camera.main;
        _camTransform = _cam.transform;

        // 记录初始状态
        _lastCamPos = _camTransform.position;
        _targetPosition = transform.position;

        _startOrthoSize = _cam.orthographicSize;
        _startScale = transform.localScale;
        _targetScale = _startScale;
    }

    private void LateUpdate()
    {
        if (_camTransform == null || _cam == null) return;

        // ==========================================
        // 1. 位置视差 (Pan)
        // ==========================================
        // 计算摄像机这一帧真实的位移量
        Vector3 deltaCamPos = _camTransform.position - _lastCamPos;

        // 核心算法：让背景跟着镜头移动一部分，造成“它在很远的地方”的错觉
        Vector3 parallaxMove = deltaCamPos * (1f - parallaxEffect);
        _targetPosition += parallaxMove;

        // 强制锁定 Z 轴，防止背景意外穿模或被相机裁剪
        _targetPosition.z = transform.position.z;

        // 丝滑缓冲追赶
        transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * moveSmoothSpeed);

        _lastCamPos = _camTransform.position;

        // ==========================================
        // 2. 缩放视差 (Zoom)
        // ==========================================
        if (enableZoomParallax)
        {
            // 计算当前镜头的放大缩小比例
            float zoomRatio = _cam.orthographicSize / _startOrthoSize;

            // 核心算法：让背景的缩放幅度小于真实的镜头缩放幅度
            float targetScaleFactor = Mathf.Lerp(1f, zoomRatio, zoomParallaxEffect);
            _targetScale = _startScale * targetScaleFactor;

            // 丝滑缩放缓冲
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * zoomSmoothSpeed);
        }
    }
}