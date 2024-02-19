using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using static Shared.Filter;
using WinForms = System.Windows.Forms;

namespace ModelessForms
{
    class AsyncExecuteCommand : IAsyncCommand
    {
        Func<UIApplication, Result> Method;
        private AsyncExecuteCommand() { }
        public AsyncExecuteCommand(Func<UIApplication, Result> method)
        {
            Method = method;
        }

        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public void Execute(UIApplication uiApp)
        {
            #region LoggerSetup
            //Nlog configuration
            var nlogConfig = new NLog.Config.LoggingConfiguration();
            //Targets
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "X:\\GitHub\\log.txt", DeleteOldFileOnStartup = true };
            //Rules
            nlogConfig.AddRule(LogLevel.Info, LogLevel.Fatal, logfile);
            //Apply config
            NLog.LogManager.Configuration = nlogConfig;
            //DISABLE LOGGING
            NLog.LogManager.DisableLogging();
            #endregion

            Method.Invoke(uiApp);
        }
    }
}
