using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Mjcheetham.Git.Policy
{
    public interface IFileSystem
    {
        bool DirectoryExists(string path);
        void CreateDirectory(string path);
        void DeleteDirectory(string path, bool recurse);
        void WriteAllText(string path, string contents);
        bool FileExists(string path);
        bool TryLocateExecutable(string name, out string? path);
    }

    public class FileSystem : IFileSystem
    {
        public bool DirectoryExists(string path) => Directory.Exists(path);
        public void CreateDirectory(string path) => Directory.CreateDirectory(path);
        public void DeleteDirectory(string path, bool recurse) => Directory.Delete(path, recurse);
        public void WriteAllText(string path, string contents) => File.WriteAllText(path, contents);
        public bool FileExists(string path) => File.Exists(path);

        public bool TryLocateExecutable(string program, out string? path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (Environment.GetEnvironmentVariable("PATH") is { } pathValue)
                {
                    string[] paths = pathValue.Split(';');
                    foreach (var basePath in paths)
                    {
                        string candidatePath = Path.Combine(basePath, program);
                        if (FileExists(candidatePath))
                        {
                            path = candidatePath;
                            return true;
                        }
                    }
                }
            }
            else
            {
                using var which = ProcessHelper.CreateProcess("/usr/bin/which", program);
                which.Start();
                which.WaitForExit();

                switch (which.ExitCode)
                {
                    case 0: // found
                        string stdout = which.StandardOutput.ReadToEnd();
                        string[] results = stdout.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
                        path = results.First();
                        return true;

                    case 1: // not found
                        break;

                    default:
                        throw new Exception(
                            $"Unknown error locating '{program}' using /usr/bin/which. Exit code: {which.ExitCode}.");
                }
            }

            path = null;
            return false;
        }
    }
}
