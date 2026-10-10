using System;
using HarmonyLib;

namespace SuikodenThreeRunesPassives;

[HarmonyPatch(typeof(BattleBase), "calc_noryoku")]
internal static class R108GalePatch
{
	[HarmonyPrefix]
	[HarmonyPriority(800)]
	private static void Prefix(PLAYER_BASE player_base, out RuneSwapState __state)
	{
		__state = PassiveRuneSlots.PresentHiddenRuneToNative(player_base, Plugin.GaleRuneId.Value, "Rafale/Gale");
	}

	[HarmonyFinalizer]
	[HarmonyPriority(0)]
	private static Exception? Finalizer(Exception? __exception, RuneSwapState __state)
	{
		PassiveRuneSlots.Restore(__state);
		return __exception;
	}
}
