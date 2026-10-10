using System;
using System.Collections.Generic;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using Framework;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;
using ShareUI;
using ShareUI.Menu;

namespace SuikodenThreeRunes;

internal static class RuneSlotUi
{
	private const int VisibleSlotCount = 3;

	private static readonly HashSet<string> LoggedFailures = new HashSet<string>();

	internal static void DrawMainMenu(PLAYER_BASE player, bool explain, int selectedSlot = -1, bool selecting = false)
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Expected O, but got Unknown
		try
		{
			WindowManager instance = WindowManager.Instance;
			UIMainMenu obj = ((instance != null) ? instance.menuWindow : null);
			UIMainEmblemMenu val = ((obj != null) ? obj.menuEmblem : null);
			if (val == null)
			{
				return;
			}
			RuneState displayState = RuneSlotService.GetDisplayState(player);
			int unlockedSlotCount = RuneSlotService.GetUnlockedSlotCount(player);
			int num = ((selectedSlot >= 0) ? Math.Clamp(selectedSlot, 0, unlockedSlotCount - 1) : displayState.ActiveIndex);
			Il2CppSystem.Collections.Generic.List<UIWindowListCell.Data> val2 = new Il2CppSystem.Collections.Generic.List<UIWindowListCell.Data>();
			for (int i = 0; i < 3; i++)
			{
				bool flag = i < unlockedSlotCount;
				val2.Add(new UIWindowListCell.Data
				{
					index = i,
					leftText = ((i == num) ? $">{i + 1}" : $" {i + 1}"),
					rightText = (flag ? DisplayRuneName(displayState.RuneIds[i]) : $"Bloqué (MAG {RequiredMagic(i)})")
				});
			}
			val.SetEmblemList(val2);
			for (int j = 0; j < 3; j++)
			{
				val.SetEmblemListFontColor(j, j >= unlockedSlotCount);
			}
			val.SetEmblemListSelectCursor(num);
			if (!selecting && explain)
			{
				byte b = displayState.RuneIds[displayState.ActiveIndex];
				if (b == 0)
				{
					val.CloseEmblemSummaryWindow();
					return;
				}
				string runeName = RuneSlotService.GetRuneName(b);
				string monsyoHelp = h_monsyo_c.GetMonsyoHelp((int)b);
				val.SetEmblemSummary(runeName, monsyoHelp);
				val.OpenEmblemSummaryWindow();
			}
		}
		catch (Exception exception)
		{
			LogFailure("menu principal", exception);
		}
	}

	internal static string FormatShopOverlay(PLAYER_BASE player, int selectedSlot = -1, bool selecting = false, bool horizontalNavigation = false)
	{
		try
		{
			RuneState shopDisplayState = RuneSlotService.GetShopDisplayState(player);
			int unlockedSlotCount = RuneSlotService.GetUnlockedSlotCount(player);
			int num = ((selectedSlot >= 0) ? Math.Clamp(selectedSlot, 0, unlockedSlotCount - 1) : shopDisplayState.ActiveIndex);
			string[] array = new string[5]
			{
				selecting ? $"CHOISIR — MAG {RuneSlotService.GetMagic(player)}" : $"RUNES — MAG {RuneSlotService.GetMagic(player)}",
				null,
				null,
				null,
				null
			};
			for (int i = 0; i < 3; i++)
			{
				string value = ((i == num) ? ">" : " ");
				string value2 = ((i < unlockedSlotCount) ? DisplayRuneName(shopDisplayState.RuneIds[i]) : $"Bloqué — MAG {RequiredMagic(i)}");
				array[i + 1] = $"{value} {i + 1}. {value2}";
			}
			array[4] = (selecting ? "↑/↓ Choisir   A Valider   B Annuler" : (horizontalNavigation ? $"←/→ Emplacement   Actif : {shopDisplayState.ActiveIndex + 1}" : $"Emplacement actif : {shopDisplayState.ActiveIndex + 1}"));
			return string.Join("\n", array);
		}
		catch (Exception exception)
		{
			LogFailure("affichage sûr de la boutique", exception);
			return string.Empty;
		}
	}

	internal static void DrawBattle(PLAYER_BASE player, int selectedSlot = -1, bool selecting = false)
	{
		try
		{
			UIBattleManager instance = Singleton<UIBattleManager>.Instance;
			if (instance != null)
			{
				RuneState runeState = RuneSlotService.PeekStoredCombatState(player);
				int unlockedSlotCount = RuneSlotService.GetUnlockedSlotCount(player);
				int num = ((selectedSlot >= 0) ? Math.Clamp(selectedSlot, 0, unlockedSlotCount - 1) : runeState.ActiveIndex);
				string value = DisplayRuneName(runeState.RuneIds[runeState.ActiveIndex]);
				string[] array = new string[3];
				for (int i = 0; i < 3; i++)
				{
					string value2 = ((i == num) ? ">" : " ");
					string value3 = ((i < unlockedSlotCount) ? DisplayRuneName(runeState.RuneIds[i]) : $"Bloqué · MAG {RequiredMagic(i)}");
					array[i] = $"{value2} {i + 1}. {value3}";
				}
				instance.UpdateEmblemDataUI(selecting ? "Choisir l'emplacement de rune" : $"Rune active {runeState.ActiveIndex + 1} : {value}", string.Join("\n", array) + (selecting ? "\n↑/↓ Choisir   A Valider   B Annuler" : string.Empty));
			}
		}
		catch (Exception exception)
		{
			LogFailure("menu de magie en combat", exception);
		}
	}

	internal static void RefreshAfterSelection(PLAYER_BASE player, string context)
	{
		try
		{
			if (context == "Menu")
			{
				DrawMainMenu(player, explain: false);
			}
		}
		catch (Exception exception)
		{
			LogFailure("rafraîchissement après changement", exception);
		}
	}

	internal static bool TryGetPlayer(Il2CppReferenceArray<PLAYER_DATA>? party, int index, out PLAYER_BASE? player)
	{
		player = null;
		if (party == null || index < 0 || index >= ((Il2CppArrayBase<PLAYER_DATA>)(object)party).Length)
		{
			return false;
		}
		PLAYER_DATA val = ((Il2CppArrayBase<PLAYER_DATA>)(object)party)[index];
		if (((val != null) ? val.player_base : null) == null)
		{
			return false;
		}
		player = val.player_base;
		return true;
	}

	private static string DisplayRuneName(byte runeId)
	{
		if (runeId != 0)
		{
			return RuneSlotService.GetRuneName(runeId);
		}
		return "-";
	}

	private static int RequiredMagic(int slot)
	{
		if (slot > 0)
		{
			if (slot != 1)
			{
				return Plugin.ThirdSlotMag.Value;
			}
			return Plugin.SecondSlotMag.Value;
		}
		return 0;
	}

	private static void LogFailure(string surface, Exception exception)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		if (LoggedFailures.Add(surface))
		{
			ManualLogSource modLog = Plugin.ModLog;
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val = new BepInExWarningLogInterpolatedStringHandler(46, 2, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("Affichage des trois runes indisponible dans ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(surface);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(": ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<Exception>(exception);
			}
			modLog.LogWarning(val);
		}
	}
}
