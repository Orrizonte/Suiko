using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace SuikodenThreeRunesPassives;

internal static class PassiveRuneSlots
{
	private const int RuneRadix = 34;

	private const int PairStates = 1156;

	private const int LevelStates = 3468;

	private const ushort Signature = 32768;

	private const ushort PayloadMask = 32767;

	private const int MagicStatIndex = 4;

	private static int _secondSlotMag = 60;

	private static int _thirdSlotMag = 120;

	private static readonly HashSet<string> LoggedActivations = new HashSet<string>(StringComparer.Ordinal);

	internal static void RefreshThresholdsFromMainPlugin()
	{
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Expected O, but got Unknown
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Expected O, but got Unknown
		bool flag = default(bool);
		try
		{
			Type type = (from a in AppDomain.CurrentDomain.GetAssemblies()
				select a.GetType("SuikodenThreeRunes.Plugin", throwOnError: false)).FirstOrDefault((Type t) => (object)t != null);
			if ((object)type != null)
			{
				BindingFlags bindingAttr = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
				if (type.GetField("SecondSlotMag", bindingAttr)?.GetValue(null) is ConfigEntry<int> val)
				{
					_secondSlotMag = Math.Clamp(val.Value, 0, 255);
				}
				if (type.GetField("ThirdSlotMag", bindingAttr)?.GetValue(null) is ConfigEntry<int> val2)
				{
					_thirdSlotMag = Math.Clamp(val2.Value, _secondSlotMag, 255);
				}
				ManualLogSource modLog = Plugin.ModLog;
				BepInExInfoLogInterpolatedStringHandler val3 = new BepInExInfoLogInterpolatedStringHandler(52, 2, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val3).AppendLiteral("R108 : seuils relus depuis le mod principal = ");
					((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<int>(_secondSlotMag);
					((BepInExLogInterpolatedStringHandler)val3).AppendLiteral("/");
					((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<int>(_thirdSlotMag);
					((BepInExLogInterpolatedStringHandler)val3).AppendLiteral(" MAG.");
				}
				modLog.LogInfo(val3);
			}
		}
		catch (Exception ex)
		{
			ManualLogSource modLog2 = Plugin.ModLog;
			BepInExWarningLogInterpolatedStringHandler val4 = new BepInExWarningLogInterpolatedStringHandler(61, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val4).AppendLiteral("R108 : seuils du mod principal illisibles; 60/120 utilises : ");
				((BepInExLogInterpolatedStringHandler)val4).AppendFormatted<string>(ex.Message);
			}
			modLog2.LogWarning(val4);
		}
	}

	internal static bool HasEquippedRune(PLAYER_BASE? player, int runeId)
	{
		if (!Plugin.EnableAll.Value || ((player != null) ? player.monsyo_data : null) == null || runeId <= 0 || runeId >= 34)
		{
			return false;
		}
		PackedRuneState packedRuneState = Decode(player);
		int unlockedSlotCount = GetUnlockedSlotCount(player);
		for (int i = 0; i < unlockedSlotCount && i < packedRuneState.RuneIds.Length; i++)
		{
			if (packedRuneState.RuneIds[i] == runeId)
			{
				return true;
			}
		}
		return false;
	}

	internal static bool HasHiddenRune(PLAYER_BASE? player, int runeId)
	{
		if (((player != null) ? player.monsyo_data : null) != null && player.monsyo_data.monsyo_id != runeId)
		{
			return HasEquippedRune(player, runeId);
		}
		return false;
	}

	internal static byte[] GetEquippedRuneIds(PLAYER_BASE? player)
	{
		if (((player != null) ? player.monsyo_data : null) == null)
		{
			return Array.Empty<byte>();
		}
		PackedRuneState packedRuneState = Decode(player);
		int num = Math.Min(GetUnlockedSlotCount(player), packedRuneState.RuneIds.Length);
		byte[] array = new byte[num];
		Array.Copy(packedRuneState.RuneIds, array, num);
		return array;
	}

	internal static RuneSwapState PresentHiddenRuneToNative(PLAYER_BASE? player, int runeId, string effectName)
	{
		RuneSwapState result = default(RuneSwapState);
		if (!HasHiddenRune(player, runeId) || ((player != null) ? player.monsyo_data : null) == null)
		{
			return result;
		}
		PLAYER_MONSYO monsyo_data = player.monsyo_data;
		result.Player = player;
		result.OriginalId = monsyo_data.monsyo_id;
		result.OriginalLevel = monsyo_data.monsyo_level;
		result.PresentedRuneId = runeId;
		result.Changed = true;
		monsyo_data.monsyo_id = (byte)runeId;
		monsyo_data.monsyo_level = 0;
		LogActivationOnce(player, runeId, effectName, result.OriginalId);
		return result;
	}

	internal static RuneSwapListState PresentHiddenRuneToAllBattleHolders(int runeId, string effectName)
	{
		RuneSwapListState runeSwapListState = new RuneSwapListState();
		foreach (PLAYER_BASE battlePlayer in GetBattlePlayers())
		{
			RuneSwapState item = PresentHiddenRuneToNative(battlePlayer, runeId, effectName);
			if (item.Changed)
			{
				runeSwapListState.Swaps.Add(item);
			}
		}
		return runeSwapListState;
	}

	internal static void Restore(RuneSwapState state)
	{
		if (state.Changed)
		{
			PLAYER_BASE? player = state.Player;
			if (((player != null) ? player.monsyo_data : null) != null)
			{
				state.Player.monsyo_data.monsyo_id = state.OriginalId;
				state.Player.monsyo_data.monsyo_level = state.OriginalLevel;
			}
		}
	}

	internal static void Restore(RuneSwapListState? state)
	{
		if (state != null)
		{
			for (int num = state.Swaps.Count - 1; num >= 0; num--)
			{
				Restore(state.Swaps[num]);
			}
		}
	}

	internal static bool TryGetBattlePlayer(int index, out PLAYER_BASE? player)
	{
		player = null;
		try
		{
			BATTLE_WORK battle_work = BattleBase.battle_work;
			Il2CppReferenceArray<P_BATTLE_DATA> val = ((battle_work != null) ? battle_work.p_battle_data : null);
			if (val == null || index < 0 || index >= ((Il2CppArrayBase<P_BATTLE_DATA>)(object)val).Length)
			{
				return false;
			}
			P_BATTLE_DATA obj = ((Il2CppArrayBase<P_BATTLE_DATA>)(object)val)[index];
			object obj2;
			if (obj == null)
			{
				obj2 = null;
			}
			else
			{
				PLAYER_DATA player_data = obj.player_data;
				obj2 = ((player_data != null) ? player_data.player_base : null);
			}
			player = (PLAYER_BASE?)obj2;
			return player != null;
		}
		catch
		{
			player = null;
			return false;
		}
	}

	internal static bool TryResolveBattlePlayerFromArgs(int a, int b, out PLAYER_BASE? player, bool preferSecond = false)
	{
		if (preferSecond && TryGetBattlePlayer(b, out player) && player != null)
		{
			return true;
		}
		if (TryGetBattlePlayer(a, out player) && player != null)
		{
			return true;
		}
		if (!preferSecond && TryGetBattlePlayer(b, out player) && player != null)
		{
			return true;
		}
		player = null;
		return false;
	}

	internal static IEnumerable<PLAYER_BASE> GetBattlePlayers()
	{
		BATTLE_WORK battle_work = BattleBase.battle_work;
		Il2CppReferenceArray<P_BATTLE_DATA> party = ((battle_work != null) ? battle_work.p_battle_data : null);
		if (party == null)
		{
			yield break;
		}
		for (int i = 0; i < ((Il2CppArrayBase<P_BATTLE_DATA>)(object)party).Length; i++)
		{
			P_BATTLE_DATA obj = ((Il2CppArrayBase<P_BATTLE_DATA>)(object)party)[i];
			object obj2;
			if (obj == null)
			{
				obj2 = null;
			}
			else
			{
				PLAYER_DATA player_data = obj.player_data;
				obj2 = ((player_data != null) ? player_data.player_base : null);
			}
			PLAYER_BASE val = (PLAYER_BASE)obj2;
			if (val != null)
			{
				yield return val;
			}
		}
	}

	internal static bool PartyHasRune(int runeId)
	{
		foreach (PLAYER_BASE battlePlayer in GetBattlePlayers())
		{
			if (HasEquippedRune(battlePlayer, runeId))
			{
				return true;
			}
		}
		return false;
	}

	internal static int FindBattleIndex(PLAYER_BASE player)
	{
		try
		{
			BATTLE_WORK battle_work = BattleBase.battle_work;
			Il2CppReferenceArray<P_BATTLE_DATA> val = ((battle_work != null) ? battle_work.p_battle_data : null);
			if (val == null)
			{
				return -1;
			}
			for (int i = 0; i < ((Il2CppArrayBase<P_BATTLE_DATA>)(object)val).Length; i++)
			{
				P_BATTLE_DATA obj = ((Il2CppArrayBase<P_BATTLE_DATA>)(object)val)[i];
				object obj2;
				if (obj == null)
				{
					obj2 = null;
				}
				else
				{
					PLAYER_DATA player_data = obj.player_data;
					obj2 = ((player_data != null) ? player_data.player_base : null);
				}
				PLAYER_BASE val2 = (PLAYER_BASE)obj2;
				if (val2 != null && val2.chara_no == player.chara_no)
				{
					return i;
				}
			}
		}
		catch
		{
		}
		return -1;
	}

	private static PackedRuneState Decode(PLAYER_BASE player)
	{
		PLAYER_MONSYO monsyo_data = player.monsyo_data;
		byte[] array = new byte[3];
		ushort monsyo_exp = monsyo_data.monsyo_exp;
		if ((monsyo_exp & 0x8000) == 0)
		{
			array[0] = ValidateRuneId(monsyo_data.monsyo_id);
			return new PackedRuneState(array, 0);
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
			array[0] = ValidateRuneId(monsyo_data.monsyo_id);
			return new PackedRuneState(array, 0);
		}
		array[num4] = ValidateRuneId(monsyo_data.monsyo_id);
		int num8 = ((num4 == 0) ? 1 : 0);
		int num9 = ((num4 == 2) ? 1 : 2);
		array[num8] = ValidateRuneId((byte)num7);
		array[num9] = ValidateRuneId((byte)num6);
		return new PackedRuneState(array, num4);
	}

	private static int GetUnlockedSlotCount(PLAYER_BASE player)
	{
		try
		{
			Il2CppStructArray<byte> noryoku = player.noryoku;
			int num = ((noryoku != null && ((Il2CppArrayBase<byte>)(object)noryoku).Length > 4) ? ((Il2CppArrayBase<byte>)(object)noryoku)[4] : 0);
			if (num >= _thirdSlotMag)
			{
				return 3;
			}
			if (num >= _secondSlotMag)
			{
				return 2;
			}
		}
		catch
		{
		}
		return 1;
	}

	private static byte ValidateRuneId(byte id)
	{
		if (id >= 34)
		{
			return 0;
		}
		return id;
	}

	private static void LogActivationOnce(PLAYER_BASE player, int runeId, string effectName, byte originalId)
	{
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Expected O, but got Unknown
		if (!Plugin.LogFirstActivation.Value)
		{
			return;
		}
		string item = $"{player.chara_no}:{runeId}:{effectName}";
		if (LoggedActivations.Add(item))
		{
			string text = string.Join(",", from v in GetEquippedRuneIds(player)
				select v.ToString());
			ManualLogSource modLog = Plugin.ModLog;
			bool flag = default(bool);
			BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(143, 5, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R108 PASSIF 3-SLOTS - ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(effectName);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(": chara=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(player.chara_no);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", rune=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(runeId);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", slots=[");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(text);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("], active=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<byte>(originalId);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; rune secondaire presentee uniquement pendant son point d'effet puis active restauree.");
			}
			modLog.LogInfo(val);
		}
	}
}
