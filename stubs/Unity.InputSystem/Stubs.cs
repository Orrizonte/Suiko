namespace UnityEngine.InputSystem.Controls
{
    public class ButtonControl : Il2CppSystem.Object { public bool isPressed => false; public bool wasPressedThisFrame => false; }
    public class KeyControl : ButtonControl { }
    public class DpadControl : Il2CppSystem.Object { public ButtonControl up => null; public ButtonControl down => null; public ButtonControl left => null; public ButtonControl right => null; }
}
namespace UnityEngine.InputSystem
{
    using UnityEngine.InputSystem.Controls;
    public class Keyboard : Il2CppSystem.Object
    {
        public static Keyboard current => null;
        public KeyControl upArrowKey => null; public KeyControl downArrowKey => null; public KeyControl leftArrowKey => null; public KeyControl rightArrowKey => null;
        public KeyControl enterKey => null; public KeyControl numpadEnterKey => null; public KeyControl escapeKey => null;
    }
    public class Gamepad : Il2CppSystem.Object
    {
        public static Gamepad current => null;
        public ButtonControl buttonSouth => null; public ButtonControl buttonEast => null; public DpadControl dpad => null;
    }
}
