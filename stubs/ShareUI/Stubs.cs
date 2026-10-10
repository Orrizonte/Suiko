using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;
using Share.Battle;
using UnityEngine;
using UnityEngine.UI;
namespace ShareUI
{
    public class UIWindowBasis : MonoBehaviour { public UIWindowBasis() : base(System.IntPtr.Zero) { } public bool IsOpen => false; public bool IsOpening => false; }
    public class UIWindowList : UIWindowBasis
    {
        public List<UIWindowListCell> cellList => null;
        public bool IsFocus => false;
        public int m_enableCellNum { get => 0; set { } }
        public float m_unFocusedHeight { get => 0f; set { } }
        public RectTransform m_contents => null;
        public RectTransform m_rectTransform => null;
        public VerticalLayoutGroup m_contentsLayout => null;
        public VerticalLayoutGroup m_layout => null;
        public void SetCellText(int index, Il2CppStringArray texts) { }
        public void SetData(List<Il2CppStringArray> data) { }
        public void SetGrayout(bool grayout) { }
        public void SetSelectCell(int index) { }
    }
    public class UIWindowListCell : MonoBehaviour
    {
        public UIWindowListCell() : base(System.IntPtr.Zero) { }
        public float height => 0f;
        public RectTransform rectTransform => null;
        public class Data : Il2CppSystem.Object { public Data() { } public int index { get => 0; set { } } public string leftText { get => null; set { } } public string rightText { get => null; set { } } }
    }
    public class UISummary : MonoBehaviour { public UISummary() : base(System.IntPtr.Zero) { } public void Set(string title, string text) { } }
}
namespace ShareUI.Battle
{
    public class UIBattleMain : MonoBehaviour
    {
        public UIBattleMain() : base(System.IntPtr.Zero) { }
        public Dictionary<WindowType, UISummary> m_summaryDict => null;
        public Dictionary<WindowType, UIWindowBasis> m_windowBasisDict => null;
        public Dictionary<WindowType, UIWindowList> m_windowListDict => null;
        public UISummary m_summaryEmblem => null;
        public UIWindowList m_command => null;
        public UIWindowList m_emblem => null;
        public UIWindowList m_magic => null;
        public void FocusWindow(WindowType type, int index) { }
        public void UpdateCommandText(WindowType type, List<string> texts, List<bool> enables, List<int> values) { }
        public void UpdateSummaryText(WindowType type, List<string> titles, List<string> texts) { }
    }
}
namespace ShareUI.Menu
{
    public class UIMainMenu : MonoBehaviour { public UIMainMenu() : base(System.IntPtr.Zero) { } public UIMainEmblemMenu menuEmblem => null; }
    public class UIMainEmblemMenu : MonoBehaviour
    {
        public UIMainEmblemMenu() : base(System.IntPtr.Zero) { }
        public void CloseEmblemSummaryWindow() { }
        public void OpenEmblemSummaryWindow() { }
        public void SetEmblemList(List<UIWindowListCell.Data> list) { }
        public void SetEmblemListFontColor(int index, bool gray) { }
        public void SetEmblemListSelectCursor(int index) { }
        public void SetEmblemSummary(string name, string help) { }
    }
}
