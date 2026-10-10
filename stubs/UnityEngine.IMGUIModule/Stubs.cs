namespace UnityEngine
{
    public static class GUI
    {
        public static Color color { get => default; set { } }
        public static void Box(Rect position, string text) { }
        public static void Box(Rect position, string text, GUIStyle style) { }
        public static void DrawTexture(Rect position, Texture image) { }
    }
    public class GUIStyle : Il2CppSystem.Object { public GUIStyle() { } public GUIStyleState normal => null; public int fontSize { get => 0; set { } } }
    public class GUIStyleState : Il2CppSystem.Object { public Color textColor { get => default; set { } } }
}
