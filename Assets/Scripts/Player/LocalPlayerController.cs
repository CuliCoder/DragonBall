using UnityEngine;
using Cinemachine;
public class LocalPlayerController : PlayerController
{
    public const float SPEED = 5f;
    private const float INPUT_SEND_INTERVAL = 1f / 30f;
    public Vector2 Velocity = Vector2.zero;
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
        
        ApplyGravity();
        transform.position += (Vector3)Velocity;
        _inputSendTimer += Time.fixedDeltaTime;
        if (_inputSendTimer >= INPUT_SEND_INTERVAL)
        {
            _inputSendTimer = 0f;
            verifyPositionWithServer();
        }
        timer += Time.fixedDeltaTime;
        if (isFlying && timer > 1f)
        {
            isFlying = false;
        }
    }
    private void Update()
    {
        useBoom();
        Move();
    }
    public override void Move()
    {
        if(isBoom)
        {
            Velocity = Vector2.zero;
            return;
        }
        if (InputManager.Instance.MoveInput.x != 0)
        {
            Velocity = new Vector2(InputManager.Instance.MoveInput.x * SPEED * Time.fixedDeltaTime, Velocity.y);
        }
        else
        {
            Velocity = new Vector2(0, Velocity.y);
        }
        if (InputManager.Instance.MoveInput.y > 0)
        {
            Velocity = new Vector2(Velocity.x, 0.1f);
            isGrounded = false;
            isFlying = true;
            timer = 0f;
        }
        else if (InputManager.Instance.MoveInput.x != 0 && InputManager.Instance.MoveInput.y == 0 && isFlying)
        {
            Velocity = new Vector2(Velocity.x, 0);
            isGrounded = false;
            isFlying = true;
            timer = 0f;
        }
        else if (InputManager.Instance.MoveInput.y <= 0 && isFlying)
        {
            Velocity = new Vector2(Velocity.x, 0);
            isFlying = true;
        }
        FacingDirection(InputManager.Instance.MoveInput);
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
        if (isGrounded)
        {
            Velocity = new Vector2(Velocity.x, 0);
            return;
        }
        Velocity += new Vector2(0, -1f * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Simple ground collision check
        if (collision.CompareTag("Ground"))
        {
            Debug.Log("Player grounded");
            isGrounded = true;
            isFlying = false;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Ground"))
        {
            Debug.Log("Player left the ground");
            isGrounded = false;
        }
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
                VelX = Velocity.x,
                VelY = Velocity.y,
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
        else if (Velocity.x != 0)
        {
            return "run";
        }
        else
        {
            return "idle";
        }
    }
}