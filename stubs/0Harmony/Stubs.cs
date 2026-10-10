using System;
using System.Reflection;
namespace HarmonyLib
{
    public class Harmony { public Harmony(string id) { } public void PatchAll(Assembly assembly) { } public PatchClassProcessor CreateClassProcessor(Type type) => null; public MethodInfo Patch(MethodBase original, HarmonyMethod prefix = null, HarmonyMethod postfix = null, HarmonyMethod transpiler = null, HarmonyMethod finalizer = null, HarmonyMethod ilmanipulator = null) => null; }
    public class PatchClassProcessor { public System.Collections.Generic.List<MethodInfo> Patch() => null; }
    public class HarmonyMethod { public HarmonyMethod(MethodInfo method) { } }
    public static class AccessTools { public static MethodInfo Method(Type type, string name, Type[] parameters = null, Type[] generics = null) => null; }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class HarmonyPatch : Attribute { public HarmonyPatch() { } public HarmonyPatch(Type declaringType, string methodName) { } public HarmonyPatch(Type declaringType, string methodName, params Type[] argumentTypes) { } }
    [AttributeUsage(AttributeTargets.Method)] public class HarmonyPrefix : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public class HarmonyPostfix : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public class HarmonyFinalizer : Attribute { }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)] public class HarmonyPriority : Attribute { public HarmonyPriority(int priority) { } }
}
