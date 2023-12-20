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

        public static ILogger CreateLogger(bool toFile, bool debug)
        {
            var config = new LoggerConfiguration().WriteTo.Console();

            if (toFile) config = config.WriteTo.File("sc.log");

            if (debug) config = config.MinimumLevel.Debug();

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
            var message = logEvent.RenderMessage(_formatProvider);
            var ev = MessageBuilder.Build("log", message);
            _server.SendMessage(ev.AsMessage());
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