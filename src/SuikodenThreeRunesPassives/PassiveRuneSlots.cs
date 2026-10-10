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
	private static readonly HashSet<string> LoggedActivations = new HashSet<string>(StringComparer.Ordinal);

	internal static void RefreshThresholdsFromMainPlugin()
	{
		Plugin.ModLog.LogInfo((object)$"R157 : etat des 3 runes lu directement dans le mod principal (seuils MAG {SuikodenThreeRunes.Plugin.SecondSlotMag.Value}/{SuikodenThreeRunes.Plugin.ThirdSlotMag.Value}).");
	}

	internal static bool HasEquippedRune(PLAYER_BASE? player, int runeId)
	{
		if (!Plugin.EnableAll.Value || ((player != null) ? player.monsyo_data : null) == null || runeId <= 0 || runeId >= 34)
		{
			return false;
		}
		return Array.IndexOf(SuikodenThreeRunes.RuneSlotService.GetUnlockedRuneIds(player), (byte)runeId) >= 0;
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
		return SuikodenThreeRunes.RuneSlotService.GetUnlockedRuneIds(player);
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
		SuikodenThreeRunes.RuneSlotService.BeginTemporaryNativeRune(player, (byte)runeId);
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
				SuikodenThreeRunes.RuneSlotService.EndTemporaryNativeRune(state.Player, (byte)state.PresentedRuneId);
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
