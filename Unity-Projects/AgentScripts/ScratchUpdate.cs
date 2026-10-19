int ticks = 0;
string path = @"D:\phaser\prototype\clogged\Unity-Projects\Logs\update.txt";
System.IO.File.WriteAllText(path, "start\n");
UnityEditor.EditorApplication.update += () =>
{
    ticks++;
    System.IO.File.AppendAllText(path, "update " + ticks + "\n");
    if (ticks >= 3)
    {
        System.IO.File.AppendAllText(path, "done\n");
    }
};
return "registered";
