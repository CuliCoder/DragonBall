using TMPro;
using UnityEngine;
public abstract class PlayerController : MonoBehaviour
{
    private TextMeshPro _playerIdText;
    public int PlayerId { get; private set; }
    public string PlayerName { get; private set; }
    [SerializeField] private LayerMask groundLayer;
    public Rigidbody2D Rb { get; private set; }
    public void Initialize(int playerId, string playerName)
    {
        PlayerId = playerId;
        PlayerName = playerName;
        setName(PlayerName);
        GetComponent<PlayerStateManager>().Init(this, GetComponent<PlayerAnimationManager>());
        Rb = GetComponent<Rigidbody2D>();

    }
    private void setName(string name)
    {
        if (_playerIdText == null)
        {
            var nameTransform = transform.Find("name");
            if (nameTransform == null)
            {
                Debug.LogError("Missing child 'name' on Player prefab");
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
    public abstract void Move();

}