using System;
namespace Il2CppInterop.Runtime.InteropTypes
{
    public class Il2CppObjectBase { public IntPtr Pointer => IntPtr.Zero; }
}
namespace Il2CppInterop.Runtime.InteropTypes.Arrays
{
    public abstract class Il2CppArrayBase<T> : Il2CppObjectBase
    {
        public int Length => 0;
        public T this[int index] { get => default; set { } }
    }
    public class Il2CppReferenceArray<T> : Il2CppArrayBase<T> where T : Il2CppObjectBase { }
    public class Il2CppStructArray<T> : Il2CppArrayBase<T> where T : unmanaged { }
    public class Il2CppStringArray : Il2CppArrayBase<string> { public Il2CppStringArray(long size) { } }
}
