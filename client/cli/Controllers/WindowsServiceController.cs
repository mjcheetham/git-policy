using System;
using System.Diagnostics;
using Mjcheetham.Git.Policy.Cli.Controllers;

namespace Mjcheetham.Git.Policy.Cli
{
    public class WindowsServiceController : DaemonController
    {
        private readonly string _serviceName;

        public WindowsServiceController(string serviceName)
        {
            _serviceName = serviceName;
        }

        protected override int GetProcessId()
        {
            // TODO: get PID of running service
            return -1;
        }

        protected override int StartDaemonCore()
        {
            // Reload the daemon
            using Process startProc = ProcessHelper.CreateProcess("net", $"start {_serviceName}");
            startProc.Start();
            startProc.WaitForExit();
            if (startProc.ExitCode != 0)
            {
                throw new Exception($"Failed to start the service ({startProc.ExitCode})");
            }

            // TODO: return PID
            return 0;
        }

        protected override void StopDaemonCore()
        {
            // Reload the daemon
            using Process startProc = ProcessHelper.CreateProcess("net", $"stop {_serviceName}");
            startProc.Start();
            startProc.WaitForExit();
            if (startProc.ExitCode != 0)
            {
                throw new Exception($"Failed to stop the service ({startProc.ExitCode})");
            }
        }
    }
}
