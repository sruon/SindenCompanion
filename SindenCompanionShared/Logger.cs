using System;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace SindenCompanionShared
{
    public class Logger
    {
        public static ILogger CreateRemoteLogger(ServerInterface s)
        {
            var config = new LoggerConfiguration().WriteTo.RemoteSink(s).MinimumLevel.Information();
            Log.Logger = config.CreateLogger();

            return Log.Logger;
        }
    }

    public class RemoteSink : ILogEventSink
    {
        private readonly IFormatProvider _formatProvider;
        private readonly ServerInterface _server;

        public RemoteSink(IFormatProvider formatProvider, ServerInterface s)
        {
            _server = s;
            _formatProvider = formatProvider;
        }

        public void Emit(LogEvent logEvent)
        {
            _server.BuildAndSendMessage("log", logEvent.RenderMessage(_formatProvider));
        }
    }

    public static class RemoteSinkExtensions
    {
        public static LoggerConfiguration RemoteSink(
            this LoggerSinkConfiguration loggerConfiguration,
            ServerInterface s, IFormatProvider formatProvider = null)
        {
            return loggerConfiguration.Sink(new RemoteSink(formatProvider, s));
        }
    }
}