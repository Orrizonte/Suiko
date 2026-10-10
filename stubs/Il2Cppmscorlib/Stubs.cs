namespace Il2CppSystem
{
    public class Object : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase { }
}
namespace Il2CppSystem.Collections.Generic
{
    public class List<T> : Il2CppSystem.Object
    {
        public List() { }
        public int Count => 0;
        public T this[int index] { get => default; set { } }
        public void Add(T item) { }
    }
    public class Dictionary<TKey, TValue> : Il2CppSystem.Object
    {
        public TValue this[TKey key] { get => default; set { } }
    }
}
