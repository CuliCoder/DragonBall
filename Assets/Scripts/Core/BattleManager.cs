using UnityEngine;
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }
    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    private void Start()
    {
        init();
    }
    private async void init()
    {
        await NetworkManager.Instance.SendAsync(new C_JoinWorldPacket { PlayerId = NetworkManager.Instance.LocalPlayerId, RoomId = NetworkManager.Instance.CurrentRoomId });
    }
}