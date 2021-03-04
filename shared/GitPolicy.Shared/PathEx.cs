using System;
using System.IO;

namespace Mjcheetham.Git.Policy
{
    public static class PathEx
    {
        public static string GetHomeRelativePath(string path, bool useTilde)
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string relativePath = Path.GetRelativePath(homeDir, path);
            if (useTilde)
            {
                return Path.Combine("~", relativePath);
            }

            return relativePath;
        }
    }
}
