using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ============================================================
//  PACKET TYPES — phải khớp hoàn toàn với server
// ============================================================
public enum PacketType : ushort
{
    // Client → Server
    C_Input         = 1,
    C_JoinRoom      = 2,
    C_LeaveRoom     = 3,
    C_Chat          = 4,

    // Server → Client
    S_WorldState    = 100,
    S_PlayerJoined  = 101,
    S_PlayerLeft    = 102,
    S_Chat          = 103,
    S_JoinRoomAck   = 104,
    S_Error         = 105,
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
public class C_InputPacket : BasePacket
{
    public override PacketType Type => PacketType.C_Input;
    public float DirX   { get; set; }
    public float DirY   { get; set; }
    public bool  Jump   { get; set; }
    public bool  Attack { get; set; }
    public int   Tick   { get; set; }
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

// ============================================================
//  SERVER → CLIENT
// ============================================================
public class S_WorldStatePacket : BasePacket
{
    public override PacketType Type => PacketType.S_WorldState;
    public int ServerTick             { get; set; }
    public List<PlayerStateData> Players { get; set; } = new();
}

public class PlayerStateData
{
    public int    PlayerId  { get; set; }
    public float  X         { get; set; }
    public float  Y         { get; set; }
    public float  VelX      { get; set; }
    public float  VelY      { get; set; }
    public string AnimState { get; set; } = "";
}

public class S_PlayerJoinedPacket : BasePacket
{
    public override PacketType Type => PacketType.S_PlayerJoined;
    public int    PlayerId   { get; set; }
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
    public int    SenderId   { get; set; }
    public string SenderName { get; set; } = "";
    public string Message    { get; set; } = "";
}

public class S_JoinRoomAckPacket : BasePacket
{
    public override PacketType Type => PacketType.S_JoinRoomAck;
    public string RoomId   { get; set; } = "";
    public int    PlayerId { get; set; }
}

public class S_ErrorPacket : BasePacket
{
    public override PacketType Type => PacketType.S_Error;
    public string Message { get; set; } = "";
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
        ushort totalLen  = (ushort)(2 + 2 + jsonBytes.Length);

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
            PacketType.S_WorldState   => JsonConvert.DeserializeObject<S_WorldStatePacket>(json),
            PacketType.S_PlayerJoined => JsonConvert.DeserializeObject<S_PlayerJoinedPacket>(json),
            PacketType.S_PlayerLeft   => JsonConvert.DeserializeObject<S_PlayerLeftPacket>(json),
            PacketType.S_Chat         => JsonConvert.DeserializeObject<S_ChatPacket>(json),
            PacketType.S_JoinRoomAck  => JsonConvert.DeserializeObject<S_JoinRoomAckPacket>(json),
            PacketType.S_Error        => JsonConvert.DeserializeObject<S_ErrorPacket>(json),
            _                         => null
        };
    }
}