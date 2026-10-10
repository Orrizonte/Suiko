using System;
using HarmonyLib;

namespace SuikodenThreeRunesPassives;

[HarmonyPatch(typeof(BattleBase), "check_meichu")]
internal static class R108HazinessPatch
{
	[HarmonyPrefix]
	[HarmonyPriority(800)]
	private static void Prefix(int attack, int guard, out RuneSwapState __state)
	{
		__state = default(RuneSwapState);
		if (PassiveRuneSlots.TryGetBattlePlayer(guard, out PLAYER_BASE player) && player != null)
		{
			__state = PassiveRuneSlots.PresentHiddenRuneToNative(player, Plugin.HazinessRuneId.Value, "Brume/Haziness");
		}
	}

	[HarmonyFinalizer]
	[HarmonyPriority(0)]
	private static Exception? Finalizer(Exception? __exception, RuneSwapState __state)
	{
		PassiveRuneSlots.Restore(__state);
		return __exception;
	}
}
