using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KinectHelloWorld.SupportClasses {
    public delegate void ResponseCallback(string response);

    class StateObject {
        // Client  socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }

    class MasterKiwiSocket {
        public const string EOF = "<EOF>";
        ResponseCallback callback;
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        public bool isActive = true;
        Socket socket;
        int port;

        public MasterKiwiSocket(ResponseCallback callback) {
            Configuration confg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if( confg.AppSettings.Settings["Port"] == null || !int.TryParse(confg.AppSettings.Settings["Port"].Value, out port) ) {
                port = 11000;
                Console.WriteLine(string.Format("Invalid port. Using and saving to default port: {0}", port));
                confg.AppSettings.Settings.Add(new KeyValueConfigurationElement("Port", port.ToString()));
                confg.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            allDone = new ManualResetEvent(false);
            this.callback = callback;
        }

        public void StartListening() {
            IPHostEntry ipHostInfo = Dns.GetHostEntry("localhost");
            IPAddress ipAddress = ( from address in ipHostInfo.AddressList
                                    where address.AddressFamily == AddressFamily.InterNetwork
                                    select address ).ToArray()[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);
            try {
                socket.Bind(localEndPoint);
                socket.Listen(10);
                while( isActive ) {
                    // Set the event to nonsignaled state.
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.
                    Console.WriteLine("Waiting for a connection...");
                    socket.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        socket);

                    // Wait until a connection is made before continuing.
                    allDone.WaitOne();
                }
            }
            catch( Exception e ) {
                Console.WriteLine(e.ToString());
            }
            finally {
                socket.Close();
            }
        }

        public void AcceptCallback(IAsyncResult ar) {
            allDone.Set();
            Socket listener = (Socket) ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            // Create the state object.

            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        public void ReadCallback(IAsyncResult ar) {
            string content = string.Empty;

            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateObject state = (StateObject) ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket. 
            int bytesRead = handler.EndReceive(ar);

            if( bytesRead > 0 ) {
                // There  might be more data, so store the data received so far.
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read 
                // more data.
                content = state.sb.ToString();
                if( content.IndexOf(EOF) > -1 ) {
                    // All the data has been read from the 
                    // client. Display it on the console.
                    Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                        content.Length, content);
                    this.callback(content.Substring(0, content.IndexOf(EOF)));
                }
                else {
                    // Not all data received. Get more.
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
                }
            }
        }

    }
}
