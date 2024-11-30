using System;
using System.Collections.Generic;

namespace JFramework.Udp
{
    [Serializable]
    public struct Setting
    {
        public int MaxUnit;
        public uint Timeout;
        public uint Interval;
        public uint DeadLink;
        public uint FastResend;
        public uint SendWindow;
        public uint ReceiveWindow;
        public bool NoDelay;
        public bool DualMode;
        public bool Congestion;

        public Setting(
            int MaxUnit = Kcp.MTU_DEF,
            uint Timeout = Kcp.TIME_OUT,
            uint Interval = 10,
            uint DeadLink = Kcp.DEAD_LINK,
            uint FastResend = 0,
            uint SendWindow = Kcp.WND_SND,
            uint ReceiveWindow = Kcp.WND_RCV,
            bool NoDelay = true,
            bool DualMode = true,
            bool Congestion = false)
        {
            this.MaxUnit = MaxUnit;
            this.Timeout = Timeout;
            this.Interval = Interval;
            this.DeadLink = DeadLink;
            this.FastResend = FastResend;
            this.SendWindow = SendWindow;
            this.ReceiveWindow = ReceiveWindow;
            this.NoDelay = NoDelay;
            this.DualMode = DualMode;
            this.Congestion = Congestion;
        }
    }
}