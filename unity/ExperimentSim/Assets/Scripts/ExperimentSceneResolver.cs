public class ExperimentSceneResolver
{
    public static string ResolveSceneName(int experimentId, string experimentName)
    {
        string name = (experimentName ?? "").Trim().ToLowerInvariant();

        if (name.Contains("güneş sistemi") ||
            name.Contains("gunes sistemi") ||
            name.Contains("solar system") ||
            name.Contains("gezegen") ||
            name.Contains("gözlem") ||
            name.Contains("gozlem"))
        {
            return "SolarSystemScene";
        }

        if (name.Contains("güneş") ||
            name.Contains("gunes") ||
            name.Contains("ay tutulması") ||
            name.Contains("ay tutulmasi") ||
            name.Contains("tutulma"))
        {
            return "SolarEclipseScene";
        }

        return "";
    }
}