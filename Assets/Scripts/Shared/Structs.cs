using System.Numerics;
public struct RoomInfo
{
    public string RoomId { get; set; }
    public int PlayerCount { get; set; }
    public int MaxPlayer { get; set; }
}
public struct PlayerState
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float VelX { get; set; }
    public float VelY { get; set; }
    public string AnimState { get; set; }
    public Vector2 Position { get; set; }

}
public struct PlayerInfo
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; }
}