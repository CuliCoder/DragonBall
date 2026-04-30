// ============================================================
//  GAME CLIENT
//  Xử lý toàn bộ game logic liên quan đến network:
//    - Subscribe events từ NetworkManager
//    - Gửi input mỗi FixedUpdate (sync với physics)
//    - Apply server state lên remote players
//    - Interpolate remote players để render mượt
//
//  KHÔNG đụng vào socket, byte, hay thread ở đây.
// ============================================================
using UnityEngine;
using System.Collections.Generic;
using TMPro;
public class GameClient : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private string _serverIp = "127.0.0.1";
    [SerializeField] private int _serverPort = 8888;

    [Header("Prefabs")]
    [SerializeField] private GameObject PlayerPrefab;
    [SerializeField] private SkinData defaultSkin;
    [SerializeField] private CardRoom _cardRoomPrefab;
    [SerializeField] private Transform _cardParent;
    [SerializeField] private CardPlayer _cardPlayerPrefab;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private GameObject btnPlay;
    [SerializeField] private GameObject EnemyPrefab;
    [SerializeField] private SkinData brolySkin;
    // ========== LOCAL PLAYER INPUT ==========
    private Vector2 _inputDirection;
    private bool _jumpPressed;
    private bool _attackPressed;
    private int _clientTick = 0;

    // ========== REMOTE PLAYERS ==========
    // sessionId → GameObject của remote player
    private readonly Dictionary<int, RemotePlayer> _remotePlayers = new();
    private readonly Dictionary<int, EnemyController> _enemies = new();
    private LocalPlayerController _localPlayer;

    // ============================================================
    private async void Start()
    {
        DontDestroyOnLoad(gameObject); // giữ GameClient qua các scene
        // Subscribe events TRƯỚC khi connect
        SubscribeNetworkEvents();

        await NetworkManager.Instance.ConnectAsync(_serverIp, _serverPort);

        // Sau khi connect → join room
        await NetworkManager.Instance.SendAsync(new C_GetRoomsPacket()); // Lấy danh sách phòng trước
    }

    // ============================================================
    //  SUBSCRIBE — đăng ký handler cho từng sự kiện mạng
    //  Tách riêng để dễ đọc và dễ unsubscribe
    // ============================================================
    private void SubscribeNetworkEvents()
    {
        var net = NetworkManager.Instance;

        net.OnJoinRoomAck += HandleJoinRoomAck;
        net.OnWorldState += HandleWorldState;   // ← quan trọng nhất
        net.OnPlayerLeft += HandlePlayerLeft;
        net.OnChat += HandleChat;
        net.OnError += HandleError;
        net.OnDisconnected += HandleDisconnected;
        net.OnListRooms += HandleListRooms;
        net.OnJoinWorldAck += HandleJoinWorldAck;
        net.OnTeleport += HandleTeleport;
        net.OnBossState += HandleBossState;
        net.OnBossDefeated += HandleBossDefeated;
    }

    private void OnDestroy()
    {
        var net = NetworkManager.Instance;
        if (net == null) return;

        net.OnJoinRoomAck -= HandleJoinRoomAck;
        net.OnWorldState -= HandleWorldState;
        net.OnPlayerLeft -= HandlePlayerLeft;
        net.OnChat -= HandleChat;
        net.OnError -= HandleError;
        net.OnDisconnected -= HandleDisconnected;
        net.OnListRooms -= HandleListRooms;
        net.OnJoinWorldAck -= HandleJoinWorldAck;
        net.OnTeleport -= HandleTeleport;
        net.OnBossState -= HandleBossState;
        net.OnBossDefeated -= HandleBossDefeated;
    }

    // ============================================================
    //  UPDATE — đọc input, xử lý UI
    // ============================================================
    private void Update()
    {
        // Đọc input từ người dùng (lưu lại để gửi trong FixedUpdate)
        _inputDirection = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );
        _jumpPressed = Input.GetButton("Jump");
        _attackPressed = Input.GetMouseButton(0);

        // Interpolate remote players mỗi frame (mượt hơn)
        foreach (var remote in _remotePlayers.Values)
            remote.InterpolatePosition();
    }

    // ============================================================
    //  FIXED UPDATE — gửi input lên server
    //
    //  Tại sao FixedUpdate, không phải Update?
    //    - FixedUpdate chạy cố định (50Hz mặc định)
    //    - Khớp với server tick rate → input rate ổn định
    //    - Tránh gửi quá nhiều khi FPS cao (Update chạy 144Hz trên máy mạnh)
    // ============================================================
    private void FixedUpdate()
    {
        
    }
    private void HandleListRooms(S_ListRoomsPacket packet)
    {
        titleText.text = $"Danh sách phòng";
        foreach (Transform child in _cardParent)
        {
            if (child != null)
                Destroy(child.gameObject);
        }
        Debug.Log($"[GameClient] Danh sách phòng ({packet.Rooms.Count}):");
        foreach (var room in packet.Rooms)
        {
            Debug.Log($"- {room.RoomId} | Players: {room.PlayerCount}/{room.MaxPlayer}");
            // Hiển thị card phòng trên UI
            if (_cardRoomPrefab != null)
            {
                var card = Instantiate(_cardRoomPrefab, Vector3.zero, Quaternion.identity, _cardParent);
                card.Initialize(room.RoomId, room.PlayerCount, room.MaxPlayer);
                Debug.Log($"[GameClient] Hiển thị card cho phòng {room.RoomId}, có {room.PlayerCount} người chơi.");
            }
        }
        btnPlay.SetActive(false);
    }
    private void HandleTeleport(S_TeleportPacket packet)
    {
        if (NetworkManager.Instance.LocalPlayerId != packet.SessionId) return;

        if (_localPlayer != null)
        {
            _localPlayer.transform.position = new Vector3(packet.TargetPosition.X, packet.TargetPosition.Y, 0);
            Debug.Log($"[GameClient] Teleported to {packet.TargetPosition}");
        }
    }
    private void HandleBossState(S_BossStatePacket packet)
    {
        if (_enemies.TryGetValue(packet.BossId, out var enemy))
        {
            enemy.Move(new Vector2(packet.BossX, packet.BossY));
            // enemy.FacingDirection(new Vector2(packet.VelX, 0));
        }
        else
        {
            Debug.Log($"[GameClient] Spawn boss {packet.BossId} at ({packet.BossX}, {packet.BossY})");
            var go = Instantiate(EnemyPrefab, new Vector3(packet.BossX, packet.BossY, 0), Quaternion.identity);
            go.GetComponent<PlayerAnimationManager>().SetSkin(brolySkin);
            EnemyController boss = go.AddComponent<EnemyController>();
            boss.Initialize(packet.BossId, packet.BossType); // TODO: lấy type từ server
            _enemies[packet.BossId] = boss;
        }
    }
    private void HandleBossDefeated(S_BossDefeatPacket packet)
    {
        
    }
    // ============================================================
    //  PACKET HANDLERS
    //  Tất cả được gọi trong Update() (main thread) → an toàn
    // ============================================================
    private void HandleJoinRoomAck(S_JoinRoomAckPacket packet)
    {
        titleText.text = $"Danh sách người chơi";
        foreach (Transform child in _cardParent)
        {
            if (child != null)
                Destroy(child.gameObject);
        }
        foreach (var p in packet.CurrentPlayers)
        {
            Debug.Log($"- Player {p.PlayerId}");
            if (_cardPlayerPrefab != null)
            {
                var card = Instantiate(_cardPlayerPrefab, Vector3.zero, Quaternion.identity, _cardParent);
                card.Initialize(p.PlayerName, "test");
                Debug.Log($"[GameClient] Hiển thị card cho player {p.PlayerName}");
            }
        }
        btnPlay.SetActive(true);
    }

    // ============================================================
    //  WORLD STATE — nhận mỗi server tick (50Hz)
    //  Apply lên remote players, bỏ qua local player
    //  (local player dùng client-side prediction)
    // ============================================================
    private void HandleWorldState(S_WorldStatePacket packet)
    {
        int localId = NetworkManager.Instance.LocalPlayerId;

        foreach (var playerState in packet.Players)
        {
            if (playerState.PlayerId == localId)
            {
                // Reconcile: nếu vị trí client lệch quá xa so với server → snap về
                if (_localPlayer != null)
                {
                    var serverPos = new Vector3(playerState.X, playerState.Y, 0);
                    float drift = Vector3.Distance(_localPlayer.transform.position, serverPos);
                    if (drift > 6f)
                    {
                        _localPlayer.transform.position = serverPos;
                    }
                    else if (drift > 1f)
                    {
                        _localPlayer.transform.position = Vector3.Lerp(
                            _localPlayer.transform.position,
                            serverPos,
                            0.08f
                        );
                    }
                }
                continue;
            }

            // Cập nhật snapshot cho remote player để interpolate
            if (_remotePlayers.TryGetValue(playerState.PlayerId, out var remote))
            {
                remote.PushSnapshot(playerState);
            }
        }
    }

    private void HandleJoinWorldAck(S_JoinWorldPacket packet)
    {
        foreach (PlayerState playerState in packet.CurrentPlayers)
        {
            Debug.Log($"[GameClient] Player in world: {playerState.PlayerName} (ID: {playerState.PlayerId})");
            if (NetworkManager.Instance.LocalPlayerId == playerState.PlayerId)
            {
                if (_localPlayer == null)
                {
                    var go = Instantiate(PlayerPrefab, new Vector3(playerState.X, playerState.Y, 0), Quaternion.identity);
                    go.GetComponent<PlayerAnimationManager>().SetSkin(defaultSkin);
                    _localPlayer = go.AddComponent<LocalPlayerController>();
                    _localPlayer.Initialize(playerState.PlayerId, playerState.PlayerName);
                }
                Debug.Log($"[GameClient] Local player instantiated with ID {playerState.PlayerName}");
                continue;
            }

            Debug.Log($"[GameClient] {playerState.PlayerName} vào phòng, ID: {NetworkManager.Instance.LocalPlayerId}");

            // Spawn remote player
            if (PlayerPrefab != null && !_remotePlayers.ContainsKey(playerState.PlayerId))
            {
                var go = Instantiate(PlayerPrefab, new Vector3(playerState.X, playerState.Y, 0), Quaternion.identity);
                go.GetComponent<PlayerAnimationManager>().SetSkin(defaultSkin);
                RemotePlayer remotePlayer = go.AddComponent<RemotePlayer>();
                remotePlayer.Initialize(playerState.PlayerId, playerState.PlayerName, go.transform.position);
                _remotePlayers[playerState.PlayerId] = remotePlayer;
            }
        }
    }

    private void HandlePlayerLeft(S_PlayerLeftPacket packet)
    {
        Debug.Log($"[GameClient] Player {packet.PlayerId} rời phòng");

        if (_remotePlayers.TryGetValue(packet.PlayerId, out var remote))
        {
            Destroy(remote.gameObject);
            _remotePlayers.Remove(packet.PlayerId);
        }
    }

    private void HandleChat(S_ChatPacket packet)
    {
        Debug.Log($"[Chat] {packet.SenderName}: {packet.Message}");
        // TODO: hiện lên UI chat
    }

    private void HandleError(S_ErrorPacket packet)
    {
        Debug.LogWarning($"[GameClient] Server error: {packet.Message}");
    }

    private void HandleDisconnected()
    {
        Debug.Log("[GameClient] Mất kết nối với server.");
        // TODO: hiện UI reconnect
    }

    // ============================================================
    //  GỬI CHAT (gọi từ UI)
    // ============================================================
    public void SendChat(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        _ = NetworkManager.Instance.SendAsync(new C_ChatPacket { Message = message });
    }
}