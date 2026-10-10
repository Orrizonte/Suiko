using System;
using System.Collections.Generic;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace SuikodenThreeRunes;

internal static class RuneSlotService
{
	private const int RuneRadix = 34;

	private const int PairStates = 1156;

	private const int LevelStates = 3468;

	private const ushort Signature = 32768;

	private const ushort PayloadMask = 32767;

	private const byte EmptyRune = 0;

	private const byte SoulEaterRune = 1;

	private const int MagicStatIndex = 4;

	private const int RuneShopPlayerWindowIndex = 3;

	private const int RuneShopItemWindowIndex = 4;

	private const int RuneShopEquippedRuneWindowIndex = 5;

	private const int RuneShopHelpWindowIndex = 6;

	private static readonly Dictionary<int, RuneState> States = new Dictionary<int, RuneState>();

	private static bool IsNativeFixedRune(int charaNo, byte runeId)
	{
		switch (charaNo)
		{
		case 8:
			if (runeId != 1)
			{
				break;
			}
			return true;
		case 6:
			if (runeId != 8)
			{
				break;
			}
			return true;
		case 36:
			if (runeId != 10)
			{
				break;
			}
			return true;
		case 23:
			if (runeId != 9)
			{
				break;
			}
			return true;
		case 75:
			if (runeId != 33)
			{
				break;
			}
			return true;
		case 91:
			if (runeId != 18)
			{
				break;
			}
			return true;
		case 39:
			if (runeId != 12)
			{
				break;
			}
			return true;
		case 18:
			if (runeId != 7)
			{
				break;
			}
			return true;
		case 13:
			if (runeId != 11)
			{
				break;
			}
			return true;
		case 62:
			if (runeId != 5)
			{
				break;
			}
			return true;
		case 48:
			if (runeId != 27)
			{
				break;
			}
			return true;
		case 49:
			if (runeId != 31)
			{
				break;
			}
			return true;
		case 59:
			if (runeId != 30)
			{
				break;
			}
			return true;
		case 109:
			if (runeId != 4)
			{
				break;
			}
			return true;
		}
		return false;
	}

	private static byte GetNativeRuneLevel(PLAYER_BASE player, RuneState state, byte activeRune)
	{
		switch (activeRune)
		{
		case 0:
			return 0;
		case 1:
			return state.SoulEaterLevel;
		default:
			if (!IsNativeFixedRune(player.chara_no, activeRune))
			{
				return 0;
			}
			return 1;
		}
	}

	private static void NormalizeNativeRuneLevel(PLAYER_BASE player, RuneState state)
	{
		PLAYER_MONSYO monsyo_data = player.monsyo_data;
		if (monsyo_data != null)
		{
			byte activeRune = ValidateRuneId(state.RuneIds[state.ActiveIndex]);
			monsyo_data.monsyo_level = GetNativeRuneLevel(player, state, activeRune);
		}
	}

	internal static void Reset()
	{
		States.Clear();
	}

	internal static bool TryGetSelectedPlayer(out PLAYER_BASE? player, out string context)
	{
		return TryGetSelectedPlayer(int.MinValue, out player, out context);
	}

	internal static bool TryGetSelectedPlayer(int lockedCharaNo, out PLAYER_BASE? player, out string context)
	{
		player = null;
		context = string.Empty;
		try
		{
			BattleBase.WINDOW_WORK window_work = BattleBase.window_work;
			BATTLE_WORK battle_work = BattleBase.battle_work;
			if (window_work != null && battle_work != null)
			{
				PLAYER_DATA selected_player_data = window_work.selected_player_data;
				if (selected_player_data != null && selected_player_data.player_base != null)
				{
					player = selected_player_data.player_base;
					context = "Combat";
					return true;
				}
				int selected_player = window_work.selected_player;
				Il2CppReferenceArray<P_BATTLE_DATA> p_battle_data = battle_work.p_battle_data;
				if (p_battle_data != null && selected_player >= 0 && selected_player < ((Il2CppArrayBase<P_BATTLE_DATA>)(object)p_battle_data).Length)
				{
					P_BATTLE_DATA val = ((Il2CppArrayBase<P_BATTLE_DATA>)(object)p_battle_data)[selected_player];
					if (val != null && val.player_data != null && val.player_data.player_base != null)
					{
						player = val.player_data.player_base;
						context = "Combat";
						return true;
					}
				}
			}
		}
		catch
		{
		}
		try
		{
			h_monsyo_c.MONSYO_WORK monsyo_work = h_monsyo_c.monsyo_work;
			if (monsyo_work != null && IsRuneShopSelectionWindowVisible(monsyo_work))
			{
				if (lockedCharaNo != int.MinValue && TryFromPartyByCharaNo(monsyo_work.player_data, lockedCharaNo, out player))
				{
					context = "Boutique de runes";
					return true;
				}
				if (IsRuneShopPlayerWindowVisible(monsyo_work) && TryFromParty(monsyo_work.player_data, monsyo_work.index, monsyo_work.selected_player, out player))
				{
					context = "Boutique de runes";
					return true;
				}
				if (TryFromParty(monsyo_work.player_data, monsyo_work.selected_player, monsyo_work.index, out player))
				{
					context = "Boutique de runes";
					return true;
				}
			}
		}
		catch
		{
		}
		try
		{
			G_cmd_c.COMMAND_WORK command_work = G_cmd_c.command_work;
			if (command_work != null && command_work.selected_player_data != null && command_work.selected_player_data.player_base != null)
			{
				player = command_work.selected_player_data.player_base;
				context = "Menu";
				return true;
			}
			if (command_work != null && TryFromParty(command_work.player_data, command_work.selected_player, command_work.index, out player))
			{
				context = "Menu";
				return true;
			}
		}
		catch
		{
		}
		player = null;
		return false;
	}

	internal static bool IsRuneShopSelectionWindowVisible()
	{
		try
		{
			h_monsyo_c.MONSYO_WORK monsyo_work = h_monsyo_c.monsyo_work;
			return monsyo_work != null && IsRuneShopSelectionWindowVisible(monsyo_work);
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsRuneShopItemWindowVisible()
	{
		try
		{
			h_monsyo_c.MONSYO_WORK monsyo_work = h_monsyo_c.monsyo_work;
			return monsyo_work != null && IsWindowVisible(monsyo_work, 4);
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsRuneShopPlayerWindowVisible()
	{
		try
		{
			h_monsyo_c.MONSYO_WORK monsyo_work = h_monsyo_c.monsyo_work;
			return monsyo_work != null && IsRuneShopPlayerWindowVisible(monsyo_work);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsRuneShopPlayerWindowVisible(h_monsyo_c.MONSYO_WORK work)
	{
		return IsWindowVisible(work, 3);
	}

	internal static bool SyncRuneShopHighlightedPlayer(out int previousSelected, out int currentIndex)
	{
		previousSelected = -1;
		currentIndex = -1;
		try
		{
			h_monsyo_c.MONSYO_WORK monsyo_work = h_monsyo_c.monsyo_work;
			if (monsyo_work == null || !IsRuneShopSelectionWindowVisible(monsyo_work) || !IsRuneShopPlayerWindowVisible(monsyo_work))
			{
				return false;
			}
			Il2CppReferenceArray<PLAYER_DATA> player_data = monsyo_work.player_data;
			if (player_data == null || ((Il2CppArrayBase<PLAYER_DATA>)(object)player_data).Length == 0)
			{
				return false;
			}
			int index = monsyo_work.index;
			if (index < 0 || index >= ((Il2CppArrayBase<PLAYER_DATA>)(object)player_data).Length)
			{
				return false;
			}
			PLAYER_DATA obj = ((Il2CppArrayBase<PLAYER_DATA>)(object)player_data)[index];
			if (((obj != null) ? obj.player_base : null) == null)
			{
				return false;
			}
			previousSelected = monsyo_work.selected_player;
			currentIndex = index;
			if (previousSelected == currentIndex)
			{
				return false;
			}
			monsyo_work.selected_player = currentIndex;
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsRuneShopSelectionWindowVisible(h_monsyo_c.MONSYO_WORK work)
	{
		if (!IsWindowVisible(work, 3) && !IsWindowVisible(work, 4) && !IsWindowVisible(work, 5))
		{
			return IsWindowVisible(work, 6);
		}
		return true;
	}

	private static bool IsWindowVisible(h_monsyo_c.MONSYO_WORK work, int windowIndex)
	{
		Il2CppReferenceArray<WIN> window = work.window;
		if (window == null || windowIndex < 0 || ((Il2CppArrayBase<WIN>)(object)window).Length <= windowIndex)
		{
			return false;
		}
		WIN val = ((Il2CppArrayBase<WIN>)(object)window)[windowIndex];
		if (val == null || val.close_flg != 0)
		{
			return false;
		}
		if (val.disp_flg == 0)
		{
			return val.open_flg != 0;
		}
		return true;
	}

	private static bool TryFromPartyByCharaNo(Il2CppReferenceArray<PLAYER_DATA>? party, int charaNo, out PLAYER_BASE? player)
	{
		player = null;
		if (party == null || ((Il2CppArrayBase<PLAYER_DATA>)(object)party).Length == 0)
		{
			return false;
		}
		for (int i = 0; i < ((Il2CppArrayBase<PLAYER_DATA>)(object)party).Length; i++)
		{
			PLAYER_DATA val = ((Il2CppArrayBase<PLAYER_DATA>)(object)party)[i];
			if (((val != null) ? val.player_base : null) != null && val.player_base.chara_no == charaNo)
			{
				player = val.player_base;
				return true;
			}
		}
		return false;
	}

	private static bool TryFromParty(Il2CppReferenceArray<PLAYER_DATA>? party, int selected, int fallback, out PLAYER_BASE? player)
	{
		player = null;
		if (party == null || ((Il2CppArrayBase<PLAYER_DATA>)(object)party).Length == 0)
		{
			return false;
		}
		int num = ((selected >= 0 && selected < ((Il2CppArrayBase<PLAYER_DATA>)(object)party).Length) ? selected : fallback);
		if (num < 0 || num >= ((Il2CppArrayBase<PLAYER_DATA>)(object)party).Length)
		{
			return false;
		}
		PLAYER_DATA val = ((Il2CppArrayBase<PLAYER_DATA>)(object)party)[num];
		if (val == null || val.player_base == null)
		{
			return false;
		}
		player = val.player_base;
		return true;
	}

	internal static RuneCycleResult Cycle(PLAYER_BASE player, int direction)
	{
		RuneState state = GetState(player);
		Capture(player, state);
		int unlockedSlotCount = GetUnlockedSlotCount(player);
		if (unlockedSlotCount <= 1)
		{
			WriteState(player, state);
			return RuneCycleResult.Locked;
		}
		int activeIndex = (state.ActiveIndex + ((direction >= 0) ? 1 : (-1)) + unlockedSlotCount) % unlockedSlotCount;
		state.ActiveIndex = activeIndex;
		WriteState(player, state);
		return RuneCycleResult.Changed;
	}

	internal static RuneCycleResult SelectSlot(PLAYER_BASE player, int slot)
	{
		RuneState state = GetState(player);
		Capture(player, state);
		int unlockedSlotCount = GetUnlockedSlotCount(player);
		if (slot < 0 || slot >= unlockedSlotCount)
		{
			WriteState(player, state);
			return RuneCycleResult.Locked;
		}
		state.ActiveIndex = slot;
		WriteState(player, state);
		return RuneCycleResult.Changed;
	}

	internal static RuneState SelectStoredCombatSlot(PLAYER_BASE player, int slot)
	{
		RuneState state = GetState(player);
		int unlockedSlotCount = GetUnlockedSlotCount(player);
		if (slot < 0 || slot >= unlockedSlotCount)
		{
			WriteState(player, state);
			return state;
		}
		state.ActiveIndex = slot;
		WriteState(player, state);
		return state;
	}

	internal static void Capture(PLAYER_BASE player)
	{
		RuneState state = GetState(player);
		Capture(player, state);
		if (IsRuneShopSelectionWindowVisible() && player.monsyo_data != null && player.monsyo_data.monsyo_id != 1)
		{
			player.monsyo_data.monsyo_exp = 0;
		}
		else
		{
			WritePackedState(player, state);
		}
	}

	internal static void EnterRuneShopCompatibility(PLAYER_BASE player)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Expected O, but got Unknown
		RuneState state = GetState(player);
		Capture(player, state);
		ApplyRuneShopCompatibilityValue(player, state);
		PLAYER_MONSYO monsyo_data = player.monsyo_data;
		if (monsyo_data != null)
		{
			byte b = ValidateRuneId(state.RuneIds[state.ActiveIndex]);
			bool flag = b == 1 || IsNativeFixedRune(player.chara_no, b);
			ManualLogSource modLog = Plugin.ModLog;
			bool flag2 = default(bool);
			BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(61, 5, out flag2);
			if (flag2)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R107 boutique : chara=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(player.chara_no);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", slot=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(state.ActiveIndex + 1);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", rune=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<byte>(b);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", ");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("monsyo_level=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<byte>(monsyo_data.monsyo_level);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", statut=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(flag ? "fixe" : "amovible");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
			}
			modLog.LogInfo(val);
		}
	}

	internal static void CaptureForRuneShop(PLAYER_BASE player)
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		RuneState state = GetState(player);
		byte b = state.RuneIds[state.ActiveIndex];
		byte b2 = (byte)((player.monsyo_data != null) ? ValidateRuneId(player.monsyo_data.monsyo_id) : 0);
		Capture(player, state);
		if (b != 0 && b2 == 0 && state.RuneIds[state.ActiveIndex] == 0)
		{
			ManualLogSource modLog = Plugin.ModLog;
			bool flag = default(bool);
			BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(77, 3, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R107 boutique : retrait natif detecte pour personnage ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(player.chara_no);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", slot ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(state.ActiveIndex + 1);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", rune ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<byte>(b);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" -> vide.");
			}
			modLog.LogInfo(val);
		}
		ApplyRuneShopCompatibilityValue(player, state);
	}

	internal static void ExitRuneShopCompatibility(PLAYER_BASE player)
	{
		RuneState state = GetState(player);
		Capture(player, state);
		WritePackedState(player, state);
	}

	private static void ApplyRuneShopCompatibilityValue(PLAYER_BASE player, RuneState state)
	{
		PLAYER_MONSYO monsyo_data = player.monsyo_data;
		if (monsyo_data != null)
		{
			NormalizeNativeRuneLevel(player, state);
			if (ValidateRuneId(state.RuneIds[state.ActiveIndex]) == 1)
			{
				WritePackedState(player, state);
			}
			else
			{
				monsyo_data.monsyo_exp = 0;
			}
		}
	}

	internal static RuneState GetDisplayState(PLAYER_BASE player)
	{
		RuneState state = GetState(player);
		Capture(player, state);
		WriteState(player, state);
		return state;
	}

	internal static bool PresentStoredCombatSlotNativeOnly(PLAYER_BASE player, int slot, out byte runeId)
	{
		runeId = 0;
		RuneState state = GetState(player);
		int unlockedSlotCount = GetUnlockedSlotCount(player);
		if (slot < 0 || slot >= unlockedSlotCount || player.monsyo_data == null)
		{
			return false;
		}
		runeId = ValidateRuneId(state.RuneIds[slot]);
		if (runeId == 0)
		{
			return false;
		}
		player.monsyo_data.monsyo_id = runeId;
		player.monsyo_data.monsyo_level = GetNativeRuneLevel(player, state, runeId);
		return true;
	}

	internal static bool PrepareDirectNativeRuntime(PLAYER_BASE player, byte directRuneId, out ushort previousExp)
	{
		previousExp = 0;
		PLAYER_MONSYO monsyo_data = player.monsyo_data;
		if (monsyo_data == null)
		{
			return false;
		}
		RuneState state = GetState(player);
		int num = Math.Clamp(state.ActiveIndex, 0, 2);
		byte b = ValidateRuneId(state.RuneIds[num]);
		if (b != directRuneId)
		{
			return false;
		}
		previousExp = monsyo_data.monsyo_exp;
		monsyo_data.monsyo_id = b;
		monsyo_data.monsyo_level = GetNativeRuneLevel(player, state, b);
		monsyo_data.monsyo_exp = 0;
		return true;
	}

	internal static RuneState RestoreStoredCombatState(PLAYER_BASE player)
	{
		RuneState state = GetState(player);
		WriteState(player, state);
		return state;
	}

	internal static RuneState PeekStoredCombatState(PLAYER_BASE player)
	{
		return GetState(player);
	}

	internal static string DescribeStoredCombatState(PLAYER_BASE player)
	{
		RuneState state = GetState(player);
		return $"active={state.ActiveIndex + 1} ids={state.RuneIds[0]}/{state.RuneIds[1]}/{state.RuneIds[2]}";
	}

	internal static RuneState GetShopDisplayState(PLAYER_BASE player)
	{
		RuneState state = GetState(player);
		Capture(player, state);
		return state;
	}

	private static void Capture(PLAYER_BASE player, RuneState state)
	{
		PLAYER_MONSYO monsyo_data = player.monsyo_data;
		if (monsyo_data != null)
		{
			state.RuneIds[state.ActiveIndex] = ValidateRuneId(monsyo_data.monsyo_id);
			if (monsyo_data.monsyo_id == 1)
			{
				state.SoulEaterLevel = (byte)Math.Clamp((int)monsyo_data.monsyo_level, 0, 8);
			}
		}
	}

	internal static void NormalizeCurrentGame()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		try
		{
			GAME_WORK game_work = OldSrcBase.game_work;
			if (game_work != null)
			{
				NormalizeGame(game_work);
			}
		}
		catch (Exception ex)
		{
			ManualLogSource modLog = Plugin.ModLog;
			bool flag = default(bool);
			BepInExWarningLogInterpolatedStringHandler val = new BepInExWarningLogInterpolatedStringHandler(33, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("Normalisation finale impossible: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
			}
			modLog.LogWarning(val);
		}
	}

	internal static void PrepareGame(GAME_WORK game)
	{
		Il2CppReferenceArray<PLAYER_BASE> player_base = game.player_base;
		if (player_base == null)
		{
			return;
		}
		for (int i = 0; i < ((Il2CppArrayBase<PLAYER_BASE>)(object)player_base).Length; i++)
		{
			PLAYER_BASE val = ((Il2CppArrayBase<PLAYER_BASE>)(object)player_base)[i];
			if (val != null && val.monsyo_data != null)
			{
				RuneState state = GetState(val);
				if (Array.IndexOf(state.RuneIds, (byte)1) >= 0)
				{
					int b = Array.IndexOf(state.RuneIds, (byte)1);
					Swap(state.RuneIds, 0, b);
					state.ActiveIndex = 0;
					WriteState(val, state);
				}
			}
		}
	}

	internal static void NormalizeGame(GAME_WORK game)
	{
		Il2CppReferenceArray<PLAYER_BASE> player_base = game.player_base;
		if (player_base == null)
		{
			return;
		}
		for (int i = 0; i < ((Il2CppArrayBase<PLAYER_BASE>)(object)player_base).Length; i++)
		{
			PLAYER_BASE val = ((Il2CppArrayBase<PLAYER_BASE>)(object)player_base)[i];
			if (val != null && val.monsyo_data != null)
			{
				RuneState state = GetState(val);
				Capture(val, state);
				int num = Array.IndexOf(state.RuneIds, (byte)1);
				if (num > 0)
				{
					Swap(state.RuneIds, 0, num);
				}
				state.ActiveIndex = 0;
				WriteState(val, state);
			}
		}
	}

	internal static RuneGameSnapshot SnapshotAndNormalizeForSave(GAME_WORK game)
	{
		RuneGameSnapshot runeGameSnapshot = new RuneGameSnapshot();
		Il2CppReferenceArray<PLAYER_BASE> player_base = game.player_base;
		if (player_base == null)
		{
			return runeGameSnapshot;
		}
		for (int i = 0; i < ((Il2CppArrayBase<PLAYER_BASE>)(object)player_base).Length; i++)
		{
			PLAYER_BASE val = ((Il2CppArrayBase<PLAYER_BASE>)(object)player_base)[i];
			if (val != null && val.monsyo_data != null)
			{
				RuneState state = GetState(val);
				Capture(val, state);
				RuneStateSnapshot runeStateSnapshot = new RuneStateSnapshot
				{
					ActiveIndex = state.ActiveIndex,
					SoulEaterLevel = state.SoulEaterLevel
				};
				Array.Copy(state.RuneIds, runeStateSnapshot.RuneIds, state.RuneIds.Length);
				runeGameSnapshot.Players[val.chara_no] = runeStateSnapshot;
			}
		}
		NormalizeGame(game);
		return runeGameSnapshot;
	}

	internal static int RestoreAfterSaveCopy(GAME_WORK game, RuneGameSnapshot? snapshot)
	{
		if (snapshot == null || snapshot.Players.Count == 0)
		{
			return 0;
		}
		Il2CppReferenceArray<PLAYER_BASE> player_base = game.player_base;
		if (player_base == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < ((Il2CppArrayBase<PLAYER_BASE>)(object)player_base).Length; i++)
		{
			PLAYER_BASE val = ((Il2CppArrayBase<PLAYER_BASE>)(object)player_base)[i];
			if (val != null && val.monsyo_data != null && snapshot.Players.TryGetValue(val.chara_no, out RuneStateSnapshot value))
			{
				RuneState state = GetState(val);
				Array.Copy(value.RuneIds, state.RuneIds, state.RuneIds.Length);
				state.SoulEaterLevel = value.SoulEaterLevel;
				state.ActiveIndex = Math.Clamp(value.ActiveIndex, 0, GetUnlockedSlotCount(val) - 1);
				WriteState(val, state);
				if (state.ActiveIndex > 0)
				{
					num++;
				}
			}
		}
		return num;
	}

	internal static string FormatStatus(PLAYER_BASE player, string context, RuneCycleResult result)
	{
		RuneState state = GetState(player);
		int magic = GetMagic(player);
		int unlockedSlotCount = GetUnlockedSlotCount(player);
		string value = ((result == RuneCycleResult.Locked) ? $"Emplacement suivant verrouille (MAG {Plugin.SecondSlotMag.Value} requise)" : $"Rune active {state.ActiveIndex + 1}/{unlockedSlotCount}");
		string[] array = new string[3];
		for (int i = 0; i < array.Length; i++)
		{
			bool flag = i < unlockedSlotCount;
			string value2 = ((i == state.ActiveIndex) ? ">" : " ");
			array[i] = (flag ? $"{value2}{i + 1}:{GetRuneName(state.RuneIds[i])}" : $" {i + 1}:verrouille");
		}
		return $"{context} - {value} - MAG {magic}\n{string.Join("   ", array)}";
	}

	private static RuneState GetState(PLAYER_BASE player)
	{
		int chara_no = player.chara_no;
		if (States.TryGetValue(chara_no, out RuneState value))
		{
			return value;
		}
		RuneState runeState = Decode(player);
		States[chara_no] = runeState;
		WriteState(player, runeState);
		return runeState;
	}

	private static RuneState Decode(PLAYER_BASE player)
	{
		RuneState runeState = new RuneState();
		PLAYER_MONSYO monsyo_data = player.monsyo_data;
		ushort monsyo_exp = monsyo_data.monsyo_exp;
		if ((monsyo_exp & 0x8000) == 0)
		{
			runeState.RuneIds[0] = ValidateRuneId(monsyo_data.monsyo_id);
			runeState.RuneIds[1] = 0;
			runeState.RuneIds[2] = 0;
			runeState.ActiveIndex = 0;
			runeState.SoulEaterLevel = (byte)((monsyo_data.monsyo_id == 1) ? ((byte)Math.Clamp((int)monsyo_data.monsyo_level, 0, 8)) : 0);
			return runeState;
		}
		int num = monsyo_exp & 0x7FFF;
		int num2 = num / 3468;
		int num3 = num % 3468;
		int num4 = num3 / 1156;
		int num5 = num3 % 1156;
		int num6 = num5 / 34;
		int num7 = num5 % 34;
		if (num4 < 0 || num4 > 2 || num2 < 0 || num2 > 8)
		{
			return NewVanillaState(monsyo_data);
		}
		runeState.ActiveIndex = num4;
		runeState.SoulEaterLevel = (byte)num2;
		runeState.RuneIds[num4] = ValidateRuneId(monsyo_data.monsyo_id);
		int num8 = ((num4 == 0) ? 1 : 0);
		int num9 = ((num4 == 2) ? 1 : 2);
		runeState.RuneIds[num8] = ValidateRuneId((byte)num7);
		runeState.RuneIds[num9] = ValidateRuneId((byte)num6);
		return runeState;
	}

	private static RuneState NewVanillaState(PLAYER_MONSYO rune)
	{
		RuneState runeState = new RuneState();
		runeState.RuneIds[0] = ValidateRuneId(rune.monsyo_id);
		runeState.SoulEaterLevel = (byte)((rune.monsyo_id == 1) ? ((byte)Math.Clamp((int)rune.monsyo_level, 0, 8)) : 0);
		return runeState;
	}

	private static void WriteState(PLAYER_BASE player, RuneState state)
	{
		PLAYER_MONSYO monsyo_data = player.monsyo_data;
		if (monsyo_data != null)
		{
			int unlockedSlotCount = GetUnlockedSlotCount(player);
			if (state.ActiveIndex >= unlockedSlotCount)
			{
				state.ActiveIndex = 0;
			}
			byte b2 = (monsyo_data.monsyo_id = ValidateRuneId(state.RuneIds[state.ActiveIndex]));
			monsyo_data.monsyo_level = GetNativeRuneLevel(player, state, b2);
			if (IsRuneShopSelectionWindowVisible() && b2 != 1)
			{
				monsyo_data.monsyo_exp = 0;
			}
			else
			{
				WritePackedState(player, state);
			}
		}
	}

	private static void WritePackedState(PLAYER_BASE player, RuneState state)
	{
		PLAYER_MONSYO monsyo_data = player.monsyo_data;
		if (monsyo_data != null)
		{
			int num = Math.Clamp(state.ActiveIndex, 0, 2);
			int num2 = ((num == 0) ? 1 : 0);
			int num3 = ((num == 2) ? 1 : 2);
			byte num4 = ValidateRuneId(state.RuneIds[num2]);
			int num5 = ValidateRuneId(state.RuneIds[num3]);
			int num6 = Math.Clamp((int)state.SoulEaterLevel, 0, 8);
			int num7 = num4 + 34 * num5 + 1156 * num + 3468 * num6;
			monsyo_data.monsyo_exp = (ushort)(0x8000u | (uint)num7);
		}
	}

	internal static int GetUnlockedSlotCount(PLAYER_BASE player)
	{
		int magic = GetMagic(player);
		if (magic >= Plugin.ThirdSlotMag.Value)
		{
			return 3;
		}
		if (magic >= Plugin.SecondSlotMag.Value)
		{
			return 2;
		}
		return 1;
	}

	internal static int GetMagic(PLAYER_BASE player)
	{
		Il2CppStructArray<byte> noryoku = player.noryoku;
		if (noryoku == null || ((Il2CppArrayBase<byte>)(object)noryoku).Length <= 4)
		{
			return 0;
		}
		return ((Il2CppArrayBase<byte>)(object)noryoku)[4];
	}

	private static byte ValidateRuneId(byte runeId)
	{
		if (runeId >= 34)
		{
			return 0;
		}
		return runeId;
	}

	internal static string GetRuneName(byte runeId)
	{
		if (runeId == 0)
		{
			return "vide";
		}
		try
		{
			Il2CppReferenceArray<MONSYO_DATA> monsyo_data_table = G_monsyo_c_data.monsyo_data_table;
			if (monsyo_data_table != null && runeId < ((Il2CppArrayBase<MONSYO_DATA>)(object)monsyo_data_table).Length && ((Il2CppArrayBase<MONSYO_DATA>)(object)monsyo_data_table)[(int)runeId] != null)
			{
				string text = G1_string_id_h.G1_SID_MSG(((Il2CppArrayBase<MONSYO_DATA>)(object)monsyo_data_table)[(int)runeId].name);
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text;
				}
			}
		}
		catch
		{
		}
		return $"#{runeId}";
	}

	private static void Swap(byte[] values, int a, int b)
	{
		if (a != b)
		{
			(values[a], values[b]) = (values[b], values[a]);
		}
	}
}
