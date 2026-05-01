using UnityEngine;
using Cinemachine;
public class LocalPlayerController : PlayerController
{
    public const float SPEED = 7f;
    private const float INPUT_SEND_INTERVAL = 1f / 30f;
    private bool isGrounded = false;
    private float timer = 0f;
    private bool isFlying = true;
    public bool isBoom = false;
    private float _inputSendTimer = 0f;
    private bool _isInputSendLoopRunning = false;
    private C_InputPacket _latestPendingInput;
    private void Awake()
    {
        if (CameraController.Instance != null)
        {
            CameraController.Instance.SetFollowTarget(transform);
        }
        else
        {
            FindAnyObjectByType<CinemachineVirtualCamera>().Follow = transform;
        }
    }
    private void FixedUpdate()
    {
        _inputSendTimer += Time.fixedDeltaTime;
        if (_inputSendTimer >= INPUT_SEND_INTERVAL)
        {
            _inputSendTimer = 0f;
            verifyPositionWithServer();
        }
        Move();
        ApplyGravity(); 
        timer += Time.fixedDeltaTime;
        if (isFlying && timer > 1f)
        {
            isFlying = false;
        }
    }
    private void Update()
    {
        useBoom();
    }
    public override void Move()
    {
        if (isBoom)
        {
            Rb.velocity = Vector2.zero;
            return;
        }
        MoveX();
        MoveY();
        FacingDirection(InputManager.Instance.MoveInput);
    }
    private void MoveY()
    {
        if (InputManager.Instance.MoveInput.y > 0)
        {
            Rb.velocity = new Vector2(Rb.velocity.x, SPEED);
            isGrounded = false;
            isFlying = true;
            timer = 0f;
        }
        else if (InputManager.Instance.MoveInput.y == 0 && isFlying)
        {
            Rb.velocity = new Vector2(Rb.velocity.x, 0);
        }
    }
    private void MoveX()
    {
        if (InputManager.Instance.MoveInput.x != 0)
        {
            Rb.velocity = new Vector2(InputManager.Instance.MoveInput.x * SPEED , Rb.velocity.y);
            if (isFlying)
            {
                isGrounded = false;
                isFlying = true;
                timer = 0f;
            }
        }
        else
        {
            Rb.velocity = new Vector2(0, Rb.velocity.y);
        }
    }
    private void useBoom()
    {
        if (InputManager.Instance.number1)
        {
            isBoom = true;
        }
    }
    private void ApplyGravity()
    {
        if (isFlying || isBoom)
        {
            return;
        }
        Rb.velocity += new Vector2(0, -1f);
    }
    private void verifyPositionWithServer()
    {
        // Gửi vị trí hiện tại đến server để xác nhận
        if (NetworkManager.Instance == null || !NetworkManager.Instance.IsConnected)
        {
            return;
        }

        var currentPos = transform.position;

        var packet = new C_InputPacket
        {
            DirX = InputManager.Instance.MoveInput.x,
            DirY = InputManager.Instance.MoveInput.y,
            Fly = InputManager.Instance.MoveInput.y > 0,
            isNumber1 = InputManager.Instance.number1,
            Attack = false,
            Tick = 0,
            PlayerState = new PlayerState
            {
                X = currentPos.x,
                Y = currentPos.y,
                VelX = Rb.velocity.x,
                VelY = Rb.velocity.y,
                AnimState = GetAnimationState()
            },
            DeltaTime = Time.fixedDeltaTime,
        };

        QueueLatestInput(packet);
    }

    private void QueueLatestInput(C_InputPacket packet)
    {
        _latestPendingInput = packet;
        if (_isInputSendLoopRunning)
        {
            return;
        }

        _ = SendInputLoopAsync();
    }

    private async System.Threading.Tasks.Task SendInputLoopAsync()
    {
        _isInputSendLoopRunning = true;
        try
        {
            while (_latestPendingInput != null && NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
            {
                var packet = _latestPendingInput;
                _latestPendingInput = null;
                await NetworkManager.Instance.SendAsync(packet);
            }
        }
        finally
        {
            _isInputSendLoopRunning = false;
        }
    }

    private string GetAnimationState()
    {
        if (isBoom)
        {
            return "boom";
        }
        else if (Rb.velocity.x != 0)
        {
            return "run";
        }
        else
        {
            return "idle";
        }
    }
}