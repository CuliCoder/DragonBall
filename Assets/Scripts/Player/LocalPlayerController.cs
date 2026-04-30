using UnityEngine;
using Cinemachine;
public class LocalPlayerController : PlayerController
{
    public const float SPEED = 5f;
    public Vector2 Velocity = Vector2.zero;
    private bool isGrounded = false;
    private float timer = 0f;
    private bool isFlying = true;
    public bool isBoom = false;
    private void Awake()
    {
        FindAnyObjectByType<CinemachineVirtualCamera>().Follow = transform;
    }
    private void FixedUpdate()
    {
        ApplyGravity();
        transform.position += (Vector3)Velocity;
        verifyPositionWithServer();
        timer += Time.fixedDeltaTime;
        if (isFlying && timer > 1f)
        {
            isFlying = false;
        }
    }
    private void Update()
    {
        useBoom();
        if(isBoom)
        {
            return;
        }
        Move();
    }
    public override void Move()
    {
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
        Debug.Log($"Applying gravity. New velocity: {Velocity}");
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
        if (!NetworkManager.Instance.IsConnected)
        {
            Debug.LogWarning("[LocalPlayerController] Not connected to server, cannot send position");
            return;
        }

        var currentPos = transform.position;
        var targetPos = currentPos + (Vector3)Velocity;

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

        Debug.Log($"[LocalPlayerController] Sending position: current ({currentPos.x:F2}, {currentPos.y:F2}), target ({targetPos.x:F2}, {targetPos.y:F2})");

        // Log JSON payload để debug
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(packet);
        Debug.Log($"[LocalPlayerController] JSON payload: {json}");

        _ = NetworkManager.Instance.SendAsync(packet);
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