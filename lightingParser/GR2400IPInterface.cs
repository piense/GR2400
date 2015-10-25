using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace lightingParser
{

    public class dataLog
    {
        public byte[] FromPC;
        public byte[] ToPC;
        public string Title;
        public string Description;
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

    public class GR2400IPInterface
    {

        public List<dataLog> LightingDataLog;

        public event EventHandler NewMessage;

        IPEndPoint receiveEndpoint;
        IPEndPoint serverEndpoint;
        UdpClient udpClient;

        private void OnNewMessage(IAsyncResult ar)
        {
            UdpClient u = (UdpClient)((UdpState)(ar.AsyncState)).u;
            IPEndPoint e = (IPEndPoint)((UdpState)(ar.AsyncState)).e;

            Byte[] receiveBytes = u.EndReceive(ar, ref e);
            string receiveString = Encoding.ASCII.GetString(receiveBytes);

            dataLog logEntry = new dataLog(null, receiveBytes, "", "");

            if (receiveBytes.Length == 13 && receiveBytes.SequenceEqual(new byte[13] { 0x33, 0x42, 0x30, 0x36, 0x46, 0x38, 0x30, 0x31, 0x43, 0x31, 0x46, 0x42, 0x0d }))
            {
                logEntry.Title = "Heartbeat";
                logEntry.Description = "Heartbeat from GR2400 system.";
            }

            LightingDataLog.Add(logEntry);
            u.BeginReceive(new AsyncCallback(OnNewMessage), ((UdpState)(ar.AsyncState)));

            EventHandler handler = NewMessage;
            if (handler != null) { handler(this, new MessageEventArgs(receiveString, receiveBytes)); }
        }

        public void QueryLCDData()
        {
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600ff3d" + (char)0x0d + (char)0x0a, "Query LCD Data", "Query LCD Data");
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

        public string QueryID(int id)
        {
            string ToSend = "3820" + (char)0x0d + "37" + id.ToString("x2") + ((int)((55 + id) % 255)).ToString("x2") + (char)0x0d + (char)0x0a;
            byte[] sendbuf = Encoding.ASCII.GetBytes(ToSend);

            if (udpClient != null && serverEndpoint != null)
                udpClient.Send(sendbuf, sendbuf.Length, serverEndpoint);


            //byte[] retBuf = udpClient.Receive(ref receiveEndpoint);
            //MessageBox.Show(System.Text.Encoding.ASCII.GetString(retBuf));

            /*
                1   3F0E30631011B
                30  3F0E302320219
                34  3F0E304350521

                6 button switch: 3F0E1000600E7
                nothing: 3F00000000000
                Heartbaet: 3B06F801C1FB
             */
            return "";
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

            RequestLCDData();

            udpClient.BeginReceive(new AsyncCallback(OnNewMessage), s);
        }

        public void RequestLCDData()
        {
            byte[] sendbuf = Encoding.ASCII.GetBytes("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600ff3d" + (char)0x0d + (char)0x0a);

            try
            {
                serverEndpoint = new IPEndPoint(IPAddress.Parse("10.1.4.152"), 10001);
                udpClient.Send(sendbuf, sendbuf.Length, serverEndpoint);
            }
            catch (Exception a)
            {
                MessageBox.Show(a.ToString());
            }
        }

        public GR2400IPInterface()
        {
            LightingDataLog = new List<dataLog>();
            beginListening();
        }
    }
}
