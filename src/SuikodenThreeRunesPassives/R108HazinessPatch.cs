using System;
using HarmonyLib;

namespace SuikodenThreeRunesPassives;

[HarmonyPatch(typeof(BattleBase), "check_meichu")]
internal static class R108HazinessPatch
{
	[HarmonyPrefix]
	[HarmonyPriority(800)]
	private static void Prefix(out RuneSwapListState __state)
	{
		__state = PassiveRuneSlots.PresentHiddenRuneToAllBattleHolders(Plugin.HazinessRuneId.Value, "Brume/Haziness");
	}

	[HarmonyFinalizer]
	[HarmonyPriority(0)]
	private static Exception? Finalizer(Exception? __exception, RuneSwapListState __state)
	{
		PassiveRuneSlots.Restore(__state);
		return __exception;
	}
}
