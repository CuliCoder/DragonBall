// ============================================================
//  PACKET DISPATCHER — client side
//
//  Cách dùng:
//    dispatcher.Register<S_WorldStatePacket>(PacketType.S_WorldState, OnWorldState);
//    dispatcher.Dispatch(packet);  // gọi trong Update()
//
//  Lợi ích:
//    - Thêm packet mới chỉ thêm 1 dòng Register, không đụng loop
//    - Handler nằm đúng chỗ, không bị lẫn vào nhau
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;
public class ClientPacketDispatcher
{
    private readonly Dictionary<PacketType, Action<BasePacket>> _handlers = new();

    public void Register<T>(PacketType type, Action<T> handler) where T : BasePacket
    {
        _handlers[type] = packet =>
        {
            if (packet is T typed)
                handler(typed); 
            else
                Debug.LogWarning($"[Dispatcher] Type mismatch cho {type}");
        };
    }

    // Gọi trong Update() — chạy trên main thread Unity
    public void Dispatch(BasePacket packet)
    {
        if (_handlers.TryGetValue(packet.Type, out var handler))
            handler(packet);
        else
            Debug.LogWarning($"[Dispatcher] Không có handler cho: {packet.Type}");
    }
}