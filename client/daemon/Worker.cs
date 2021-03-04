using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mjcheetham.Git.Policy.Daemon
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IGit _git;

        public Worker(ILogger<Worker> logger, IGit git)
        {
            _logger = logger;
            _git = git;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker starting...");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker stopping...");
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            TimeSpan interval;
            string? intervalStr = _git.GetConfig("policy.interval");
            if (!string.IsNullOrWhiteSpace(intervalStr) && int.TryParse(intervalStr, out int intervalSec))
            {
                interval = TimeSpan.FromSeconds(intervalSec);
            }
            else
            {
                interval = TimeSpan.FromMinutes(30);
            }

            _logger.LogInformation("Using polling interval of {0} seconds", interval.TotalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Syncing policies...");

                using Process proc = _git.CreateProcess("policy sync");
                proc.Start();
                proc.WaitForExit();

                if (proc.ExitCode != 0)
                {
                    _logger.LogError("Failed to sync policies (exit: {0})", proc.ExitCode);
                    _logger.LogError("stdout: {0}", proc.StandardOutput.ReadToEnd());
                    _logger.LogError("stderr: {0}", proc.StandardError.ReadToEnd());
                }
                else
                {
                    _logger.LogInformation("Policies synced OK");
                }

                try
                {
                    await Task.Delay(interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }
    }
}
