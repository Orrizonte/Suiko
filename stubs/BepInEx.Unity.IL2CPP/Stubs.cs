namespace BepInEx.Unity.IL2CPP
{
    public abstract class BasePlugin
    {
        public BepInEx.Logging.ManualLogSource Log => null;
        public BepInEx.Configuration.ConfigFile Config => null;
        public abstract void Load();
        public T AddComponent<T>() where T : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase => null;
    }
}
