using System;
using System.Runtime.CompilerServices;
namespace BepInEx
{
    [AttributeUsage(AttributeTargets.Class)] public class BepInPlugin : Attribute { public BepInPlugin(string GUID, string Name, string Version) { } }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class BepInDependency : Attribute
    {
        [Flags] public enum DependencyFlags { HardDependency = 1, SoftDependency = 2 }
        public BepInDependency(string DependencyGUID, DependencyFlags Flags = DependencyFlags.HardDependency) { }
    }
    public static class Paths { public static string GameRootPath => null; }
}
namespace BepInEx.Configuration
{
    public class ConfigEntry<T> { public T Value { get => default; set { } } }
    public class ConfigFile { public ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, string description) => null; }
}
namespace BepInEx.Logging
{
    using BepInEx.Core.Logging.Interpolation;
    public class ManualLogSource
    {
        public void LogDebug(object data) { }
        public void LogInfo(object data) { }
        public void LogWarning(object data) { }
        public void LogError(object data) { }
        public void LogInfo([InterpolatedStringHandlerArgument("")] BepInExInfoLogInterpolatedStringHandler logHandler) { }
        public void LogWarning([InterpolatedStringHandlerArgument("")] BepInExWarningLogInterpolatedStringHandler logHandler) { }
        public void LogError([InterpolatedStringHandlerArgument("")] BepInExErrorLogInterpolatedStringHandler logHandler) { }
    }
}
namespace BepInEx.Core.Logging.Interpolation
{
    public class BepInExLogInterpolatedStringHandler
    {
        public BepInExLogInterpolatedStringHandler(int literalLength, int formattedCount, bool isEnabled, out bool shouldAppend) { shouldAppend = isEnabled; }
        public void AppendLiteral(string s) { }
        public void AppendFormatted<T>(T t) { }
        public void AppendFormatted<T>(T t, string format) { }
    }
    [InterpolatedStringHandler] public class BepInExInfoLogInterpolatedStringHandler : BepInExLogInterpolatedStringHandler { public BepInExInfoLogInterpolatedStringHandler(int literalLength, int formattedCount, out bool isEnabled) : base(literalLength, formattedCount, true, out isEnabled) { } }
    [InterpolatedStringHandler] public class BepInExWarningLogInterpolatedStringHandler : BepInExLogInterpolatedStringHandler { public BepInExWarningLogInterpolatedStringHandler(int literalLength, int formattedCount, out bool isEnabled) : base(literalLength, formattedCount, true, out isEnabled) { } }
    [InterpolatedStringHandler] public class BepInExErrorLogInterpolatedStringHandler : BepInExLogInterpolatedStringHandler { public BepInExErrorLogInterpolatedStringHandler(int literalLength, int formattedCount, out bool isEnabled) : base(literalLength, formattedCount, true, out isEnabled) { } }
}
