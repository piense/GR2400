using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.ComponentModel;

namespace lightingParser
{

    public class dataLog
    {
        public byte[] FromPC;
        public byte[] ToPC;
        public string Title;
        public string Description;
        public DateTime Time;
        public dataLog(byte[] fromPC, byte[] toPC, string title, string description)
        {
            if (fromPC != null)
                FromPC = fromPC;
            else
                FromPC = null;
            if (toPC != null)
                ToPC = toPC;
            else
                ToPC = null;
            Title = title;
            Description = description;
            Time = DateTime.Now;
        }

        public override string ToString()
        {
            return Title;
        }
    }

    public enum RelayStateT { On, Off, Unknown};

    public class RelayC
    {
        public int Id;
        public int Relay;
        public RelayStateT RelayState;
        public DateTime LastUpdated;
        public RelayC(int id, int relay, RelayStateT relayState)
        {
            Id = id;
            Relay = relay;
            RelayState = relayState;
            LastUpdated = DateTime.Now;
        }
    }

    public class UdpState
    {
        public IPEndPoint e;
        public UdpClient u;
    }

    public class MessageEventArgs : EventArgs
    {
        public string Message;
        public byte[] MessageBytes;
        public MessageEventArgs(string message, byte[] messageBytes) { Message = message; MessageBytes = messageBytes; }
    }

    enum LastQueryT { Relay, DeviceScan, LCD};

    public class GR2400IPInterface
    {

        public BindingList<dataLog> LightingDataLog;

        public event EventHandler NewMessage;

        IPEndPoint receiveEndpoint;
        IPEndPoint serverEndpoint;
        UdpClient udpClient;
        LastQueryT LastQuery;
        int lastRelayQueryId;
        int lastRelayQueryRelay;
        int lastDeviceQueryId;

        public List<RelayC> DiscoveredRelays;

        bool probingRelays;

        public bool RelayExists(int id, int relay)
        {

            int id2 = id + (int)relay / 8;
            int relay2 = relay % 8;

            foreach (RelayC r in DiscoveredRelays)
                if ((r.Id + (int)r.Relay / 8) == (id + (int)relay / 8) && (r.Relay % 8) == (relay % 8))
                    return true;

            return false;
        }

        public RelayC getOldestOrUnknownRelay()
        {
            

            if (DiscoveredRelays.Count == 0)
                return null;

            RelayC currentOldest = DiscoveredRelays.FirstOrDefault();

            foreach (RelayC r in DiscoveredRelays)
                if (r.RelayState == RelayStateT.Unknown)
                    return r;

            foreach (RelayC r in DiscoveredRelays)
                if (r.LastUpdated < currentOldest.LastUpdated)
                    currentOldest = r;

            return currentOldest;
        }

        public void addRelay(int id, int relay, RelayStateT relayState)
        {
            if (!RelayExists(id, relay))
                DiscoveredRelays.Add(new RelayC(id, relay, relayState));
        }

        bool checkForMatch(byte[] array1, byte[] array2, int start)
        {
            if (array1.Length < (start + array2.Length))
                return false;

            for (int i = 0; i < array2.Length; i++)
                if (array1[start + i] != array2[i])
                    return false;

            return true;
        }

        void AddRelays(int id, int qty)
        {
            for (int i = 0; i < qty; i++) {
                addRelay(id, i, RelayStateT.Unknown);
            }
        }

        void updateRelay(int id, int relay, RelayStateT s)
        {
            foreach (RelayC r in DiscoveredRelays)
                if ((r.Id + (int)r.Relay / 8) == (id + (int)relay / 8) && (r.Relay % 8) == (relay % 8))
                {
                    r.RelayState = s;
                    r.LastUpdated = DateTime.Now;
                }
        }

        void probeNextRelay()
        {
            Thread.Sleep(50);
            if (probingRelays == false)
                return;
            RelayC r = getOldestOrUnknownRelay();
            if (r == null)
                return;
            QueryRelay(r.Id, r.Relay);
        }

        private void OnNewMessage(IAsyncResult ar)
        {
            UdpClient u = (UdpClient)((UdpState)(ar.AsyncState)).u;
            IPEndPoint e = (IPEndPoint)((UdpState)(ar.AsyncState)).e;

            Byte[] receiveBytes = u.EndReceive(ar, ref e);
            string receiveString = Encoding.ASCII.GetString(receiveBytes);

            dataLog logEntry = new dataLog(null, receiveBytes, "Unknown", "Unknown");

            if (receiveBytes.Length == 13 && receiveBytes.SequenceEqual(new byte[13] { 0x33, 0x42, 0x30, 0x36, 0x46, 0x38, 0x30, 0x31, 0x43, 0x31, 0x46, 0x42, 0x0D }))
            {
                logEntry.Title = "Heartbeat 1";
                logEntry.Description = "Heartbeat from GR2400 system";
            }

            if (receiveBytes.Length == 13 && receiveBytes.SequenceEqual(new byte[13] { 0x33,0x42, 0x30, 0x34, 0x46, 0x38, 0x30, 0x31, 0x43, 0x31, 0x46, 0x39, 0x0D}))
            {
                logEntry.Title = "Heartbeat 2";
                logEntry.Description = "Heartbeat from GR2400 system";
            }

            if (receiveBytes.Length == 13 && receiveBytes.SequenceEqual(new byte[13] { 0x33, 0x42, 0x30, 0x30, 0x38, 0x30, 0x30, 0x31, 0x43, 0x30, 0x37, 0x43, 0x0D }))
            {
                logEntry.Title = "Heartbeat 3";
                logEntry.Description = "Heartbeat from GR2400 system";
            }

            if (receiveBytes.Length == 13 && receiveBytes.SequenceEqual(new byte[13] { 0x33, 0x42, 0x30, 0x36, 0x30, 0x30, 0x30, 0x31, 0x43, 0x30, 0x30, 0x32, 0x0D }))
            {
                logEntry.Title = "Heartbeat 4";
                logEntry.Description = "Heartbeat from GR2400 system";
            }

            if (checkForMatch(receiveBytes, new byte[7] { 0x33, 0x46, 0x30, 0x30, 0x30, 0x30, 0x30 }, 0))
            {
                switch(LastQuery)
                {
                    case LastQueryT.Relay: logEntry.Title = "Relay #" + (lastRelayQueryRelay + 1).ToString() + " at id " + lastRelayQueryId.ToString() + " is off"; logEntry.Description = logEntry.Title; updateRelay(lastRelayQueryId, lastRelayQueryRelay, RelayStateT.Off); probeNextRelay(); break;
                    case LastQueryT.DeviceScan: logEntry.Title = "No Device at ID " + lastDeviceQueryId; logEntry.Description = logEntry.Title; break;
                    case LastQueryT.LCD: logEntry.Title = "Negative LCD Query?"; logEntry.Description = "Negative LCD Query"; break;
                    default: logEntry.Title = "Negative"; logEntry.Description = "Negative"; break;
                }
            }

            if (checkForMatch(receiveBytes, new byte[8] { 0x33, 0x46, 0x30, 0x30, 0x31, 0x30, 0x31, 0x0D }, 0))
            {
                switch (LastQuery)
                {
                    case LastQueryT.Relay: logEntry.Title = "Relay #" + (lastRelayQueryRelay + 1).ToString() + " at id " + lastRelayQueryId.ToString() + " is on"; logEntry.Description = logEntry.Title; updateRelay(lastRelayQueryId, lastRelayQueryRelay, RelayStateT.On); probeNextRelay(); break;
                    case LastQueryT.DeviceScan: logEntry.Title = "Affirmative Device?" + lastDeviceQueryId; logEntry.Description = logEntry.Title; break;
                    case LastQueryT.LCD: logEntry.Title = "Affirmative LCD Query?"; logEntry.Description = "Affirmative LCD Query"; break;
                    default: logEntry.Title = "Affirmative 1"; logEntry.Description = "Affirmative 1"; break;
                }
            }

            if (checkForMatch(receiveBytes, new byte[8] { 0x33, 0x46, 0x30, 0x32, 0x31, 0x32, 0x31, 0x0D }, 0))
            {
                switch (LastQuery)
                {
                    case LastQueryT.Relay: logEntry.Title = "Relay #" + (lastRelayQueryRelay + 1).ToString() + " at id " + lastRelayQueryId.ToString() + " is on"; logEntry.Description = logEntry.Title + " & first"; updateRelay(lastRelayQueryId, lastRelayQueryRelay, RelayStateT.On); probeNextRelay(); break;
                    case LastQueryT.DeviceScan: logEntry.Title = "Affirmative Device?" + lastDeviceQueryId; logEntry.Description = logEntry.Title + " & first"; break;
                    case LastQueryT.LCD: logEntry.Title = "Affirmative LCD Query?"; logEntry.Description = "Affirmative LCD Query" + " & first"; break;
                    default: logEntry.Title = "Affirmative 2"; logEntry.Description = "Affirmative 2"; break;
                }
            }

            if (checkForMatch(receiveBytes, new byte[8] { 0x33, 0x46, 0x30, 0x32, 0x30, 0x32, 0x30, 0x0D }, 0))
            {
                switch (LastQuery)
                {
                    case LastQueryT.Relay: logEntry.Title = "Relay #" + (lastRelayQueryRelay + 1).ToString() + " at id " + lastRelayQueryId.ToString() + " is off"; logEntry.Description = logEntry.Title + " first"; updateRelay(lastRelayQueryId, lastRelayQueryRelay, RelayStateT.Off); probeNextRelay(); break;
                    case LastQueryT.DeviceScan: logEntry.Title = "No Device at ID " + lastDeviceQueryId; logEntry.Description = logEntry.Title + " first"; break;
                    case LastQueryT.LCD: logEntry.Title = "Negative LCD Query?"; logEntry.Description = "Negative LCD Query" + " first"; break;
                    default: logEntry.Title = "Negative 2"; logEntry.Description = "Negative" + " first"; break;
                }
            }

            if (checkForMatch(receiveBytes, new byte[3] { 0x33, 0x38, 0x30},0))
            {
                logEntry.Title = "LCD Text";
                logEntry.Description = "LCD Text";
            }

            if (checkForMatch(receiveBytes, new byte[4] { 0x33, 0x46, 0x30, 0x45 }, 0))
            {
                logEntry.Title = "Device found";
                logEntry.Description = "Device of some sort has been detected.";

                if (checkForMatch(receiveBytes, new byte[7] { 0x33, 0x46, 0x30, 0x45, 0x31, 0x30, 0x30 }, 0))
                {
                    logEntry.Title = "Scan Response - 6 Btn Swtch";
                    logEntry.Description = "6 Button Chelsea Switch";
                }

                if (checkForMatch(receiveBytes, new byte[7] { 0x33, 0x46, 0x30, 0x45, 0x33, 0x30, 0x30 }, 0))
                {
                    logEntry.Title = "Scan Response - Continuation";
                    logEntry.Description = "ID occupied by the previously detected panel";
                }

                if (checkForMatch(receiveBytes, new byte[7] { 0x33, 0x46, 0x30, 0x45, 0x33, 0x30, 0x31 }, 0))
                {
                    logEntry.Title = "Scan Response - 8 Chnl Panel";                  
                    logEntry.Description = "8 Channel Panel";
                    AddRelays(lastDeviceQueryId, 8);
                }

                if (checkForMatch(receiveBytes, new byte[7] { 0x33, 0x46, 0x30, 0x45, 0x33, 0x30, 0x32 }, 0))
                {
                    logEntry.Title = "Scan Response - 16 Chnl Panel";
                    logEntry.Description = "16 Channel Panel";
                    AddRelays(lastDeviceQueryId, 16);
                }

                if (checkForMatch(receiveBytes, new byte[7] { 0x33, 0x46, 0x30, 0x45, 0x33, 0x30, 0x33 }, 0))
                {
                    AddRelays(lastDeviceQueryId, 24);
                    logEntry.Title = "Scan Response - 24 Chnl Panel";
                    logEntry.Description = "24 Channel Panel";
                }

                if (checkForMatch(receiveBytes, new byte[7] { 0x33, 0x46, 0x30, 0x45, 0x33, 0x30, 0x34 }, 0))
                {
                    logEntry.Title = "Scan Response - 32 Chnl Panel";
                    logEntry.Description = "32 Channel Panel";
                    AddRelays(lastDeviceQueryId, 32);
                }

                if (checkForMatch(receiveBytes, new byte[7] { 0x33, 0x46, 0x30, 0x45, 0x33, 0x30, 0x35 }, 0))
                {
                    logEntry.Title = "Scan Response - 40 Chnl Panel";
                    logEntry.Description = "40 Channel Panel";
                    AddRelays(lastDeviceQueryId, 40);
                }

                if (checkForMatch(receiveBytes, new byte[14] { 0x33, 0x46, 0x30, 0x45, 0x33, 0x30, 0x36, 0x33, 0x31, 0x30, 0x31, 0x31, 0x42, 0x0d }, 0))
                {
                    logEntry.Title = "Scan Response - 48 Chnl Panel";
                    logEntry.Description = "48 Channel Panel";
                    AddRelays(lastDeviceQueryId, 48);
                }
            }



            LightingDataLog.Add(logEntry);
            u.BeginReceive(new AsyncCallback(OnNewMessage), ((UdpState)(ar.AsyncState)));

            EventHandler handler = NewMessage;
            if (handler != null) { handler(this, new MessageEventArgs(receiveString, receiveBytes)); }
        }

        public void QueryLCDData()
        {
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600ff3d" + (char)0x0d + (char)0x0a, "Query LCD Data", "Query LCD Data");
            LastQuery = LastQueryT.LCD;
        }

        private void sendData(byte[] sendbuf, string title, string description)
        {
            LightingDataLog.Add(new dataLog(sendbuf, null, title, description));
            if (udpClient != null && serverEndpoint != null)
                udpClient.Send(sendbuf, sendbuf.Length, serverEndpoint);
        }

        private void sendData(string sendbuf, string title, string description)
        {
            sendData(Encoding.ASCII.GetBytes(sendbuf), title, description);
        }

        public void ScrollUp()
        {
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600fe3c" + (char)0x0d + (char)0x0a, "Scroll Up Key Down", "Scroll Up Key Down");
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600ff3d" + (char)0x0d + (char)0x0a, "Scroll Up Key Up", "Scroll Up Key Up");
        }

        public void ScrollDown()
        {
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600fd3b" + (char)0x0d + (char)0x0a, "Scroll Down Key Down", "Scroll Down Key Down");
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600ff3d" + (char)0x0d + (char)0x0a, "Scroll Down Key Up", "Scroll Down Key Up");
        }

        public void Exit()
        {
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600df1d" + (char)0x0d + (char)0x0a, "Exit Key Down", "Exit Key Down");
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600ff3d" + (char)0x0d + (char)0x0a, "Exit Key Down", "Exit Key Up");
        }

        public void Delete()
        {
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600bffd" + (char)0x0d + (char)0x0a, "Delete Key Down", "Delete Key Down");
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600ff3d" + (char)0x0d + (char)0x0a, "Delete Key Up", "Delete Key Up");
        }

        public void Help()
        {
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "3806007fbd" + (char)0x0d + (char)0x0a, "Help Key Down", "Help Key Down");
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600ff3d" + (char)0x0d + (char)0x0a, "Help Key Up", "Help Key Up");
        }

        public void Enter()
        {
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600ef2d" + (char)0x0d + (char)0x0a, "Enter Key Down", "Help Key Down");
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600ff3d" + (char)0x0d + (char)0x0a, "Enter Key Up", "Enter Key Up");
        }

        public void TabUp()
        {
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600fb39" + (char)0x0d + (char)0x0a, "Tab Up Key Down", "Tab Up Key Down");
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600ff3d" + (char)0x0d + (char)0x0a, "Tab Up Key Up", "Tab Up Key Up");
        }

        public void TabDown()
        {
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600f735" + (char)0x0d + (char)0x0a, "Tab Down Key Down", "Tab Down Key Down");
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600ff3d" + (char)0x0d + (char)0x0a, "Tab Down Key Up", "Tab Down Key Up");
        }

        public void QueryID(int id)
        {
            string ToSend = "3820" + (char)0x0d + "37" + id.ToString("x2") + ((int)((55 + id) % 255)).ToString("x2") + (char)0x0d + (char)0x0a;

            sendData(ToSend, "Query ID " + id.ToString(), "Query ID " + id.ToString());

            lastDeviceQueryId = id;
            LastQuery = LastQueryT.DeviceScan;
        }

        public void QueryRelay(int id, int relay)
        {
            int id2 = id + (int)relay / 8;
            int relay2 = relay % 8;
            string ToSend = "3820" + (char)0x0d + "08" + id2.ToString("x2") + relay2.ToString("x2") + ((int)((8 + id2 + relay2) % 255)).ToString("x2") + (char)0x0d + (char)0x0a;

            sendData(ToSend, "Query Relay " + id.ToString() + " - " + (relay+1).ToString(), "Query Relay " + id.ToString() + " - " + (relay+1).ToString());

            lastRelayQueryId = id;
            lastRelayQueryRelay = relay;

            LastQuery = LastQueryT.Relay;
        }

        private void beginListening()
        {
            // Receive a message and write it to the console.

            try
            {
                receiveEndpoint = new IPEndPoint(IPAddress.Any, 9999);
                udpClient = new UdpClient(receiveEndpoint);
            }
            catch (Exception a)
            {
                MessageBox.Show(a.ToString());
                return;
            }

            UdpState s = new UdpState();
            s.e = receiveEndpoint;
            s.u = udpClient;

            serverEndpoint = new IPEndPoint(IPAddress.Parse("10.1.4.152"), 10001);

            QueryLCDData();

            udpClient.BeginReceive(new AsyncCallback(OnNewMessage), s);
        }

        public GR2400IPInterface()
        {
            LightingDataLog = new BindingList<dataLog>();
            DiscoveredRelays = new List<RelayC>();
            probingRelays = true;
            beginListening();
        }
    }
}
