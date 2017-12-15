using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;

namespace MonitorFasceEnergiaElettrica.Services
{
    public class NetworkTimeProtocolService
    {
        public const string DEFAULT_NTP_SERVER_URL = "pool.ntp.org";
        public const int DEFAULT_REFRESH_INTERVAL = 1;  // In Hours
        public const int DEFAULT_UTC_OFFSET = 1;    // In Hours

        private Timer _ntpTimer;

        public event DateTimeUpatedEventHandler DateTimeUpdated;

        public int RefreshInterval { get; private set; }

        public string NtpServer { get; private set; }

        public int UtcOffset { get; private set; }

        protected virtual void OnDateTimeUpdated(DateTimeUpdatedEventArgs args)
        {
            Debug.Print("OnDateTimeUpdated");

            var handler = DateTimeUpdated;

            if (DateTimeUpdated != null)
            {
                Debug.Print("handler");

                handler(this, args);
            }
        }

        public NetworkTimeProtocolService(string ntpServer = DEFAULT_NTP_SERVER_URL, int utcOffset = DEFAULT_UTC_OFFSET, int refreshInterval = DEFAULT_REFRESH_INTERVAL)
        {
            this.NtpServer = ntpServer;
            this.UtcOffset = utcOffset;
            this.RefreshInterval = refreshInterval;
        }

        public DateTime GetNetworkTime(string ntpServer = DEFAULT_NTP_SERVER_URL, int utcOffset = DEFAULT_UTC_OFFSET)
        {
            this.NtpServer = ntpServer;
            this.UtcOffset = utcOffset;

            DateTime dtNull = new DateTime();
            Socket socket = null;

            try
            {
                EndPoint ep = new IPEndPoint(Dns.GetHostEntry(DEFAULT_NTP_SERVER_URL).AddressList[0], 123);
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                socket.Connect(ep);
                byte[] ntpData = new byte[48]; // RFC 2030
                Array.Clear(ntpData, 0, 48);
                ntpData[0] = 0x1B;

                socket.SendTo(ntpData, ep);

                if (socket.Poll(10 * 1000 * 1000, SelectMode.SelectRead)) // Waiting an answer for 30s, if nothing: timeout
                {

                    socket.ReceiveFrom(ntpData, ref ep); // Receive Time

                    byte offsetTransmitTime = 40;
                    ulong intpart = 0;
                    ulong fractpart = 0;
                    for (int i = 0; i <= 3; i++) intpart = (intpart << 8) | ntpData[offsetTransmitTime + i];
                    for (int i = 4; i <= 7; i++) fractpart = (fractpart << 8) | ntpData[offsetTransmitTime + i];
                    ulong milliseconds = (intpart * 1000 + (fractpart * 1000) / 0x100000000L);

                    socket.Close();

                    TimeSpan timeSpan = TimeSpan.FromTicks((long)milliseconds * TimeSpan.TicksPerMillisecond);
                    DateTime dateTime = new DateTime(1900, 1, 1);

                    dateTime += timeSpan;

                    //TimeSpan offsetAmount = TimeZone.CurrentTimeZone.GetUtcOffset(dateTime);
                    TimeSpan offsetAmount = new TimeSpan(this.UtcOffset, 0, 0);

                    //Debug.Print("UTC Offset");
                    //Debug.Print(TimeZone.CurrentTimeZone.GetUtcOffset(dateTime).ToString());

                    DateTime networkDateTime = (dateTime + offsetAmount);

                    OnDateTimeUpdated(new DateTimeUpdatedEventArgs { DateTime = networkDateTime });

                    return networkDateTime;
                }

                socket.Close();
            }
            catch
            {
                try { socket.Close(); }
                catch { }
            }

            return dtNull;
        }

        public void Start(string ntpServer = DEFAULT_NTP_SERVER_URL, int utcOffset = DEFAULT_UTC_OFFSET, int refreshInterval = DEFAULT_REFRESH_INTERVAL)
        {
            this.NtpServer = ntpServer;
            this.RefreshInterval = refreshInterval;
            this.UtcOffset = utcOffset;

            Debug.Print("NTP:: Starting...");

            // Now we want to see at least once that the system time gets updated
            //var dateTime = GetNetworkTime();

            //Debug.Print("Date Time");
            //Debug.Print(dateTime.ToString());

            // Install timer to sync time every 'sleepTime' minutes

            //Debug.Print("refreshInterval");
            //Debug.Print(refreshInterval.ToString());

            TimeSpan ts = new TimeSpan(this.RefreshInterval, 0, 0);
            _ntpTimer = new Timer(callback: new TimerCallback(this.NtpTimerCallback), state: null, dueTime: new TimeSpan(0), period: ts);
        }

        public void Stop()
        {
            if (_ntpTimer != null)
            {
                _ntpTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        public void NtpTimerCallback(object state)
        {
            Debug.Print("NtpTimerCallback");

            GetNetworkTime(this.NtpServer, this.UtcOffset);
        }
    }

    public delegate void DateTimeUpatedEventHandler(object sender, DateTimeUpdatedEventArgs e);

    public class DateTimeUpdatedEventArgs : EventArgs
    {
        public DateTime DateTime { get; set; }
    }
}
