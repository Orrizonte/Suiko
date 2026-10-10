namespace Framework
{
    public class Singleton<T> : UnityEngine.MonoBehaviour where T : UnityEngine.MonoBehaviour
    {
        public Singleton() : base(System.IntPtr.Zero) { }
        public static T Instance => null;
    }
}
