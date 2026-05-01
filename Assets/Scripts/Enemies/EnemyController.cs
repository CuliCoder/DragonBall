using TMPro;
using UnityEngine;
public enum BossType
{
    None,
    Broly,
    Cell,
    Frieza
}
public enum EnemyActionState
{
    Idle,
    Move,
    Punch,
    Strike,
    Die
}
public class EnemyController : MonoBehaviour
{
    private TextMeshPro _playerIdText;
    public int EnemyId { get; private set; }
    public BossType BossType { get; set; }
    private EnemyActionState _currentState = EnemyActionState.Idle;
    private PlayerState _targetPlayer = null!;
    public BossType Type { get; set; }
    public Vector2 Velocity { get; set; }

    public int HpMax { get; set; }
    public int HpCurrent { get; set; }
    public bool IsDead { get; set; }

    public int Level { get; set; }
    public float Speed { get; set; }
    private EnemyStateManager _stateManager = null!;
    private bool isGrounded = false;

    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Transform wallCheckPoint;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private float wallCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    public void Initialize(int enemyId, BossType bossType)
    {
        EnemyId = enemyId;
        BossType = bossType;
        setName(bossType.ToString());
        _stateManager = GetComponent<EnemyStateManager>();
        _stateManager.Init(this, GetComponent<PlayerAnimationManager>());
    }
    private void setName(string name)
    {
        if (_playerIdText == null)
        {
            var nameTransform = transform.Find("name");
            if (nameTransform == null)
            {
                Debug.LogError("Missing child 'name' on Enemy prefab");
                return;
            }

            _playerIdText = nameTransform.GetComponent<TextMeshPro>();
            if (_playerIdText == null)
            {
                Debug.LogError("Child 'name' does not have TextMeshPro");
                return;
            }
        }

        _playerIdText.text = name;
    }
    public void FacingDirection(Vector2 direction)
    {
        if (direction.x > 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
            transform.Find("name").localScale = new Vector3(0.1f, 0.1f, 1);
        }
        else if (direction.x < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
            transform.Find("name").localScale = new Vector3(-0.1f, 0.1f, 1);
        }
    }
    public void UpdateState(S_BossStatePacket packet)
    {
        _currentState = packet.AnimState;
        if (_currentState == EnemyActionState.Die)
        {
            IsDead = true;
        }
        else if (_currentState == EnemyActionState.Move)
        {
            FacingDirection(new Vector2(packet.BossX - transform.position.x, packet.BossY - transform.position.y));
            Move(new Vector2(packet.BossX, packet.BossY));
            _stateManager.ChangeState(_stateManager.runState);
        }
        else if (_currentState == EnemyActionState.Idle)
        {
            _stateManager.ChangeState(_stateManager.idleState);
        }
    }
    public void Move(Vector2 newPosition)
    {
        float distance = Vector2.Distance(transform.position, newPosition);
        if (distance < 3f)
        {
        }
        else
        {
        }
        transform.position = newPosition;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }

    public bool HasGroundAhead()
    {
        Vector2 origin = groundCheckPoint != null ? (Vector2)groundCheckPoint.position : (Vector2)transform.position;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
        return hit.collider != null;
    }

    public bool HasWallAhead()
    {
        Vector2 origin = wallCheckPoint != null ? (Vector2)wallCheckPoint.position : (Vector2)transform.position;
        Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, wallCheckDistance, wallLayer);
        return hit.collider != null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector2 groundOrigin = groundCheckPoint != null ? (Vector2)groundCheckPoint.position : (Vector2)transform.position;
        Gizmos.DrawLine(groundOrigin, groundOrigin + Vector2.down * groundCheckDistance);

        Gizmos.color = Color.red;
        Vector2 wallOrigin = wallCheckPoint != null ? (Vector2)wallCheckPoint.position : (Vector2)transform.position;
        Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        Gizmos.DrawLine(wallOrigin, wallOrigin + direction * wallCheckDistance);
    }
}