using UnityEngine;
public class PlayerController : MonoBehaviour
{
    // private TcpServerManager _tcpServerManager;
    // void Start()
    // {
    //     _tcpServerManager = TcpServerManager.Instance;
    // }
    // void Update()
    // {
    //     // Đọc Input ở Update cho mượt (nhưng chưa gửi vội)
    //     float h = Input.GetAxisRaw("Horizontal");
    //     float v = Input.GetAxisRaw("Vertical");


    //     // Chỉ gửi khi đủ thời gian (VD: 0.05s) và thực sự CÓ bấm nút di chuyển
    //     SendInputToServer(h, v); // Gọi hàm phụ trợ để gửi
    // }

    // // Tách riêng hàm gửi ra, không để async void ở Update
    // private async void SendInputToServer(float h, float v)
    // {
    //     if (_tcpServerManager == null || _tcpServerManager._cts == null) return;

    //     var data = PacketSerializer.Serialize(new C_PlayerInputPacket
    //     {
    //         inputDirection = new System.Numerics.Vector2(h, v)
    //     });

    //     await _tcpServerManager.SendPacketAsync(data, _tcpServerManager._cts.Token);
    // }
}