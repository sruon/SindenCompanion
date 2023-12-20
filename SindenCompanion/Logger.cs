using System.Windows.Controls;
using Serilog;

namespace SindenCompanionShared
{
    public partial class Logger
    {
        public static ILogger CreateDesktopLogger(bool debug, RichTextBox rtb)
        {
            var config = new LoggerConfiguration().WriteTo.RichTextBox(rtb);

            if (debug) config = config.MinimumLevel.Debug();

            Log.Logger = config.CreateLogger();

            return Log.Logger;
        }
    }
}