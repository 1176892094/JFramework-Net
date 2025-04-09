// *********************************************************************************
// # Project: JFramework
// # Unity: 6000.3.5f1
// # Author: 云谷千羽
// # Version: 1.0.0
// # History: 2025-01-10 21:01:21
// # Recently: 2025-01-10 21:01:31
// # Copyright: 2024, 云谷千羽
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;

namespace JFramework.Net
{
    internal class Process
    {
        private readonly Dictionary<string, Room> rooms = new Dictionary<string, Room>();
        private readonly Dictionary<int, Room> clients = new Dictionary<int, Room>();
        private readonly HashSet<int> connections = new HashSet<int>();
        private readonly Transport transport;

        public Process(Transport transport)
        {
            this.transport = transport;
        }

        public List<Room> roomData => rooms.Values.ToList();

        public void ServerConnect(int clientId)
        {
            connections.Add(clientId);
            using var setter = MemorySetter.Pop();
            setter.SetByte((byte)OpCodes.Connect);
            transport.SendToClient(clientId, setter);
        }

        public void ServerDisconnect(int clientId)
        {
            var copies = rooms.Values.ToList();
            foreach (var room in copies)
            {
                if (room.clientId == clientId) // 主机断开
                {
                    using var setter = MemorySetter.Pop();
                    setter.SetByte((byte)OpCodes.LeaveRoom);
                    foreach (var client in room.clients)
                    {
                        transport.SendToClient(client, setter);
                        clients.Remove(client);
                    }

                    room.clients.Clear();
                    rooms.Remove(room.roomId);
                    clients.Remove(clientId);
                    return;
                }

                if (room.clients.Remove(clientId)) // 客户端断开
                {
                    using var setter = MemorySetter.Pop();
                    setter.SetByte((byte)OpCodes.KickRoom);
                    setter.SetInt(clientId);
                    transport.SendToClient(room.clientId, setter);
                    clients.Remove(clientId);
                    break;
                }
            }
        }

        public void ServerReceive(int clientId, ArraySegment<byte> segment, int channel)
        {
            try
            {
                using var getter = MemoryGetter.Pop(segment);
                var opcode = (OpCodes)getter.GetByte();
                if (opcode == OpCodes.Connected)
                {
                    if (connections.Contains(clientId))
                    {
                        var serverKey = getter.GetString();
                        if (serverKey == Program.Setting.ServerKey)
                        {
                            using var setter = MemorySetter.Pop();
                            setter.SetByte((byte)OpCodes.Connected);
                            transport.SendToClient(clientId, setter);
                        }

                        connections.Remove(clientId);
                    }
                }
                else if (opcode == OpCodes.CreateRoom)
                {
                    ServerDisconnect(clientId);
                    string id;
                    do
                    {
                        id = new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 5).Select(s => s[Service.Random.Next(s.Length)]).ToArray());
                    } while (rooms.ContainsKey(id));

                    var room = new Room
                    {
                        roomId = id,
                        clientId = clientId,
                        roomName = getter.GetString(),
                        roomData = getter.GetString(),
                        maxCount = getter.GetInt(),
                        roomMode = getter.GetByte(),
                        clients = new HashSet<int>(),
                    };

                    rooms.Add(id, room);
                    clients.Add(clientId, room);
                    Log.Info(Service.Text.Format("客户端 {0} 创建房间。 房间名称: {1} 房间数: {2} 连接数: {3}", clientId, room.roomName, rooms.Count, clients.Count));

                    using var setter = MemorySetter.Pop();
                    setter.SetByte((byte)OpCodes.CreateRoom);
                    setter.SetString(room.roomId);
                    transport.SendToClient(clientId, setter);
                }
                else if (opcode == OpCodes.JoinRoom)
                {
                    ServerDisconnect(clientId);
                    var roomId = getter.GetString();
                    if (rooms.TryGetValue(roomId, out var room) && room.clients.Count + 1 < room.maxCount)
                    {
                        room.clients.Add(clientId);
                        clients.Add(clientId, room);
                        Log.Info(Service.Text.Format("客户端 {0} 加入房间。 房间名称: {1} 房间数: {2} 连接数: {3}", clientId, room.roomName, rooms.Count, clients.Count));

                        using var setter = MemorySetter.Pop();
                        setter.SetByte((byte)OpCodes.JoinRoom);
                        setter.SetInt(clientId);
                        transport.SendToClient(clientId, setter);
                        transport.SendToClient(room.clientId, setter);
                    }
                    else
                    {
                        using var setter = MemorySetter.Pop();
                        setter.SetByte((byte)OpCodes.LeaveRoom);
                        transport.SendToClient(clientId, setter);
                    }
                }
                else if (opcode == OpCodes.UpdateRoom)
                {
                    if (clients.TryGetValue(clientId, out var room))
                    {
                        room.roomName = getter.GetString();
                        room.roomData = getter.GetString();
                        room.roomMode = getter.GetByte();
                        room.maxCount = getter.GetInt();
                    }
                }
                else if (opcode == OpCodes.LeaveRoom)
                {
                    ServerDisconnect(clientId);
                }
                else if (opcode == OpCodes.UpdateData)
                {
                    var message = getter.GetArraySegment();
                    var targetId = getter.GetInt();
                    if (clients.TryGetValue(clientId, out var room) && room != null)
                    {
                        if (message.Count > transport.SendLength(channel))
                        {
                            Log.Warn(Service.Text.Format("接收消息大小过大！消息大小: {0}", message.Count));
                            ServerDisconnect(clientId);
                            return;
                        }

                        if (room.clientId == clientId)
                        {
                            if (room.clients.Contains(targetId))
                            {
                                using var setter = MemorySetter.Pop();
                                setter.SetByte((byte)OpCodes.UpdateData);
                                setter.SetArraySegment(message);
                                transport.SendToClient(targetId, setter, channel);
                            }
                        }
                        else
                        {
                            using var setter = MemorySetter.Pop();
                            setter.SetByte((byte)OpCodes.UpdateData);
                            setter.SetArraySegment(message);
                            setter.SetInt(clientId);
                            transport.SendToClient(room.clientId, setter, channel);
                        }
                    }
                }
                else if (opcode == OpCodes.KickRoom)
                {
                    var targetId = getter.GetInt();
                    var copies = rooms.Values.ToList();
                    foreach (var room in copies)
                    {
                        if (room.clientId == targetId) // 踢掉的是主机
                        {
                            using var setter = MemorySetter.Pop();
                            setter.SetByte((byte)OpCodes.LeaveRoom);
                            foreach (var client in room.clients)
                            {
                                transport.SendToClient(client, setter);
                                clients.Remove(client);
                            }

                            room.clients.Clear();
                            rooms.Remove(room.roomId);
                            clients.Remove(targetId);
                            return;
                        }

                        if (room.clientId == clientId) // 踢掉的是客户端
                        {
                            if (room.clients.Remove(targetId))
                            {
                                using var setter = MemorySetter.Pop();
                                setter.SetByte((byte)OpCodes.KickRoom);
                                setter.SetInt(targetId);
                                transport.SendToClient(room.clientId, setter);
                                clients.Remove(targetId);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                transport.StopClient(clientId);
            }
        }
    }
}