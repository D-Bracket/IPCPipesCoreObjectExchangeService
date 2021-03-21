using DBracket.IPC.Pipes.Core;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace DBracket.IPC.Pipes.Core.ObjectExchangeService
{
    /// <summary>
    /// Exchanges objects with an ObjectExchangeHandler in another program via pipes. The ExchangeHandlers need to have the same pipename.
    /// Since this handler was meant to transfer data from one app to another, there can only be two handler on the same pipename.
    /// </summary>
    public class ObjectExchangeHandler
    {
        #region Private Fields
        /// <summary>
        /// Object whose data should be exchanged, with the connected app
        /// </summary>
        private IExchangeObject _objectToExchange;

        /// <summary>
        /// Clientpipe, used if the ExchangeHandler is set to PointB
        /// </summary>
        private ClientPipe _clientPipe;

        /// <summary>
        /// Serverpipe, used if the ExchangeHandler is set to PointA
        /// </summary>
        private ServerPipe _serverPipe;

        /// <summary>
        /// Determines if the data exchange has been startet
        /// </summary>
        private bool _bStarted;

        /// <summary>
        /// Contains the serialized data of the object
        /// </summary>
        private string _responseJson;

        /// <summary>
        /// Type of the object to exchange
        /// </summary>
        private Type _ipcObjectType;

        /// <summary>
        /// Determines if the exchangehandler acts as Server or Client
        /// </summary>
        Points _appChannel = Points.NotSet;

        /// <summary>
        /// Name of the used pipe
        /// </summary>
        private string _pipeName;

        /// <summary>
        /// Timer to automatically reconnect as sone as the connection is lost
        /// </summary>
        private Timer _reconnectTmr;
        #endregion



        #region Constructor
        /// <summary>
        /// Exchanges objects with an ObjectExchangeHandler in another program via pipes. The ExchangeHandlers need to have the same pipename
        /// Since this handler was meant to transfer data from one app to another, there can only be two handler on the same pipename
        /// </summary>
        /// <param name="objectToExchange">Object whose data should be exchanged, with the connected app</param>
        public ObjectExchangeHandler(IExchangeObject objectToExchange)
        {
            _objectToExchange = objectToExchange;
            _objectToExchange.ObjectChanged += Update;
            _ipcObjectType = _objectToExchange.GetType();
        }
        #endregion



        #region Methods
        #region Public Methods
        /// <summary>
        /// Starts the data exchange. Once started, the handler automatically tries to connect to another handler with the same pipe name
        /// </summary>
        /// <param name="pipename">Name of the pipe, on which the handler should try and connect. Use the same name on the other app</param>
        /// <param name="point">This is used to differentiate the to apps. Set PointA in one app and PointB in the other app</param>
        public void Start(string pipename, Points point)
        {
            if (!_bStarted)
            {
                if (point == Points.PointA)
                {
                    CreateServer(pipename);
                    _appChannel = Points.PointA;
                }
                else
                {
                    CreateClient(pipename);
                    _appChannel = Points.PointB;
                }
            }

            _pipeName = pipename;
            _bStarted = true;
        }

        /// <summary>
        /// Stops the data exchange
        /// </summary>
        public void Stop()
        {
            _clientPipe.Close();
            _bStarted = false;
        }
        #endregion


        #region Private Methods
        /// <summary>
        /// Creates a new server instance
        /// </summary>
        /// <param name="Pipename">Name of the pipe, on which the handler should try and connect</param>
        private void CreateServer(string Pipename)
        {
            _serverPipe = new ServerPipe(Pipename, p => p.StartMessageReaderAsync());

            _serverPipe.DataReceived += (sndr, args) =>
            {
                ServerPipe sender = sndr as ServerPipe;
                OnServerMessageReceived(sender, args.Data);
            };

            _serverPipe.Connected += (sndr, args) =>
            {
                ConnectionStateChange?.Invoke(true);
            };

            _serverPipe.Disconnect += (sndr, args) =>
            {
                ConnectionStateChange?.Invoke(false);
                CreateServer(_pipeName);
            };
        }

        /// <summary>
        /// Creates a new client instance
        /// </summary>
        /// <param name="pipename">Name of the pipe, on which the handler should try and connect</param>
        private void CreateClient(string pipename)
        {
            _clientPipe = new ClientPipe(".", pipename, p => p.StartMessageReaderAsync());

            _clientPipe.DataReceived += (sndr, args) =>
            {
                ClientPipe sender = sndr as ClientPipe;
                OnClientMessageReceived(sender, args.Data);
            };

            _clientPipe.Disconnect += ResetClient;

            if (!_clientPipe.Connect(1000))
            {
                _reconnectTmr = new Timer(TryToReconnect, null, 0, 2000);
            }
            else
            {
                ConnectionStateChange?.Invoke(true);
            }
        }
        #endregion


        #region Events
        /// <summary>
        /// Server recieved new object data from the other point
        /// </summary>
        /// <param name="serverPipe">Pipe of the Server</param>
        /// <param name="bytes">Recieved data</param>
        private void OnServerMessageReceived(ServerPipe serverPipe, byte[] bytes)
        {
            _responseJson = Encoding.ASCII.GetString(bytes);
            _objectToExchange.ObjectChanged -= Update;

            var tmp = (IExchangeObject)JsonConvert.DeserializeObject(_responseJson, _ipcObjectType);

            var props = _ipcObjectType.GetProperties();

            foreach (PropertyInfo prop in props)
            {
                object newValue = tmp.GetType().GetProperty(prop.Name).GetValue(tmp);
                _objectToExchange.GetType().GetProperty(prop.Name).SetValue(_objectToExchange, newValue);
            }

            _objectToExchange.ObjectChanged += Update;
            ObjectChanged?.Invoke(_objectToExchange);
        }

        /// <summary>
        /// Client recieved new object data from the other point
        /// </summary>
        /// <param name="clientPipe">Pipe of the client</param>
        /// <param name="bytes">Recieved data</param>
        private void OnClientMessageReceived(ClientPipe clientPipe, byte[] bytes)
        {
            _responseJson = Encoding.ASCII.GetString(bytes);
            _objectToExchange.ObjectChanged -= Update;

            var tmp = (IExchangeObject)JsonConvert.DeserializeObject(_responseJson, _ipcObjectType);

            var props = _ipcObjectType.GetProperties();

            foreach (PropertyInfo prop in props)
            {
                object newValue = tmp.GetType().GetProperty(prop.Name).GetValue(tmp);
                _objectToExchange.GetType().GetProperty(prop.Name).SetValue(_objectToExchange, newValue);
            }

            _objectToExchange.ObjectChanged += Update;
            ObjectChanged?.Invoke(_objectToExchange);
        }

        /// <summary>
        /// Is called when the object was changed from this point. Sends the object to the other point
        /// </summary>
        private void Update()
        {
            var responseJson = JsonConvert.SerializeObject(_objectToExchange);
            byte[] responseBytes = Encoding.ASCII.GetBytes(responseJson);
            try
            {
                if (_appChannel == Points.PointA)
                {
                    _serverPipe.WriteBytes(responseBytes, false);
                }
                else
                {
                    _clientPipe.WriteBytes(responseBytes, false);
                }
            }
            catch (IOException) //Catch this exception to ensure disconnect
            {
            }
        }

        /// <summary>
        /// Is called when the client loses the connection. Starts the reconnection process to the server
        /// </summary>
        /// <param name="sender">-</param>
        /// <param name="e">-</param>
        private void ResetClient(object sender, EventArgs e)
        {
            ConnectionStateChange?.Invoke(false);
            if (!_clientPipe.Connect(1000))
            {
                _reconnectTmr = new Timer(TryToReconnect, null, 0, 2000);
            }
        }

        /// <summary>
        /// Tries to reconnect to the server (only PointB)
        /// </summary>
        /// <param name="state">-</param>
        private void TryToReconnect(object state)
        {
            if (_clientPipe.Connect(1000))
            {
                _reconnectTmr.Dispose();
            }
        }
        #endregion
        #endregion



        #region Public Properties
        #region Properties

        #endregion

        #region Events
        /// <summary>
        /// Is thrown, when the objects data where changed from the other point
        /// </summary>
        public event ObjectChangedHandler ObjectChanged;
        public delegate void ObjectChangedHandler(IExchangeObject exchangedObject);

        /// <summary>
        /// Is thrown, when the connectionstate to the other point changes
        /// </summary>
        public event ConnectionChangedHandler ConnectionStateChange;
        public delegate void ConnectionChangedHandler(bool isConnected);
        #endregion
        #endregion
    }

    /// <summary>
    /// ICP Points to connect to
    /// </summary>
    public enum Points
    {
        NotSet = 0,
        PointA = 1,
        PointB = 2
    }
}