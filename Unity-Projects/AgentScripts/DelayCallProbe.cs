using System.IO;
using UnityEditor;
using UnityEngine;

public static class DelayCallProbe
{
    public static string Run()
    {
        string marker = Path.Combine(Application.dataPath, "..", "Logs", "delaycall-probe.txt");
        if (File.Exists(marker))
        {
            File.Delete(marker);
        }

        EditorApplication.delayCall += () => File.WriteAllText(marker, "fired " + System.DateTime.Now.ToString("O"));
        return "scheduled";
    }
}
