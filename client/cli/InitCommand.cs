using System;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Mjcheetham.Git.Policy.Cli
{
    public class InitCommand : Command
    {
        private readonly IGit _git;

        public InitCommand(IGit git) : base("init", "Connect to a Git Policy authority.")
        {
            _git = git;

            AddArgument(new Argument("url")
            {
                Arity = ArgumentArity.ExactlyOne,
                Description = "Git Policy authority URL."
            });

            Handler = CommandHandler.Create<string>(Execute);
        }

        private int Execute(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                Console.Error.WriteLine("error: URL is invalid");
                return -1;
            }

            try
            {
                _git.SetConfig("policy.url", url, GitConfigurationScope.Global);
                return 0;
            }
            catch (GitException ex)
            {
                Console.Error.WriteLine("error: failed to initialize git-policy: {0} (exit={1})",
                    ex.Message, ex.ExitCode);
                return -1;
            }
        }
    }
}
