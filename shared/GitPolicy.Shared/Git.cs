using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Mjcheetham.Git.Policy
{
    public interface IGit
    {
        Process CreateProcess(string args);

        string GetVersion();

        void AddConfig(string key, string value, GitConfigurationScope scope);

        void AddConfigInFile(string filePath, string key, string value);

        void SetConfig(string key, string value, GitConfigurationScope scope);

        void SetConfigInFile(string key, string? value, string filePath);

        string? GetConfig(string key, GitConfigurationScope? scope = null);

        IEnumerable<string> GetAllConfig(string key, GitConfigurationScope? scope = null);
    }

    public enum GitConfigurationScope
    {
        Unknown = 0,
        Global = 1,
        System = 2,
    }

    public class Git : IGit
    {
        private readonly string _path;

        public Git(string path)
        {
            _path = path;
        }

        public static bool TryDiscover(IFileSystem fs, out string? path)
        {
            string execName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "git.exe" : "git";
            return fs.TryLocateExecutable(execName, out path);
        }

        public Process CreateProcess(string args) => ProcessHelper.CreateProcess(_path, args);

        public string GetVersion()
        {
            Process proc = CreateProcess("--version");
            proc.Start();

            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                throw new GitException("Failed to get Git version", proc.ExitCode);
            }

            return output.TrimEnd();
        }

        public void AddConfig(string key, string value, GitConfigurationScope scope)
        {
            var args = new StringBuilder("config");
            args.AppendFormat(" {0}", GetScopeOption(scope));
            args.AppendFormat(" --add {0} {1}", QuoteArgument(key), QuoteArgument(value));

            Process proc = this.CreateProcess(args);
            proc.Start();

            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                throw new GitException("Failed to add configuration", proc.ExitCode);
            }
        }

        public void AddConfigInFile(string filePath, string key, string value)
        {
            var args = new StringBuilder("config");
            args.AppendFormat(" -f {0}", QuoteArgument(filePath));
            args.AppendFormat(" --add {0} {1}", QuoteArgument(key), QuoteArgument(value));

            Process proc = this.CreateProcess(args);
            proc.Start();

            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                throw new GitException("Failed to add configuration", proc.ExitCode);
            }
        }

        public void SetConfig(string key, string? value, GitConfigurationScope scope)
        {
            var args = new StringBuilder("config");
            args.AppendFormat(" {0}", GetScopeOption(scope));
            args.AppendFormat(" {0} {1}", QuoteArgument(key), QuoteArgument(value));

            Process proc = this.CreateProcess(args);
            proc.Start();

            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                throw new GitException("Failed to set configuration", proc.ExitCode);
            }
        }

        public void SetConfigInFile(string key, string? value, string filePath)
        {
            var args = new StringBuilder("config");
            args.AppendFormat(" -f {0}", QuoteArgument(filePath));
            args.AppendFormat(" {0} {1}", QuoteArgument(key), QuoteArgument(value));

            Process proc = this.CreateProcess(args);
            proc.Start();

            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                throw new GitException("Failed to set configuration", proc.ExitCode);
            }
        }

        public string? GetConfig(string key, GitConfigurationScope? scope)
        {
            var args = new StringBuilder("config");
            if (scope.HasValue)
            {
                args.AppendFormat(" {0}", GetScopeOption(scope.Value));
            }
            args.AppendFormat(" -z {0}", QuoteArgument(key));

            using Process proc = this.CreateProcess(args);
            proc.Start();
            string data = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            switch (proc.ExitCode)
            {
                case 0: // OK
                    return data.TrimEnd('\0');

                case 1: // No results
                    return null;

                default:
                    throw new GitException($"Failed to get Git configuration entry '{key}'", proc.ExitCode);
            }
        }

        public IEnumerable<string> GetAllConfig(string key, GitConfigurationScope? scope)
        {
            var args = new StringBuilder("config");
            if (scope.HasValue)
            {
                args.AppendFormat(" {0}", GetScopeOption(scope.Value));
            }
            args.AppendFormat(" --get-all -z {0}", QuoteArgument(key));

            using Process proc = this.CreateProcess(args);
            proc.Start();
            string data = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            switch (proc.ExitCode)
            {
                case 0: // OK
                    string[] entries = data.Split('\0');

                    // Because each line terminates with the \0 character, splitting leaves us with one
                    // bogus blank entry at the end of the array which we should ignore
                    for (var i = 0; i < entries.Length - 1; i++)
                    {
                        yield return entries[i];
                    }
                    break;

                case 1: // No results
                    break;

                default:
                    throw new GitException($"Failed to get all Git configuration entries '{key}'", proc.ExitCode);
            }
        }

        public void UnsetAll(string key, string valueRegex, GitConfigurationScope scope)
        {
            var args = new StringBuilder("config");
            args.AppendFormat(" {0}", GetScopeOption(scope));
            args.AppendFormat(" --unset-all {0} {1}", QuoteArgument(key), QuoteArgument(valueRegex));

            Process proc = this.CreateProcess(args);
            proc.Start();

            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                throw new GitException("Failed to unset configuration", proc.ExitCode);
            }
        }

        private static string GetScopeOption(GitConfigurationScope scope)
        {
            return scope switch
            {
                GitConfigurationScope.Global => "--global",
                GitConfigurationScope.System => "--system",
                GitConfigurationScope.Unknown => throw new InvalidOperationException("Must specify valid scope"),
                _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, "Unknown scope")
            };
        }

        private static string QuoteArgument(string? str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return "\"\"";
            }

            char[] needsQuoteChars = {'"', ' ', '\\', '\n', '\r', '\t'};
            bool needsQuotes = str.Any(x => needsQuoteChars.Contains(x));

            if (!needsQuotes)
            {
                return str;
            }

            // Replace all '\' characters with an escaped '\\', and all '"' with '\"'
            string escapedStr = str.Replace("\\", "\\\\").Replace("\"", "\\\"");

            // Bookend the escaped string with double-quotes '"'
            return $"\"{escapedStr}\"";
        }
    }

    public static class GitExtensions
    {
        public static Process CreateProcess(this IGit git, StringBuilder args) => git.CreateProcess(args.ToString());
    }

    public class GitException : Exception
    {
        public GitException(string message, int exitCode) : base(message)
        {
            ExitCode = exitCode;
        }

        public int ExitCode { get; }
    }
}
