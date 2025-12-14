using System;
using System.Reflection;


public class CorruptionReflectionTools
{
    public static void UpdateSelectable(KMSelectable selectable)
    {
        try
        {
            Type type = Type.GetType("ModSelectable, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            type
                .GetMethod("CopySettingsFromProxy", BindingFlags.Instance | BindingFlags.Public)
                .Invoke(selectable.GetComponent("ModSelectable"), null);
        }
        catch { }
    }

    public static bool IsSolved(KMBombModule module)
    {
        try
        {
            Type type = Type.GetType("ModBombComponent, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            return (bool)type
                .GetField("IsSolved", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(module.GetComponent("ModBombComponent"));
        }
        catch { return false; }
    }
}
