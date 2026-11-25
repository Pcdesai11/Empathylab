#nullable enable
using System;
using System.IO;

namespace EmpathyLab.Tests.EditMode
{
    /// <summary>
    /// Finds the shipped scenario folder by walking up from the working directory.
    /// The same walk works for Unity EditMode and for <c>dotnet test</c>.
    /// </summary>
    static class ScenarioTestFiles
    {
        public static string DirectoryPath()
        {
            foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                var found = Walk(start);
                if (found != null)
                    return found;
            }

            throw new DirectoryNotFoundException("Could not find Assets/StreamingAssets/Scenarios.");
        }

        static string? Walk(string? start)
        {
            if (string.IsNullOrEmpty(start))
                return null;

            var dir = new DirectoryInfo(start);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "Assets", "StreamingAssets", "Scenarios");
                if (Directory.Exists(candidate))
                    return candidate;
                dir = dir.Parent;
            }

            return null;
        }
    }
}
