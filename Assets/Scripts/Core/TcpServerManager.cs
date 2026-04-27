using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Text;
using System;
public class TcpServerManager : MonoBehaviour
{
    private TcpClient _client;
    private NetworkStream _stream;
    private CancellationTokenSource _cts;
    private ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>();
    public static TcpServerManager Instance { get; private set; }
    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private async void Start()
    {
        await ConnectToServer("127.0.0.1", 8888);
    }
    private async Task ConnectToServer(string ip, int port)
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(IPAddress.Parse(ip), port);
            _stream = _client.GetStream();
            _cts = new CancellationTokenSource();
            Debug.Log("Connected to server");
            _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
            await SendDataAsync("{\"action\":\"hello\", \"name\":\"Player1\"}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Network] Lỗi kết nối: {ex.Message}");
        }
    }
    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        try
        {
            byte[] lengthBuffer = new byte[4];
            while (!token.IsCancellationRequested && _client.Connected)
            {
                // Read message length
                int bytesRead = await _stream.ReadAsync(lengthBuffer, 0, 4, token);
                if (bytesRead == 0) break; // Connection closed

                int payloadLength = BitConverter.ToInt32(lengthBuffer, 0);

                // Bước 2: Chuẩn bị mảng byte để chứa nội dung JSON theo đúng độ dài vừa đọc
                byte[] payloadBuffer = new byte[payloadLength];
                int totalBytesRead = 0;

                // Phải dùng vòng lặp vì gói tin lớn có thể bị cắt làm nhiều mảnh trên đường truyền
                while (totalBytesRead < payloadLength)
                {
                    int read = await _stream.ReadAsync(payloadBuffer, totalBytesRead, payloadLength - totalBytesRead, token);
                    if (read == 0) throw new Exception("Mất kết nối khi đang đọc dữ liệu");
                    totalBytesRead += read;
                }

                // Bước 3: Dịch mảng byte thành chuỗi JSON và nhét vào hàng đợi
                string jsonMessage = Encoding.UTF8.GetString(payloadBuffer);
                _messageQueue.Enqueue(jsonMessage);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Network] Ngừng nhận dữ liệu: {ex.Message}");
        }
    }
    public async Task SendDataAsync(string jsonMessage)
    {
        if (_client == null || !_client.Connected) return;

        try
        {
            // Dịch chuỗi JSON ra mảng byte
            byte[] payloadBytes = Encoding.UTF8.GetBytes(jsonMessage);

            // Tạo 4 byte chứa độ dài của mảng byte trên
            byte[] lengthBytes = BitConverter.GetBytes(payloadBytes.Length);

            // Gộp 4 byte độ dài và byte nội dung lại với nhau
            byte[] fullPacket = new byte[4 + payloadBytes.Length];
            Buffer.BlockCopy(lengthBytes, 0, fullPacket, 0, 4);
            Buffer.BlockCopy(payloadBytes, 0, fullPacket, 4, payloadBytes.Length);

            // Gửi cái rụp lên Server
            await _stream.WriteAsync(fullPacket, 0, fullPacket.Length);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Network] Lỗi khi gửi: {ex.Message}");
        }
    }
    private void Update()
    {
        // Liên tục kiểm tra xem có thư mới trong hộp không
        while (_messageQueue.TryDequeue(out string incomingJson))
        {
            // ĐÂY LÀ NƠI BẠN XỬ LÝ GAME LOGIC CỦA UNITY
            Debug.Log($"[Network] Server nói: {incomingJson}");

            // Ví dụ: 
            // PlayerInfo info = JsonUtility.FromJson<PlayerInfo>(incomingJson);
            // playerObject.transform.position = new Vector3(info.x, info.y, info.z);
        }
    }

    // Dọn dẹp an toàn khi tắt game
    private void OnDestroy()
    {
        _cts?.Cancel();
        _stream?.Close();
        _client?.Close();
    }
}