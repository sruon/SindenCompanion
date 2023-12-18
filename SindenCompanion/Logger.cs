using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SindenCompanionShared
{
    public partial class Logger
    {
        public static ILogger CreateDesktopLogger(bool debug, System.Windows.Controls.RichTextBox rtb)
        {
            LoggerConfiguration config = new LoggerConfiguration().WriteTo.RichTextBox(rtb);

            if (debug)
            {
                config = config.MinimumLevel.Debug();
            }

            Log.Logger = config.CreateLogger();

            return Log.Logger;
        }
    }
}
