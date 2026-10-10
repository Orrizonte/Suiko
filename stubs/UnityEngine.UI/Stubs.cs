namespace UnityEngine.UI
{
    public class LayoutGroup : UnityEngine.MonoBehaviour { public LayoutGroup() : base(System.IntPtr.Zero) { } }
    public class HorizontalOrVerticalLayoutGroup : LayoutGroup { public float spacing { get => 0f; set { } } }
    public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public static class LayoutRebuilder { public static void ForceRebuildLayoutImmediate(UnityEngine.RectTransform layoutRoot) { } }
}
