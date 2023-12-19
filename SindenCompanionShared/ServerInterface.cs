using System;
using NetMQ;
using NetMQ.Sockets;
using System.Collections.Generic;
using Serilog;


namespace SindenCompanionShared
{
    public delegate void RecvCallback(List<string> messages);

    /// <summary>
    /// Provides an interface for communicating from the client (target) to the server (injector)
    /// </summary>
    public class ServerInterface : IDisposable
    {
        PushSocket pusher;
        PullSocket puller;
        NetMQPoller poller;
        RecvCallback _callback;
        private ILogger _logger;
        public ServerInterface(bool isServer, int IpcPort, ILogger logger, RecvCallback callback) {
            _logger = logger;
            if (isServer)
            {
                if (_logger != null)
                {
                    _logger.Information("Starting communication interface for server (Push: {@Pusher}) (Pull: {@Puller})", IpcPort, IpcPort + 1);
                }
                pusher = new PushSocket($"@tcp://*:{IpcPort}");
                puller = new PullSocket($"@tcp://localhost:{IpcPort + 1}");
            } else
            {
                if (_logger != null)
                {
                    _logger.Information("Starting communication interface for client {@Pusher} {@Puller}", IpcPort + 1, IpcPort);
                }
                pusher = new PushSocket($">tcp://localhost:{IpcPort + 1}");
                puller = new PullSocket($">tcp://localhost:{IpcPort}");
            }
            poller = new NetMQPoller { puller };
            _callback = callback;
            puller.ReceiveReady += this.ReceiveMessages;
            poller.RunAsync();
        }

        public void Dispose()
        {
            poller.StopAsync();
        }
        public void SetLogger( ILogger logger )
        {
            _logger = logger;
        }
        public void SendMessage(string msg)
        {
            if (_logger != null)
            {
                _logger.Debug("Sending message {@Message}", msg);
            }
            pusher.SendFrame(msg);
        }

        public void ReceiveMessages(object sender, NetMQSocketEventArgs e)
        {
            List<string> ret = new List<string>();
            string msg = string.Empty;
            bool more = true;
            while (more)
            {
                bool received = e.Socket.TryReceiveFrameString(out msg, out more);
                if (received)
                {
                    if (_logger != null)
                    {
                        _logger.Debug("Received message {@Message}", msg);
                    }
                    ret.Add(msg);
                }
            }
            if (_logger != null && ret.Count > 0)
            {
                _logger.Debug("Received {@Count} messages", ret.Count);
            }
            if (_callback != null)
            {
                _callback(ret);
            }
        }
    }
}
