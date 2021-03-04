using System.Diagnostics;

namespace Mjcheetham.Git.Policy
{
    public static class ProcessHelper
    {
        public static Process CreateProcess(string program, string args)
        {
            var psi = new ProcessStartInfo(program, args)
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            return new Process {StartInfo = psi};
        }
    }
}
