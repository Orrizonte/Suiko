public class GRInputManager : Il2CppSystem.Object
{
    // Valeurs utilisees par le mod : 0=Haut, 1=Bas, 4=Valider, 5=Annuler.
    public enum Type { Up = 0, Down = 1, Left = 2, Right = 3, Decide = 4, Cancel = 5 }
    public static bool Pause { get => false; set { } }
    public static bool IsOn(Type type) => false;
    public static bool IsPress(Type type) => false;
    public static bool IsRepeat(Type type) => false;
}
namespace Share.Battle
{
    // Valeurs utilisees par le mod : 1=Commandes, 5=Runes (Emblem), 8=Magie.
    public enum WindowType { None = 0, Command = 1, Emblem = 5, Magic = 8 }
}
