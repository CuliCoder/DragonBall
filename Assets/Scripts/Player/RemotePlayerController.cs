using UnityEngine;
// ============================================================
//  REMOTE PLAYER
//  Chứa snapshot buffer để interpolate vị trí mượt
//
//  Tại sao cần interpolate?
//    Server gửi 50Hz nhưng game render 60-144fps
//    → cần nội suy giữa 2 snapshot để không giật
// ============================================================
public class RemotePlayer : PlayerController
{
    public Transform Transform { get; private set; }

    // Lưu 2 snapshot gần nhất để lerp giữa chúng
    private PlayerState _from;
    private PlayerState _to;
    private float _lerpT = 1f; // 0 = from, 1 = to
    private const float LERP_SPEED = 15f;

    public void Initialize(int id, string playerName, Transform transform, Vector3 initialPosition)
    {
        base.Initialize(id, playerName);
        Transform = transform;
        Transform.position = initialPosition;
        // Khởi tạo snapshot rỗng
        _from = new PlayerState { PlayerId = id, X = initialPosition.x, Y = initialPosition.y };
        _to = new PlayerState { PlayerId = id, X = initialPosition.x, Y = initialPosition.y };
    }

    // Nhận snapshot mới từ server
    public void PushSnapshot(PlayerState newState)
    {
        _from = _to;          // snapshot cũ trở thành điểm xuất phát
        _to = newState;     // snapshot mới là đích đến
        _lerpT = 0f;           // reset lerp
    }

    // Gọi mỗi frame trong Update() để render mượt
    public void InterpolatePosition()
    {
        Debug.Log($"[RemotePlayer] New interpolated position for Player 0 {PlayerId}: ({Transform.position.x}, {Transform.position.y})");

        if (_lerpT >= 1f) return;

        _lerpT = Mathf.MoveTowards(_lerpT, 1f, Time.deltaTime * LERP_SPEED);

        Transform.position = Vector3.Lerp(
            new Vector3(_from.X, _from.Y, 0),
            new Vector3(_to.X, _to.Y, 0),
            _lerpT
        );
        Debug.Log($"[RemotePlayer] New interpolated position for Player 1 {PlayerId}: ({Transform.position.x}, {Transform.position.y})");
    }
    public override void Move()
    {
        InterpolatePosition();
    }
}