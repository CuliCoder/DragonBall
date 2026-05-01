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

}