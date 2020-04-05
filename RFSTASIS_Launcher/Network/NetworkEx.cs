using MiniLauncher.Network.BinaryConverter;
using MiniLauncher.Network.Packets;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MiniLauncher.Network
{
    public class StateObject
    {
        // Client socket.
        public Socket WorkSocket;
        // Size of receive buffer.
        public const int BufferSize = 256;
        // Receive buffer.
        public byte[] Buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder Sb = new StringBuilder();
    }

    public class NetworkEx
    {
        public event EventHandler OnConnected;
        public event EventHandler OnError;

        private bool _acceptRecive = true;
        public IPAddress IpAddress;
        private readonly string _ipAddr;
        private readonly int _port;

        private readonly ManualResetEvent _connectDone =
            new ManualResetEvent(false);
        private readonly ManualResetEvent _sendDone =
            new ManualResetEvent(false);
        private readonly ManualResetEvent _receiveDone =
            new ManualResetEvent(false);
        public Socket Client { get; set; }
        public bool IsResived { get; set; } = false;

        public NetworkEx(string sIpAddr, int iPort)
        {
            _ipAddr = sIpAddr;
            _port = iPort;
        }
        public void StartClient()
        {
            bool IsSuccessfully = false;
            while (!IsSuccessfully)
            {
                try
                {
                    IPAddress ipAddress = IPAddress.Parse(_ipAddr);
                    IpAddress = ipAddress;
                    IPEndPoint remoteEp = new IPEndPoint(ipAddress, _port);

                    Client = new Socket(AddressFamily.InterNetwork,
                        SocketType.Stream, ProtocolType.Tcp);

                    Client.BeginConnect(remoteEp,
                        ConnectCallback, Client);
                    var cd = _connectDone.WaitOne(2000);

                    Receive(Client);
                    _receiveDone.WaitOne();
                    if (cd && IsResived)
                        IsSuccessfully = true;
                }
                catch (Exception e)
                {
                    OnClientError();
                    Thread.Sleep(1000);
                }
                finally
                {
                    //Client.Shutdown(SocketShutdown.Both);
                    //Client.Close();
                    _receiveDone.Reset();
                    _connectDone.Reset();
                }
            }
        }
        public void StopListen()
        {
            _acceptRecive = false;
        }
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;

                client.EndConnect(ar);

                OnClientConnected();
                AcceptClientCheck(client);

                _connectDone.Set();
            }
            catch (Exception e)
            {
                OnClientError();
            }
        }

        private void Receive(Socket client)
        {
            
            try
            {
                StateObject state = new StateObject { WorkSocket = client };

                client.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0,
                    ReceiveCallback, state);
            }
            catch (Exception e)
            {
                OnClientError();
                _receiveDone.Set();
            }
        }
        private void ReceiveCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket client = state.WorkSocket;
            try
            {
                int bytesRead = client.EndReceive(ar);
                if (_acceptRecive)
                {
                    for (int i = 0; i < bytesRead;)
                    {
                        _MSG_HEADER msgHeader = BinaryStructConverter.FromByteArray<_MSG_HEADER>(state.Buffer, i, 4);

                        byte[] data = new byte[msgHeader.m_wSize - 4];
                        for (int j = 0; j < msgHeader.m_wSize - 4; ++j)
                        {
                            data[j] = state.Buffer[i + j + 4];
                        }
                        i += msgHeader.m_wSize;
                        DataAnalyze(client, msgHeader, data);
                    }
                    client.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0,
                            ReceiveCallback, state);
                }
                else
                {
                    _receiveDone.Set();
                }
            }
            catch (Exception exc)
            {
                OnClientError();
                _receiveDone.Set();
                //throw new Exception("Disconnected", exc);
            }

        }

        public void Send(Socket client, byte[] byteData)
        {
            client.BeginSend(byteData, 0, byteData.Length, 0,
                SendCallback, client);

            _sendDone.WaitOne();
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                _sendDone.Set();
            }
            catch (Exception e)
            {
                OnClientError();
            }
        }
        public virtual void AcceptClientCheck(Socket client)
        {

        }
        public virtual void DataAnalyze(Socket client, _MSG_HEADER header, byte[] data)
        {

        }
        public void EnCryptString(byte[] pStr, int nSize, byte byPlus, byte wCryptKey)
        {

            for (int i = 0; i < nSize; i++)
            {
                pStr.SetValue((byte)(((byte)pStr.GetValue(i) + byPlus + 1) ^ (wCryptKey + 3)), i);
            }
        }


        protected virtual void OnClientConnected()
        {
            EventHandler handler = OnConnected;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }
        protected virtual void OnClientError()
        {
            EventHandler handler = OnError;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }
    }
}
