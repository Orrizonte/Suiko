using System;
using HarmonyLib;

namespace SuikodenThreeRunesPassives;

[HarmonyPatch(typeof(BattleBase), "calc_exp")]
internal static class R108FortunePatch
{
	[HarmonyPrefix]
	[HarmonyPriority(800)]
	private static void Prefix(out RuneSwapListState __state)
	{
		__state = PassiveRuneSlots.PresentHiddenRuneToAllBattleHolders(Plugin.FortuneRuneId.Value, "Fortune");
	}

	[HarmonyFinalizer]
	[HarmonyPriority(0)]
	private static Exception? Finalizer(Exception? __exception, RuneSwapListState __state)
	{
		PassiveRuneSlots.Restore(__state);
		return __exception;
	}
}
