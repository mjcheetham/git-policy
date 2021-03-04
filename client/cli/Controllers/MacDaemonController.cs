using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Mjcheetham.Git.Policy.Cli.Controllers;

namespace Mjcheetham.Git.Policy.Cli
{
    public class MacDaemonController : DaemonController
    {
        private readonly string _name;
        private readonly string _plistPath;
        private readonly bool _isSystem;

        public MacDaemonController(string name, string plistPath, bool isSystem)
        {
            _name = name;
            _plistPath = plistPath;
            _isSystem = isSystem;
        }

        protected override int StartDaemonCore()
        {
            // Try to unload first (in case the config has changed)
            using Process unloadProc = ProcessHelper.CreateProcess("launchctl", $"unload -F {_plistPath}");
            unloadProc.Start();
            unloadProc.WaitForExit();

            // Reload the daemon
            using Process loadProc = ProcessHelper.CreateProcess("launchctl", $"load -F {_plistPath}");
            loadProc.Start();
            loadProc.WaitForExit();
            if (loadProc.ExitCode != 0)
            {
                throw new Exception($"Failed to load the launch agent plist ({loadProc.ExitCode})");
            }

            // Kick-start the daemon (and get the PID out of it)
            string serviceTarget = GetServiceTarget();
            using Process kickProc = ProcessHelper.CreateProcess("launchctl", $"kickstart -p {serviceTarget}");
            kickProc.Start();
            kickProc.WaitForExit();

            if (kickProc.ExitCode == 0)
            {
                string pid = kickProc.StandardOutput.ReadToEnd().TrimEnd();
                return int.Parse(pid);
            }

            throw new Exception($"Failed to kickstart daemon ({kickProc.ExitCode})");
        }

        protected override void StopDaemonCore()
        {
            using Process proc = ProcessHelper.CreateProcess("launchctl", $"stop {_name}");
            proc.Start();
            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                throw new Exception($"Failed to stop daemon ({proc.ExitCode})");
            }
        }

        protected override int GetProcessId()
        {
            string serviceTarget = GetServiceTarget();
            using Process proc = ProcessHelper.CreateProcess("launchctl", $"print {serviceTarget}");
            proc.Start();
            proc.WaitForExit();

            if (proc.ExitCode == 0)
            {
                string output = proc.StandardOutput.ReadToEnd();
                var match = Regex.Match(output, @"\s*pid = (?'pid'\d+)");
                if (match.Success && int.TryParse(match.Groups["pid"].Value, out int pid))
                {
                    return pid;
                }
            }

            return -1;
        }

        private string GetServiceTarget()
        {
            if (_isSystem)
            {
                return $"system/{_name}";
            }

            return $"gui/{getuid()}/{_name}";
        }

        [DllImport("libc", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint getuid();
    }
}
