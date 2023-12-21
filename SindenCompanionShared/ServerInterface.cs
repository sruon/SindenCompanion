using System;
using System.Collections.Generic;
using NetMQ;
using NetMQ.Sockets;
using Serilog;

namespace SindenCompanionShared
{
    public delegate void RecvCallback(List<string> messages);

    /// <summary>
    /// Provides an interface for communicating from the client (target) to the server (injector)
    /// </summary>
    public class ServerInterface : IDisposable
    {
        private readonly RecvCallback _callback;
        private readonly NetMQPoller _poller;
        private readonly PullSocket _puller;
        private readonly PushSocket _pusher;
        private ILogger _logger;

        public ServerInterface(bool isServer, int ipcPort, ILogger logger, RecvCallback callback)
        {
            _logger = logger;
            if (isServer)
            {
                if (_logger != null)
                    _logger.Information(
                        "Starting communication interface for server (Push: {@Pusher}) (Pull: {@Puller})", ipcPort,
                        ipcPort + 1);
                _pusher = new PushSocket($"@tcp://*:{ipcPort}");
                _puller = new PullSocket($"@tcp://localhost:{ipcPort + 1}");
            }
            else
            {
                if (_logger != null)
                    _logger.Information("Starting communication interface for client {@Pusher} {@Puller}", ipcPort + 1,
                        ipcPort);
                _pusher = new PushSocket($">tcp://localhost:{ipcPort + 1}");
                _puller = new PullSocket($">tcp://localhost:{ipcPort}");
            }

            _poller = new NetMQPoller { _puller };
            _callback = callback;
            _puller.ReceiveReady += ReceiveMessages;
            _poller.RunAsync();
        }

        public void Dispose()
        {
            _poller.StopAsync();
        }

        public void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        public void SendMessage(string msg)
        {
            if (_logger != null) _logger.Debug("Sending message {@Message}", msg);
            _pusher.SendFrame(msg);
        }

        public void ReceiveMessages(object sender, NetMQSocketEventArgs e)
        {
            var ret = new List<string>();
            var msg = string.Empty;
            var more = true;
            while (more)
            {
                var received = e.Socket.TryReceiveFrameString(out msg, out more);
                if (received)
                {
                    if (_logger != null) _logger.Debug("Received message {@Message}", msg);
                    ret.Add(msg);
                }
            }

            if (_logger != null && ret.Count > 0) _logger.Debug("Received {@Count} messages", ret.Count);
            if (_callback != null) _callback(ret);
        }
    }
}