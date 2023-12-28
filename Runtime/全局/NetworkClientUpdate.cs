namespace JFramework.Net
{
    public partial class NetworkManager
    {
        public partial class NetworkClient
        {
            /// <summary>
            /// 在Update前调用
            /// </summary>
            internal void EarlyUpdate()
            {
                if (Transport.current != null)
                {
                    Transport.current.ClientEarlyUpdate();
                }

                connection?.UpdateInterpolation();
            }

            /// <summary>
            /// 在Update之后调用
            /// </summary>
            internal void AfterUpdate()
            {
                if (isActive)
                {
                    if (NetworkUtils.HeartBeat(NetworkTime.localTime, Instance.sendRate, ref lastSendTime))
                    {
                        Broadcast();
                    }
                }

                if (connection != null)
                {
                    if (Instance.mode == NetworkMode.Host)
                    {
                        connection.Update();
                    }
                    else
                    {
                        if (isActive && isConnect)
                        {
                            NetworkTime.Update();
                            connection.Update();
                        }
                    }
                }

                if (Transport.current != null)
                {
                    Transport.current.ClientAfterUpdate();
                }
            }

            /// <summary>
            /// 客户端进行广播
            /// </summary>
            private void Broadcast()
            {
                if (!connection.isReady) return;
                if (Server.isActive) return;
                foreach (var @object in spawns.Values)
                {
                    using var writer = NetworkWriter.Pop();
                    @object.ClientSerialize(writer);
                    if (writer.position > 0)
                    {
                        SendMessage(new EntityMessage(@object.objectId, writer.ToArraySegment()));
                        @object.ClearDirty();
                    }
                }

                SendMessage(new SnapshotMessage(), Channel.Unreliable);
            }
        }
    }
}