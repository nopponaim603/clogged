using McpUnity.Unity;

public static class RestartMcpBridge
{
    public static string Run(int port)
    {
        McpUnitySettings settings = McpUnitySettings.Instance;
        settings.Port = port;
        settings.AutoStartServer = true;
        settings.SaveSettings();

        McpUnityServer server = McpUnityServer.Instance;
        if (server == null)
        {
            return "McpUnityServer.Instance is null";
        }

        bool before = server.IsListening;
        server.RestartServer();
        return $"port={settings.Port} before={before} scheduled={server.HasScheduledStart} status={server.ScheduledStartStatus}";
    }
}
