using System;

namespace Mjcheetham.Git.Policy.Cli.Controllers
{
    public interface IDaemonController
    {
        int StartDaemon();

        int StopDaemon();

        int RestartDaemon();
    }

    public abstract class DaemonController : IDaemonController
    {
        public int StartDaemon()
        {
            // Check if we already have a running instance
            int pid = GetProcessId();
            if (pid > -1)
            {
                Console.Error.WriteLine($"Daemon already running with PID: {pid}");
                return 0;
            }

            try
            {
                int newPid = StartDaemonCore();
                Console.Error.WriteLine($"Daemon started with PID: {newPid}");
            }
            catch (Exception)
            {
                Console.Error.WriteLine("error: failed to start daemon");
                return -1;
            }

            return 0;
        }

        public int StopDaemon()
        {
            // Check if we already have a running instance
            int pid = GetProcessId();
            if (pid < 0)
            {
                Console.Error.WriteLine("No daemon running.");
                return 0;
            }

            try
            {
                StopDaemonCore();
            }
            catch (Exception)
            {
                Console.Error.WriteLine("error: failed to stop daemon");
                return -1;
            }

            Console.Error.WriteLine("Daemon stopped.");
            return 0;
        }

        public int RestartDaemon()
        {
            int result = StopDaemon();
            if (result != 0)
            {
                return result;
            }

            return StartDaemon();
        }

        protected abstract int GetProcessId();

        protected abstract int StartDaemonCore();

        protected abstract void StopDaemonCore();
    }
}
