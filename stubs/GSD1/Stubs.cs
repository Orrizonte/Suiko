using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Share.Battle;

public class PLAYER_MONSYO : Il2CppSystem.Object { public byte monsyo_id { get => 0; set { } } public byte monsyo_level { get => 0; set { } } public ushort monsyo_exp { get => 0; set { } } }
public class PLAYER_BASE : Il2CppSystem.Object { public int chara_no { get => 0; set { } } public PLAYER_MONSYO monsyo_data { get => null; set { } } public Il2CppStructArray<byte> noryoku { get => null; set { } } }
public class PLAYER_DATA : Il2CppSystem.Object { public PLAYER_BASE player_base { get => null; set { } } }
public class P_BATTLE_DATA : Il2CppSystem.Object { public PLAYER_DATA player_data { get => null; set { } } }
public class PM_DATA : Il2CppSystem.Object { }
public class BATTLE_WORK : Il2CppSystem.Object { public Il2CppReferenceArray<P_BATTLE_DATA> p_battle_data { get => null; set { } } public Il2CppReferenceArray<PM_DATA> pm_data { get => null; set { } } }
public class GAME_WORK : Il2CppSystem.Object { public Il2CppReferenceArray<PLAYER_BASE> player_base { get => null; set { } } }
public class MONSYO_DATA : Il2CppSystem.Object { public int name { get => 0; set { } } }
public class WIN : Il2CppSystem.Object { public byte close_flg { get => 0; set { } } public byte disp_flg { get => 0; set { } } public byte open_flg { get => 0; set { } } }

public class BattleBase : Il2CppSystem.Object
{
    public class WINDOW_WORK : Il2CppSystem.Object
    {
        public PLAYER_DATA selected_player_data { get => null; set { } }
        public int index_level { get => 0; set { } }
        public int magic_kazu { get => 0; set { } }
        public int return_value { get => 0; set { } }
        public int selected_item { get => 0; set { } }
        public int selected_monster { get => 0; set { } }
        public int selected_player { get => 0; set { } }
        public int window_index { get => 0; set { } }
        public int window_player_index { get => 0; set { } }
        public int window_step { get => 0; set { } }
    }
    public static BATTLE_WORK battle_work { get => null; set { } }
    public static WINDOW_WORK window_work { get => null; set { } }
    public static int make_magic_window(int player_no, bool flag) => 0;
}

public class G_cmd_c : Il2CppSystem.Object
{
    public class COMMAND_WORK : Il2CppSystem.Object
    {
        public Il2CppReferenceArray<PLAYER_DATA> player_data { get => null; set { } }
        public PLAYER_DATA selected_player_data { get => null; set { } }
        public int index { get => 0; set { } }
        public int selected_player { get => 0; set { } }
    }
    public static COMMAND_WORK command_work { get => null; set { } }
}

public class h_monsyo_c : Il2CppSystem.Object
{
    public class MONSYO_WORK : Il2CppSystem.Object
    {
        public Il2CppReferenceArray<PLAYER_DATA> player_data { get => null; set { } }
        public Il2CppReferenceArray<WIN> window { get => null; set { } }
        public int index { get => 0; set { } }
        public int selected_player { get => 0; set { } }
    }
    public static MONSYO_WORK monsyo_work { get => null; set { } }
    public static string GetMonsyoHelp(int monsyo_id) => null;
}

public class G_monsyo_c_data : Il2CppSystem.Object { public static Il2CppReferenceArray<MONSYO_DATA> monsyo_data_table { get => null; set { } } }
public class G1_string_id_h : Il2CppSystem.Object { public static string G1_SID_MSG(int id) => null; }
public class OldSrcBase : Il2CppSystem.Object { public static GAME_WORK game_work { get => null; set { } } }
public class SaveDataManager : Il2CppSystem.Object { }

public class WindowManager : Il2CppSystem.Object { public static WindowManager Instance => null; public ShareUI.Menu.UIMainMenu menuWindow => null; }

public class UIBattleManager : Framework.Singleton<UIBattleManager>
{
    public ShareUI.Battle.UIBattleMain battleWindow => null;
    public bool IsOpenWindowUI(WindowType type) => false;
    public void CloseWindowUI(WindowType type) { }
    public void OpenWindowUI(WindowType type, bool openSummary) { }
    public void SetCursorUI(WindowType type, int index) { }
    public void UpdateEmblemDataUI(string title, string text) { }
}
