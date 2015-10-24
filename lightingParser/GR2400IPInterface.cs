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

        public event EventHandler NewMessage;

        IPEndPoint receiveEndpoint;
        IPEndPoint serverEndpoint;
        UdpClient udpClient;

        AsyncCallback currentReceiveCallback;

        private void OnNewMessage(IAsyncResult ar)
        {
            UdpClient u = (UdpClient)((UdpState)(ar.AsyncState)).u;
            IPEndPoint e = (IPEndPoint)((UdpState)(ar.AsyncState)).e;

            Byte[] receiveBytes = u.EndReceive(ar, ref e);
            string receiveString = Encoding.ASCII.GetString(receiveBytes);

            //if (receiveBytes.Length == 13 && receiveBytes.SequenceEqual(new byte[13] { 0x33, 0x42, 0x30, 0x36, 0x46, 0x38, 0x30, 0x31, 0x43, 0x31, 0x46, 0x42, 0x0d }))
            //    QueryLCDData();

            currentReceiveCallback = new AsyncCallback(OnNewMessage);

            u.BeginReceive(currentReceiveCallback, ((UdpState)(ar.AsyncState)));

            EventHandler handler = NewMessage;
            if (handler != null) { handler(this, new MessageEventArgs(receiveString,receiveBytes)); }
        }

        public void QueryLCDData()
        {
            byte[] sendbuf = Encoding.ASCII.GetBytes("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600ff3d" + (char)0x0d + (char)0x0a);
            if(udpClient != null && serverEndpoint != null)
                udpClient.Send(sendbuf, sendbuf.Length, serverEndpoint);
        }

        private void sendData(byte[]sendbuf)
        {
            if (udpClient != null && serverEndpoint != null)
                udpClient.Send(sendbuf, sendbuf.Length, serverEndpoint);
        }

        private void sendData(string sendbuf)
        {
            sendData(Encoding.ASCII.GetBytes(sendbuf));
        }

        public void ScrollUp()
        {
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600fe3c" + (char)0x0d + (char)0x0a);
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600ff3d" + (char)0x0d + (char)0x0a);
        }

        public void ScrollDown()
        {
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600fd3b" + (char)0x0d + (char)0x0a);
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600ff3d" + (char)0x0d + (char)0x0a);
        }

        public void Exit()
        {
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600df1d" + (char)0x0d + (char)0x0a);
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600ff3d" + (char)0x0d + (char)0x0a);
        }

        public void Delete()
        {
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600bffd" + (char)0x0d + (char)0x0a);
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600ff3d" + (char)0x0d + (char)0x0a);
        }

        public void Help()
        {
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "3806007fbd" + (char)0x0d + (char)0x0a);
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600ff3d" + (char)0x0d + (char)0x0a);
        }

        public void Enter()
        {
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600ef2d" + (char)0x0d + (char)0x0a);
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600ff3d" + (char)0x0d + (char)0x0a);
        }

        public void TabUp()
        {
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600fb39" + (char)0x0d + (char)0x0a);
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600ff3d" + (char)0x0d + (char)0x0a);
        }

        public void TabDown()
        {
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600f735" + (char)0x0d + (char)0x0a);
            sendData("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600ff3d" + (char)0x0d + (char)0x0a);
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

            byte[] sendbuf = Encoding.ASCII.GetBytes("38d0" + (char)0x0d + "38d108" + (char)0x0d + "380600ff3d" + (char)0x0d + (char)0x0a);

            try
            {
                serverEndpoint =  new IPEndPoint(IPAddress.Parse("10.1.4.152"),10001);
                udpClient.Send(sendbuf, sendbuf.Length, serverEndpoint);
            }
            catch (Exception a)
            {
                MessageBox.Show(a.ToString());
            }

            currentReceiveCallback = new AsyncCallback(OnNewMessage);
            udpClient.BeginReceive(currentReceiveCallback, s);
        }

        public GR2400IPInterface()
        {
            beginListening();
        }
    }
}
