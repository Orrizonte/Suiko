using System;
using HarmonyLib;

namespace SuikodenThreeRunes;

[HarmonyPatch(typeof(GRInputManager), "IsRepeat", new Type[] { typeof(GRInputManager.Type) })]
internal static class RuneBattleNativeRepeatPatch
{
	[HarmonyPrefix]
	[HarmonyPriority(800)]
	private static bool Prefix(GRInputManager.Type __0, ref bool __result)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (!RuneBattleNativeEmblemFlow.TryOverrideInput(__0, out var value))
		{
			return true;
		}
		__result = value;
		return false;
	}
}
