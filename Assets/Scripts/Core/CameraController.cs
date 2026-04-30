using Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Cinemachine")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private bool resetOffsetOnTargetSet = true;

    [Header("Drag Settings")]
    [SerializeField] private float dragSensitivity = 1f;
    [SerializeField] private float returnToTargetSpeed = 8f;
    [SerializeField] private bool invertX = false;
    [SerializeField] private bool invertY = false;

    [Header("Constraints")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minBounds = new(-50f, -50f);
    [SerializeField] private Vector2 maxBounds = new(50f, 50f);

    private Camera _mainCamera;
    private Transform _followTarget;
    private Transform _proxyTarget;
    private Vector3 _dragOffset;
    private Vector3 _lastPointerScreenPosition;
    private bool _isDragging;
    private bool _dragEnabled = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        _mainCamera = Camera.main;
        if (virtualCamera == null)
        {
            virtualCamera = FindAnyObjectByType<CinemachineVirtualCamera>();
        }

        if (virtualCamera != null && virtualCamera.Follow != null)
        {
            SetFollowTarget(virtualCamera.Follow);
        }

        if (_mainCamera == null)
        {
            Debug.LogError("[CameraController] Main Camera not found!");
        }
    }

    private void Update()
    {
        if (!_dragEnabled) return;

        if (TryGetPointerDown(out var pointerScreenPosition))
        {
            _isDragging = true;
            _lastPointerScreenPosition = pointerScreenPosition;
        }

        if (_isDragging && TryGetPointerPosition(out pointerScreenPosition))
        {
            DragCamera(pointerScreenPosition);
        }

        if (TryGetPointerUp())
        {
            _isDragging = false;
        }
    }

    private void LateUpdate()
    {
        if (_proxyTarget == null || _followTarget == null)
        {
            return;
        }

        if (!_isDragging && _dragOffset != Vector3.zero)
        {
            _dragOffset = Vector3.Lerp(_dragOffset, Vector3.zero, Time.deltaTime * returnToTargetSpeed);

            if (_dragOffset.sqrMagnitude < 0.0001f)
            {
                _dragOffset = Vector3.zero;
            }
        }

        _proxyTarget.position = _followTarget.position + _dragOffset;
    }

    private void DragCamera(Vector3 currentPointerScreenPosition)
    {
        if (_mainCamera == null || _followTarget == null)
        {
            return;
        }

        Vector3 currentWorldPosition = ScreenToWorldOnTargetPlane(currentPointerScreenPosition);
        Vector3 lastWorldPosition = ScreenToWorldOnTargetPlane(_lastPointerScreenPosition);
        Vector3 worldDelta = lastWorldPosition - currentWorldPosition;

        float worldDeltaX = worldDelta.x * dragSensitivity;
        float worldDeltaY = worldDelta.y * dragSensitivity;

        if (invertX) worldDeltaX *= -1f;
        if (invertY) worldDeltaY *= -1f;

        _dragOffset += new Vector3(worldDeltaX, worldDeltaY, 0f);

        if (useBounds)
        {
            Vector3 targetPosition = _followTarget.position + _dragOffset;
            targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
            _dragOffset = targetPosition - _followTarget.position;
        }

        _lastPointerScreenPosition = currentPointerScreenPosition;
    }

    /// <summary>
    /// Set camera bounds for constraining dragging area
    /// </summary>
    public void SetBounds(Vector2 min, Vector2 max, bool enable = true)
    {
        minBounds = min;
        maxBounds = max;
        useBounds = enable;
    }

    /// <summary>
    /// Enable/disable camera dragging
    /// </summary>
    public void SetDragEnabled(bool enabled)
    {
        _dragEnabled = enabled;
        if (!enabled)
        {
            _isDragging = false;
        }
    }

    public void SetFollowTarget(Transform target)
    {
        _followTarget = target;

        if (virtualCamera == null)
        {
            return;
        }

        if (_proxyTarget == null)
        {
            GameObject proxyObject = new GameObject("CameraFollowProxy");
            proxyObject.transform.SetParent(transform);
            _proxyTarget = proxyObject.transform;
        }

        if (resetOffsetOnTargetSet)
        {
            _dragOffset = Vector3.zero;
        }

        _proxyTarget.position = _followTarget.position + _dragOffset;
        virtualCamera.Follow = _proxyTarget;
    }

    public void ResetOffset()
    {
        _dragOffset = Vector3.zero;

        if (_proxyTarget != null && _followTarget != null)
        {
            _proxyTarget.position = _followTarget.position;
        }
    }

    /// <summary>
    /// Reset camera to initial position
    /// </summary>
    public void ResetPosition(Vector3 newPosition)
    {
        if (_proxyTarget != null)
        {
            Vector3 pos = newPosition;
            if (useBounds)
            {
                pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
                pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
            }

            _dragOffset = _followTarget != null ? pos - _followTarget.position : pos;
            _proxyTarget.position = pos;
        }
    }

    private bool TryGetPointerDown(out Vector3 pointerScreenPosition)
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            pointerScreenPosition = touch.position;
            return touch.phase == TouchPhase.Began;
        }

        pointerScreenPosition = Input.mousePosition;
        return Input.GetMouseButtonDown(0);
    }

    private bool TryGetPointerPosition(out Vector3 pointerScreenPosition)
    {
        if (Input.touchCount > 0)
        {
            pointerScreenPosition = Input.GetTouch(0).position;
            return true;
        }

        pointerScreenPosition = Input.mousePosition;
        return Input.GetMouseButton(0);
    }

    private bool TryGetPointerUp()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            return touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
        }

        return Input.GetMouseButtonUp(0);
    }

    private Vector3 ScreenToWorldOnTargetPlane(Vector3 pointerScreenPosition)
    {
        if (_mainCamera == null)
        {
            return Vector3.zero;
        }

        float targetZ = _followTarget != null ? _followTarget.position.z : 0f;
        float distanceToPlane = Mathf.Abs(_mainCamera.transform.position.z - targetZ);
        Vector3 screenPosition = new Vector3(pointerScreenPosition.x, pointerScreenPosition.y, distanceToPlane);
        return _mainCamera.ScreenToWorldPoint(screenPosition);
    }
}
