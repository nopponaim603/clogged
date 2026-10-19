System.IO.File.AppendAllText(@"D:\phaser\prototype\clogged\Unity-Projects\Logs\dc.txt", "scheduled\n");
UnityEditor.EditorApplication.delayCall += () => System.IO.File.AppendAllText(@"D:\phaser\prototype\clogged\Unity-Projects\Logs\dc.txt", "fired\n");
return UnityEditor.EditorApplication.isFocused;
