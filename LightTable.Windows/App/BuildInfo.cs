namespace LightTable.Windows;

static class BuildInfo
{
    public static string Version=>typeof(BuildInfo).Assembly.GetName().Version?.ToString()??"0.1.75.2";
}
