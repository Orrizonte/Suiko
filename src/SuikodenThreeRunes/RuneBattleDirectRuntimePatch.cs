using System;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace SuikodenThreeRunes;

[HarmonyPatch(typeof(BattleBase), "player_magic_sub", new Type[] { typeof(int) })]
internal static class RuneBattleDirectRuntimePatch
{
	[HarmonyPrefix]
	private static void Prefix(int __0)
	{
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Expected O, but got Unknown
		try
		{
			BATTLE_WORK battle_work = BattleBase.battle_work;
			if (((battle_work != null) ? battle_work.p_battle_data : null) == null || __0 < 0 || __0 >= ((Il2CppArrayBase<P_BATTLE_DATA>)(object)battle_work.p_battle_data).Length)
			{
				return;
			}
			P_BATTLE_DATA obj = ((Il2CppArrayBase<P_BATTLE_DATA>)(object)battle_work.p_battle_data)[__0];
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
			if (val == null)
			{
				return;
			}
			RuneState runeState = RuneSlotService.PeekStoredCombatState(val);
			int num = Math.Clamp(runeState.ActiveIndex, 0, 2);
			byte b = runeState.RuneIds[num];
			if (b != 9)
			{
				return;
			}
			RuneBattleNativeEmblemFlow.EnsureDirectRuneCommandItemZero(__0, "avant-player_magic_sub");
			if (RuneSlotService.PrepareDirectNativeRuntime(val, b, out var previousExp) && previousExp != 0)
			{
				ManualLogSource modLog = Plugin.ModLog;
				bool flag = default(bool);
				BepInExInfoLogInterpolatedStringHandler val2 = new BepInExInfoLogInterpolatedStringHandler(89, 4, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("R156 EXEC-GUARD player_magic_sub: player=");
					((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(__0);
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(", chara=");
					((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(val.chara_no);
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(", rune=");
					((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<byte>(b);
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("; ");
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("monsyo_exp=");
					((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<ushort>(previousExp);
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("->0 avant execution.");
				}
				modLog.LogInfo(val2);
			}
		}
		catch (Exception ex)
		{
			Plugin.ModLog.LogWarning((object)("R156 EXEC-GUARD impossible: " + ex.GetBaseException().Message));
		}
	}
}
