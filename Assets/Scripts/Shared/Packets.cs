using System;
using System.Collections.Generic;
using System.Numerics;
using Newtonsoft.Json;

// ============================================================
//  PACKET TYPES — phải khớp hoàn toàn với server
// ============================================================
public enum PacketType : ushort
{
    // --- Client → Server ---
    C_Input = 1,   // Di chuyển, nhảy, tấn công...
    C_JoinRoom = 2,   // Yêu cầu vào phòng
    C_LeaveRoom = 3,   // Rời phòng
    C_Chat = 4,   // Tin nhắn chat
    C_GetRooms = 5,
    C_JoinWorld = 6,
    C_AttackBoss = 7,      // Client tấn công Boss
    // --- Server → Client ---
    S_WorldState = 100, // Broadcast vị trí TẤT CẢ player trong room (mỗi tick)
    S_PlayerJoined = 101, // Thông báo có người vào phòng
    S_PlayerLeft = 102, // Thông báo có người rời phòng
    S_Chat = 103, // Broadcast chat
    S_JoinRoomAck = 104, // Xác nhận vào phòng thành công
    S_Error = 105, // Thông báo lỗi
    S_ListRooms = 106, // Danh sách phòng hiện có (dùng cho lobby)
    S_JoinWorld = 107, // Xác nhận vào world thành công
    S_Teleport = 108, // Teleport player
    S_BossState = 200,     // Server broadcast trạng thái Boss
    S_BossDefeated = 201,  // Boss đã hạ gục
    S_BossHpUpdate = 202,  // Cập nhật HP Boss
}

// ============================================================
//  BASE PACKET
// ============================================================
public abstract class BasePacket
{
    [JsonIgnore]
    public abstract PacketType Type { get; }
}

// ============================================================
//  CLIENT → SERVER
// ============================================================
public class C_AttackBossPacket : BasePacket
{
    public override PacketType Type => PacketType.C_AttackBoss;
    public int BossId { get; set; }       // ID của boss đang tấn công
    public int SkillId { get; set; }        // Loại skill
    public float AttackX { get; set; }      // Vị trí tấn công
    public float AttackY { get; set; }
}
public class C_InputPacket : BasePacket
{
    public override PacketType Type => PacketType.C_Input;
    public float DirX { get; set; }
    public float DirY { get; set; }
    public bool Fly { get; set; }
    public bool Attack { get; set; }
    public int Tick { get; set; }
    public float DeltaTime { get; set; }
    public bool isNumber1 { get; set; }
    public PlayerState PlayerState { get; set; } = new();
}

public class C_JoinRoomPacket : BasePacket
{
    public override PacketType Type => PacketType.C_JoinRoom;
    public string RoomId { get; set; } = "default";
}

public class C_LeaveRoomPacket : BasePacket
{
    public override PacketType Type => PacketType.C_LeaveRoom;
}

public class C_ChatPacket : BasePacket
{
    public override PacketType Type => PacketType.C_Chat;
    public string Message { get; set; } = "";
}
public class C_GetRoomsPacket : BasePacket
{
    public override PacketType Type => PacketType.C_GetRooms;
}
public class C_JoinWorldPacket : BasePacket
{
    public override PacketType Type => PacketType.C_JoinWorld;
    public string RoomId { get; set; } = "";
    public int PlayerId { get; set; }
}
// ============================================================
//  SERVER → CLIENT
// ============================================================
public class S_WorldStatePacket : BasePacket
{
    public override PacketType Type => PacketType.S_WorldState;
    public int ServerTick { get; set; }
    public List<PlayerState> Players { get; set; } = new();
}

public class S_PlayerJoinedPacket : BasePacket
{
    public override PacketType Type => PacketType.S_PlayerJoined;
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = "";
}

public class S_PlayerLeftPacket : BasePacket
{
    public override PacketType Type => PacketType.S_PlayerLeft;
    public int PlayerId { get; set; }
}

public class S_ChatPacket : BasePacket
{
    public override PacketType Type => PacketType.S_Chat;
    public int SenderId { get; set; }
    public string SenderName { get; set; } = "";
    public string Message { get; set; } = "";
}

public class S_JoinRoomAckPacket : BasePacket
{
    public override PacketType Type => PacketType.S_JoinRoomAck;
    public string RoomId { get; set; } = "";
    public List<PlayerInfo> CurrentPlayers { get; set; } = new();
}
public class S_JoinWorldPacket : BasePacket
{
    public override PacketType Type => PacketType.S_JoinWorld;
    public List<PlayerState> CurrentPlayers { get; set; } = new();
}
public class S_ErrorPacket : BasePacket
{
    public override PacketType Type => PacketType.S_Error;
    public string Message { get; set; } = "";
}
public class S_ListRoomsPacket : BasePacket
{
    public override PacketType Type => PacketType.S_ListRooms;
    public int PlayerId { get; set; }
    public List<RoomInfo> Rooms { get; set; } = new();
}
public class S_TeleportPacket : BasePacket
{
    public override PacketType Type => PacketType.S_Teleport;
    public int SessionId { get; set; }
    public Vector2 TargetPosition { get; set; }
}
public class S_BossStatePacket : BasePacket
{
    public override PacketType Type => PacketType.S_BossState;
    public BossType BossType { get; set; }
    public int BossId { get; set; }       // ID của boss đang tấn công
    public float BossX { get; set; }
    public float BossY { get; set; }
    public int HpCurrent { get; set; }
    public int HpMax { get; set; }
    public string AnimState { get; set; }
}

public class S_BossDefeatPacket : BasePacket
{
    public override PacketType Type => PacketType.S_BossDefeated;
    public int BossId { get; set; }       // ID của boss đang tấn công
    public int LastHitPlayerId { get; set; }
    public long ClearTimeMs { get; set; }
    public int TotalExpReward { get; set; }
    public int TotalGoldReward { get; set; }
}
// ============================================================
//  PACKET SERIALIZER
//  Protocol: [2 bytes totalLen][2 bytes PacketType][JSON bytes]
// ============================================================
public static class PacketSerializer
{
    public static byte[] Serialize<T>(T packet) where T : BasePacket
    {
        byte[] typeBytes = BitConverter.GetBytes((ushort)packet.Type);
        string json = JsonConvert.SerializeObject(packet);
        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
        ushort totalLen = (ushort)(2 + 2 + jsonBytes.Length);

        byte[] result = new byte[totalLen];
        BitConverter.GetBytes(totalLen).CopyTo(result, 0);
        typeBytes.CopyTo(result, 2);
        jsonBytes.CopyTo(result, 4);
        return result;
    }

    // Deserialize raw bytes → đúng loại packet
    public static BasePacket Deserialize(byte[] data)
    {
        if (data.Length < 4) return null;

        var type = (PacketType)BitConverter.ToUInt16(data, 2);
        byte[] jsonBytes = new byte[data.Length - 4];
        Array.Copy(data, 4, jsonBytes, 0, jsonBytes.Length);
        string json = System.Text.Encoding.UTF8.GetString(jsonBytes);

        return type switch
        {
            PacketType.S_WorldState => JsonConvert.DeserializeObject<S_WorldStatePacket>(json),
            PacketType.S_PlayerJoined => JsonConvert.DeserializeObject<S_PlayerJoinedPacket>(json),
            PacketType.S_PlayerLeft => JsonConvert.DeserializeObject<S_PlayerLeftPacket>(json),
            PacketType.S_Chat => JsonConvert.DeserializeObject<S_ChatPacket>(json),
            PacketType.S_JoinRoomAck => JsonConvert.DeserializeObject<S_JoinRoomAckPacket>(json),
            PacketType.S_Error => JsonConvert.DeserializeObject<S_ErrorPacket>(json),
            PacketType.S_ListRooms => JsonConvert.DeserializeObject<S_ListRoomsPacket>(json),
            PacketType.S_JoinWorld => JsonConvert.DeserializeObject<S_JoinWorldPacket>(json),
            PacketType.S_Teleport => JsonConvert.DeserializeObject<S_TeleportPacket>(json),
            PacketType.S_BossState => JsonConvert.DeserializeObject<S_BossStatePacket>(json),
            PacketType.S_BossDefeated => JsonConvert.DeserializeObject<S_BossDefeatPacket>(json),
            _ => null
        };
    }
}
