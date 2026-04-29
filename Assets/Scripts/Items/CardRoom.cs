using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
public class CardRoom : MonoBehaviour, Card
{
    public string RoomId { get; private set; }
    [SerializeField] private TextMeshProUGUI _roomIdText;
    [SerializeField] private TextMeshProUGUI _playerCountText;
    public void Initialize(string roomId, int playerCount, int maxPlayer)
    {
        RoomId = roomId;
        _roomIdText.text = RoomId;
        _playerCountText.text = $"{playerCount}/{maxPlayer}";

    }
    public async void HandleClick()
    {
        Debug.Log($"Joining room {RoomId}...");
        await NetworkManager.Instance.SendAsync(new C_JoinRoomPacket { RoomId = RoomId });
    }
}