using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Mjcheetham.Git.Policy.Cli.Controllers;

namespace Mjcheetham.Git.Policy.Cli
{
    public static class Program
    {
        private const string MacDaemonName = "com.mjcheetham.gitpolicy.daemon";
        private const string WindowsServiceName = "GitPolicy";

        public static async Task Main(string[] args)
        {
            IFileSystem fs = new FileSystem();
            IDaemonController controller = CreateController();

            if (!Git.TryDiscover(fs, out string? gitPath))
            {
                await Console.Error.WriteLineAsync("fatal: unable to locate git executable");
                Environment.ExitCode = -1;
                return;
            }

            IGit git = new Git(gitPath!);

            var initCommand = new InitCommand(git);
            var syncCommand = new SyncCommand(fs, git);
            var ignoreCommand = new IgnoreCommand(git);

            var startCommand = new Command("start", "Start the Git Policy daemon.")
            {
                Handler = CommandHandler.Create(() => controller.StartDaemon())
            };

            var stopCommand = new Command("stop", "Stop the Git Policy daemon.")
            {
                Handler = CommandHandler.Create(() => controller.StopDaemon())
            };

            var restartCommand = new Command("restart", "Restart the Git Policy daemon.")
            {
                Handler = CommandHandler.Create(() => controller.RestartDaemon())
            };

            var rootCommand = new RootCommand
            {
                initCommand,
                syncCommand,
                ignoreCommand,
                startCommand,
                stopCommand,
                restartCommand,
            };

            Parser cmdLineParser = new CommandLineBuilder(rootCommand)
                .UseDefaults()
                .UseExceptionHandler(OnException)
                .Build();

            int exitCode = await cmdLineParser.InvokeAsync(args);

            Environment.Exit(exitCode);
        }

        private static IDaemonController CreateController()
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                string plistPath = $"{homeDir}/Library/LaunchAgents/{MacDaemonName}.plist";
                return new MacDaemonController(MacDaemonName, plistPath, isSystem: false);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new WindowsServiceController(WindowsServiceName);
            }

            throw new PlatformNotSupportedException();
        }

        private static void OnException(Exception ex, InvocationContext context)
        {
            IStandardStreamWriter err = context.Console.Error;

            switch (ex)
            {
                case AggregateException aex:
                    aex.Handle(x =>
                    {
                        err.WriteLine($"fatal: {x.Message}");
                        return true;
                    });
                    break;

                case Win32Exception wex:
                    err.WriteLine($"fatal: {wex.Message} [0x{wex.NativeErrorCode:x}]");
                    break;

                default:
                    err.WriteLine($"fatal: {ex.Message}");
                    break;
            }
        }
    }
}
