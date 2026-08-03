using System.Collections.Generic;

public class MissionResult
{
    public bool Success;
    public string Message;
    public Dictionary<string, int> Resources;

    public MissionResult(bool success, string message, Dictionary<string, int> resources = null)
    {
        Success = success;
        Message = message;
        Resources = resources ?? new Dictionary<string, int>();
    }
}