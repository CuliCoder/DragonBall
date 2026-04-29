using UnityEngine;
using TMPro;
public class CardPlayer : MonoBehaviour, Card
{
    public string PlayerId { get; private set; }
    [SerializeField] private TextMeshProUGUI _playerIdText;
    [SerializeField] private TextMeshProUGUI _playerClassText;
    public void Initialize(string playerId, string classPlayer)
    {
        PlayerId = playerId;
        _playerIdText.text = PlayerId;
        _playerClassText.text = classPlayer;

    }
    public void HandleClick()
    {
        // Logic khi click vào card player (ví dụ: xem thông tin chi tiết, gửi lời mời kết bạn, v.v.)
    }
}