using System.Threading.Tasks;
using UnityEditor;

public static class DelayCallProbe2
{
    public static async Task<string> Run()
    {
        bool fired = false;
        EditorApplication.delayCall += () => fired = true;
        await Task.Delay(2000);
        return "delayCallFired=" + fired + " focus=" + EditorApplication.isFocused;
    }
}
