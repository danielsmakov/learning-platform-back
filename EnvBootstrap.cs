using DotNetEnv;

namespace LearningPlatform;

internal static class EnvBootstrap
{
    /// <summary>
    /// Loads the first existing .env file from common locations (dotnet run cwd or output bin path).
    /// </summary>
    internal static void LoadLocalEnvFile()
    {
        foreach (var path in GetEnvFileCandidates())
        {
            if (!File.Exists(path)) continue;
            Env.Load(path);
            return;
        }
    }

    private static IEnumerable<string> GetEnvFileCandidates()
    {
        yield return Path.Combine(Directory.GetCurrentDirectory(), ".env");
        var fromBin = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env"));
        yield return fromBin;
    }
}
