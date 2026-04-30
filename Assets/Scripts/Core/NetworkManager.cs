using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

// ============================================================
//  NETWORK MANAGER
//  Chỉ làm đúng 1 việc: quản lý kết nối TCP và đọc/ghi bytes.
//
//  KHÔNG xử lý game logic ở đây.
//  Game logic nằm trong GameClient.cs
//
//  Thread model:
//    - Network Thread (Task.Run): đọc bytes liên tục, enqueue
//    - Main Thread (Update):      dequeue, deserialize, dispatch
// ============================================================
public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    // ========== CONNECTION STATE ==========
    public bool IsConnected => _client is { Connected: true } && _stream != null;
    public int LocalPlayerId { get; private set; } = -1;
    public string CurrentRoomId { get; private set; } = "";

    private TcpClient _client;
    private NetworkStream _stream;
    private CancellationTokenSource _cts;

    // SemaphoreSlim thay Lock — WriteAsync không thread-safe
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    // ========== QUEUE: NETWORK THREAD → MAIN THREAD ==========
    // Network Thread enqueue raw bytes
    // Update() dequeue, deserialize, dispatch
    private readonly ConcurrentQueue<byte[]> _receiveQueue = new();

    // ========== DISPATCHER ==========
    private readonly ClientPacketDispatcher _dispatcher = new();
    private const int MaxPacketsPerFrame = 1024;

    // ========== EVENTS cho GameClient đăng ký ==========
    public event Action<S_WorldStatePacket> OnWorldState;
    public event Action<S_PlayerLeftPacket> OnPlayerLeft;
    public event Action<S_ChatPacket> OnChat;
    public event Action<S_JoinRoomAckPacket> OnJoinRoomAck;
    public event Action<S_ErrorPacket> OnError;
    public event Action<S_ListRoomsPacket> OnListRooms;
    public event Action<S_JoinWorldPacket> OnJoinWorldAck;
    public event Action<S_TeleportPacket> OnTeleport;
    public event Action<S_BossStatePacket> OnBossState;
    public event Action<S_BossDefeatPacket> OnBossDefeated;
    public event Action OnDisconnected;

    // ============================================================
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        RegisterDispatcherHandlers();
    }

    private void RegisterDispatcherHandlers()
    {
        // Mỗi loại packet → forward ra event tương ứng
        // Game logic subscribe event ở GameClient.cs — không đụng vào đây
        _dispatcher.Register<S_WorldStatePacket>(PacketType.S_WorldState, p => OnWorldState?.Invoke(p));
        _dispatcher.Register<S_PlayerLeftPacket>(PacketType.S_PlayerLeft, p => OnPlayerLeft?.Invoke(p));
        _dispatcher.Register<S_ChatPacket>(PacketType.S_Chat, p => OnChat?.Invoke(p));
        _dispatcher.Register<S_ListRoomsPacket>(PacketType.S_ListRooms, p =>
        {
            LocalPlayerId = p.PlayerId;
            OnListRooms?.Invoke(p);
        });
        _dispatcher.Register<S_JoinRoomAckPacket>(PacketType.S_JoinRoomAck, p =>
        {
            CurrentRoomId = p.RoomId;
            OnJoinRoomAck?.Invoke(p);
        });
        _dispatcher.Register<S_JoinWorldPacket>(PacketType.S_JoinWorld, p => OnJoinWorldAck?.Invoke(p));
        _dispatcher.Register<S_ErrorPacket>(PacketType.S_Error, p =>
        {
            Debug.LogWarning($"[Server Error] {p.Message}");
            OnError?.Invoke(p);
        });
        _dispatcher.Register<S_TeleportPacket>(PacketType.S_Teleport, p => OnTeleport?.Invoke(p));
        _dispatcher.Register<S_BossStatePacket>(PacketType.S_BossState, p => OnBossState?.Invoke(p));
        _dispatcher.Register<S_BossDefeatPacket>(PacketType.S_BossDefeated, p => OnBossDefeated?.Invoke(p));
    }

    // ============================================================
    //  CONNECT
    // ============================================================
    public async Task ConnectAsync(string ip, int port)
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(IPAddress.Parse(ip), port);
            _stream = _client.GetStream();
            Debug.Log($"[Network] LocalPlayerId: {LocalPlayerId}");
            _cts = new CancellationTokenSource();

            Debug.Log($"[Network] Kết nối thành công: {ip}:{port}");

            // NETWORK THREAD — chạy riêng, không block main thread
            _ = Task.Run(() => ReceiveLoopAsync(_cts.Token), _cts.Token);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Network] Lỗi kết nối: {ex.Message}");
            CleanupConnection();
        }
    }

    // ============================================================
    //  NETWORK THREAD — đọc liên tục, enqueue vào queue
    //  KHÔNG deserialize, KHÔNG gọi Unity API ở đây
    // ============================================================
    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        Debug.Log("[Network] Network thread bắt đầu.");

        try
        {
            while (!token.IsCancellationRequested && _client.Connected)
            {
                byte[] rawData = await ReadRawPacketAsync(token);

                if (rawData == null)
                {
                    Debug.Log("[Network] Server ngắt kết nối.");
                    break;
                }

                // Chỉ enqueue — main thread sẽ xử lý
                _receiveQueue.Enqueue(rawData);
            }
        }
        catch (OperationCanceledException) { /* Bình thường khi disconnect */ }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Network] Network thread lỗi: {ex.Message}");
        }
        finally
        {
            // Báo main thread biết đã ngắt
            // Dùng queue đặc biệt thay vì gọi thẳng để tránh cross-thread
            _receiveQueue.Enqueue(Array.Empty<byte>()); // sentinel value
            Debug.Log("[Network] Network thread kết thúc.");
        }
    }

    // ============================================================
    //  UPDATE — MAIN THREAD
    //  Dequeue → deserialize → dispatch
    //  Tất cả Unity API được gọi ở đây (an toàn)
    // ============================================================
    private void Update()
    {
        int processed = 0;
        S_WorldStatePacket latestWorldState = null;

        while (processed < MaxPacketsPerFrame && _receiveQueue.TryDequeue(out byte[] rawData))
        {
            processed++;

            // Sentinel: empty array = đã disconnect
            if (rawData.Length == 0)
            {
                CleanupConnection();
                OnDisconnected?.Invoke();
                break;
            }

            BasePacket packet = PacketSerializer.Deserialize(rawData);
            if (packet == null)
            {
                Debug.LogWarning("[Network] Không thể deserialize packet.");
                continue;
            }

            // Ưu tiên độ trễ thấp: chỉ giữ world-state mới nhất trong frame hiện tại.
            if (packet is S_WorldStatePacket worldState)
            {
                latestWorldState = worldState;
                continue;
            }

            // Dispatch tới đúng handler theo PacketType
            _dispatcher.Dispatch(packet);
        }

        // Nếu frame trước bị nghẽn queue, chủ động cắt bớt world-state cũ còn tồn đọng.
        DrainStaleWorldStates(ref latestWorldState);
        if (latestWorldState != null)
            _dispatcher.Dispatch(latestWorldState);
    }

    private void DrainStaleWorldStates(ref S_WorldStatePacket latestWorldState)
    {
        const int MaxDrainPerFrame = 2048;
        int drained = 0;

        while (drained < MaxDrainPerFrame && _receiveQueue.TryPeek(out byte[] rawData))
        {
            // Sentinel disconnect phải được giữ lại cho vòng Update kế tiếp xử lý đúng flow.
            if (rawData.Length == 0)
                break;

            if (rawData.Length < 4)
            {
                _receiveQueue.TryDequeue(out _);
                drained++;
                continue;
            }

            var packetType = (PacketType)BitConverter.ToUInt16(rawData, 2);
            if (packetType != PacketType.S_WorldState)
                break;

            _receiveQueue.TryDequeue(out _);
            drained++;

            var packet = PacketSerializer.Deserialize(rawData) as S_WorldStatePacket;
            if (packet != null)
                latestWorldState = packet;
        }
    }

    // ============================================================
    //  GỬI PACKET — thread-safe
    //  Có thể gọi từ bất kỳ thread nào
    // ============================================================
    public async Task SendAsync<T>(T packet) where T : BasePacket
    {
        if (!IsConnected) return;

        byte[] data = PacketSerializer.Serialize(packet);
        await SendRawAsync(data);
    }

    public async Task SendRawAsync(byte[] data)
    {
        if (!IsConnected || _stream == null || _sendLock == null) return;

        try
        {
            await _sendLock.WaitAsync();
        }
        catch
        {
            // _sendLock bị dispose hoặc null
            return;
        }

        try
        {
            if (_stream == null) return;
            await _stream.WriteAsync(data);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Network] Lỗi gửi: {ex.GetType().Name} - {ex.Message}");
            CleanupConnection();
        }
        finally
        {
            if (_sendLock != null)
            {
                try
                {
                    _sendLock.Release();
                }
                catch { /* _sendLock đã bị dispose */ }
            }
        }
    }

    // ============================================================
    //  HELPER — đọc đúng 1 packet từ stream
    //  Protocol: [2 bytes totalLen][payload...]
    // ============================================================
    private async Task<byte[]> ReadRawPacketAsync(CancellationToken token)
    {
        byte[] lenBuf = new byte[2];
        if (!await ReadExactAsync(lenBuf, 2, token)) return null;

        ushort totalLen = BitConverter.ToUInt16(lenBuf, 0);
        if (totalLen < 4) return null;

        byte[] payload = new byte[totalLen - 2];
        if (!await ReadExactAsync(payload, payload.Length, token)) return null;

        byte[] full = new byte[totalLen];
        Buffer.BlockCopy(lenBuf, 0, full, 0, 2);
        Buffer.BlockCopy(payload, 0, full, 2, payload.Length);
        return full;
    }

    private async Task<bool> ReadExactAsync(byte[] buf, int count, CancellationToken token)
    {
        if (_stream == null) return false;
        
        int total = 0;
        while (total < count)
        {
            try
            {
                int n = await _stream.ReadAsync(buf, total, count - total, token);
                if (n == 0) return false;
                total += n;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }
        return true;
    }

    private void CleanupConnection()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _stream?.Close();
        _stream = null;
        _client?.Close();
        _client = null;
        LocalPlayerId = -1;
        CurrentRoomId = "";
    }

    private void OnDestroy()
    {
        CleanupConnection();
    }

    private void OnApplicationQuit()
    {
        CleanupConnection();
    }
}