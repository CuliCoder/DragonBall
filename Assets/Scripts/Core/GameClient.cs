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
public class GameClient : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private string _serverIp   = "127.0.0.1";
    [SerializeField] private int    _serverPort  = 8888;
    [SerializeField] private string _roomId      = "room_01";

    [Header("Prefabs")]
    [SerializeField] private GameObject _remotePlayerPrefab;
    [SerializeField] private Transform  _localPlayer;

    // ========== LOCAL PLAYER INPUT ==========
    private Vector2 _inputDirection;
    private bool    _jumpPressed;
    private bool    _attackPressed;
    private int     _clientTick = 0;

    // ========== REMOTE PLAYERS ==========
    // sessionId → GameObject của remote player
    private readonly Dictionary<int, RemotePlayer> _remotePlayers = new();

    // ============================================================
    private async void Start()
    {
        // Subscribe events TRƯỚC khi connect
        SubscribeNetworkEvents();

        await NetworkManager.Instance.ConnectAsync(_serverIp, _serverPort);

        // Sau khi connect → join room
        await NetworkManager.Instance.SendAsync(new C_JoinRoomPacket { RoomId = _roomId });
    }

    // ============================================================
    //  SUBSCRIBE — đăng ký handler cho từng sự kiện mạng
    //  Tách riêng để dễ đọc và dễ unsubscribe
    // ============================================================
    private void SubscribeNetworkEvents()
    {
        var net = NetworkManager.Instance;

        net.OnJoinRoomAck  += HandleJoinRoomAck;
        net.OnWorldState   += HandleWorldState;   // ← quan trọng nhất
        net.OnPlayerJoined += HandlePlayerJoined;
        net.OnPlayerLeft   += HandlePlayerLeft;
        net.OnChat         += HandleChat;
        net.OnError        += HandleError;
        net.OnDisconnected += HandleDisconnected;
    }

    private void OnDestroy()
    {
        var net = NetworkManager.Instance;
        if (net == null) return;

        net.OnJoinRoomAck  -= HandleJoinRoomAck;
        net.OnWorldState   -= HandleWorldState;
        net.OnPlayerJoined -= HandlePlayerJoined;
        net.OnPlayerLeft   -= HandlePlayerLeft;
        net.OnChat         -= HandleChat;
        net.OnError        -= HandleError;
        net.OnDisconnected -= HandleDisconnected;
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
        _jumpPressed   = Input.GetButton("Jump");
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
        if (!NetworkManager.Instance.IsConnected) return;

        _clientTick++;

        // Gửi input lên server mỗi fixed tick
        _ = NetworkManager.Instance.SendAsync(new C_InputPacket
        {
            DirX   = _inputDirection.x,
            DirY   = _inputDirection.y,
            Jump   = _jumpPressed,
            Attack = _attackPressed,
            Tick   = _clientTick
        });

        // Reset button flags (chỉ gửi 1 lần per press)
        _jumpPressed   = false;
        _attackPressed = false;
    }

    // ============================================================
    //  PACKET HANDLERS
    //  Tất cả được gọi trong Update() (main thread) → an toàn
    // ============================================================
    private void HandleJoinRoomAck(S_JoinRoomAckPacket packet)
    {
        Debug.Log($"[GameClient] Đã vào phòng: {packet.RoomId} | PlayerId: {packet.PlayerId}");
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
            // Bỏ qua local player — vị trí do client prediction quyết định
            if (playerState.PlayerId == localId)
            {
                // TODO: reconcile nếu lệch quá ngưỡng
                // ReconcileLocalPlayer(playerState);
                continue;
            }

            // Cập nhật snapshot cho remote player để interpolate
            if (_remotePlayers.TryGetValue(playerState.PlayerId, out var remote))
            {
                remote.PushSnapshot(playerState);
            }
        }
    }

    private void HandlePlayerJoined(S_PlayerJoinedPacket packet)
    {
        int localId = NetworkManager.Instance.LocalPlayerId;
        if (packet.PlayerId == localId) return; // không spawn bản thân

        Debug.Log($"[GameClient] {packet.PlayerName} vào phòng");

        // Spawn remote player
        if (_remotePlayerPrefab != null && !_remotePlayers.ContainsKey(packet.PlayerId))
        {
            var go     = Instantiate(_remotePlayerPrefab);
            var remote = new RemotePlayer(packet.PlayerId, packet.PlayerName, go.transform);
            _remotePlayers[packet.PlayerId] = remote;
        }
    }

    private void HandlePlayerLeft(S_PlayerLeftPacket packet)
    {
        Debug.Log($"[GameClient] Player {packet.PlayerId} rời phòng");

        if (_remotePlayers.TryGetValue(packet.PlayerId, out var remote))
        {
            Destroy(remote.Transform.gameObject);
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

// ============================================================
//  REMOTE PLAYER
//  Chứa snapshot buffer để interpolate vị trí mượt
//
//  Tại sao cần interpolate?
//    Server gửi 50Hz nhưng game render 60-144fps
//    → cần nội suy giữa 2 snapshot để không giật
// ============================================================
public class RemotePlayer
{
    public int       PlayerId   { get; }
    public string    PlayerName { get; }
    public Transform Transform  { get; }

    // Lưu 2 snapshot gần nhất để lerp giữa chúng
    private PlayerStateData _from;
    private PlayerStateData _to;
    private float           _lerpT = 1f; // 0 = from, 1 = to
    private const float     LERP_SPEED = 15f;

    public RemotePlayer(int id, string name, Transform transform)
    {
        PlayerId   = id;
        PlayerName = name;
        Transform  = transform;

        // Khởi tạo snapshot rỗng
        _from = new PlayerStateData { PlayerId = id };
        _to   = new PlayerStateData { PlayerId = id };
    }

    // Nhận snapshot mới từ server
    public void PushSnapshot(PlayerStateData newState)
    {
        _from  = _to;          // snapshot cũ trở thành điểm xuất phát
        _to    = newState;     // snapshot mới là đích đến
        _lerpT = 0f;           // reset lerp
    }

    // Gọi mỗi frame trong Update() để render mượt
    public void InterpolatePosition()
    {
        if (_lerpT >= 1f) return;

        _lerpT = Mathf.MoveTowards(_lerpT, 1f, Time.deltaTime * LERP_SPEED);

        Transform.position = Vector3.Lerp(
            new Vector3(_from.X, _from.Y, 0),
            new Vector3(_to.X,   _to.Y,   0),
            _lerpT
        );
    }
}