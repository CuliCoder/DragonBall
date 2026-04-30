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
        _from = _to;   // snapshot cũ → điểm xuất phát lerp
        _to = newState; // snapshot mới → đích đến
        _lerpT = 0f;   // reset lerp

        // Cập nhật hướng mặt dựa vào velocity từ server
        if (Mathf.Abs(newState.VelX) > 0.01f)
            FacingDirection(new Vector2(newState.VelX, 0));

        // Cập nhật animation dựa vào AnimState từ server
        var stateManager = GetComponent<PlayerStateManager>();
        if (stateManager != null)
        {
            if (newState.AnimState == "run" && stateManager.currentState is not RunState)
                stateManager.ChangeState(stateManager.runState);
            else if (newState.AnimState == "idle" && stateManager.currentState is not IdleState)
                stateManager.ChangeState(stateManager.idleState);
        }
    }

    // Gọi mỗi frame trong Update() để render mượt
    public void InterpolatePosition()
    {
        if (_lerpT >= 1f) return;

        _lerpT = Mathf.MoveTowards(_lerpT, 1f, Time.deltaTime * LERP_SPEED);

        Transform.position = Vector3.Lerp(
            new Vector3(_from.X, _from.Y, 0),
            new Vector3(_to.X, _to.Y, 0),
            _lerpT
        );
    }
    public override void Move()
    {
        InterpolatePosition();
    }
}