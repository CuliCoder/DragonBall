using TMPro;
using UnityEngine;
public enum BossType
{
    None,
    Broly,
    Cell,
    Frieza
}
public class EnemyController : MonoBehaviour
{
    private TextMeshPro _playerIdText;
    public int EnemyId { get; private set; }
    public BossType BossType { get; set; }
    public void Initialize(int enemyId, BossType bossType)
    {
        EnemyId = enemyId;
        BossType = bossType;
        setName(bossType.ToString());
        GetComponent<EnemyStateManager>().Init(this, GetComponent<PlayerAnimationManager>());
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

    public void Move(Vector2 newPosition)
    {
        transform.position = newPosition;
    }

}