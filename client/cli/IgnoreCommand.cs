using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;

namespace Mjcheetham.Git.Policy.Cli
{
    public class IgnoreCommand : Command
    {
        private readonly IGit _git;

        public IgnoreCommand(IGit git) : base("ignore", "Opt-out of policies.")
        {
            _git = git;

            AddArgument(new Argument("id")
            {
                Arity = ArgumentArity.OneOrMore,
                Description = "Policy IDs to ignore."
            });

            Handler = CommandHandler.Create<string[]>(ExecuteAsync);
        }

        private int ExecuteAsync(string[] id)
        {
            HashSet<string> newIds = id.ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Ignore existing ignored policies
            HashSet<string> existingIds = _git.GetAllConfig("policy.ignore", GitConfigurationScope.Global)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            newIds.ExceptWith(existingIds);

            int exitCode = 0;
            foreach (string policyId in newIds)
            {
                try
                {
                    _git.AddConfig("policy.ignore", policyId, GitConfigurationScope.Global);
                }
                catch (GitException ex)
                {
                    Console.Error.WriteLine("error: failed to ignore policy '{0}': {1} (exit={2})",
                        policyId, ex.Message, ex.ExitCode);

                    exitCode = -1;
                }
            }

            return exitCode;
        }
    }
}
