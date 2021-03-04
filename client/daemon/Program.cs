using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mjcheetham.Git.Policy.Daemon
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseConsoleLifetime()
                .UseWindowsService()
                .UseSystemd()
                .ConfigureLogging(builder => builder.AddSimpleConsole(x => x.TimestampFormat = "O"))
                .ConfigureServices((context, services) =>
                {
                    var fs = new FileSystem();

                    if (!Git.TryDiscover(fs, out string? gitPath))
                    {
                        throw new Exception("Unable to locate git executable");
                    }

                    var git = new Git(gitPath!);

                    services.AddSingleton<IFileSystem>(fs);
                    services.AddSingleton<IGit>(git);
                    services.AddHostedService<Worker>();
                });
    }
}
