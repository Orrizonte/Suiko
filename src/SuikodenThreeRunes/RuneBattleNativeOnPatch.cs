using System;
using HarmonyLib;

namespace SuikodenThreeRunes;

[HarmonyPatch(typeof(GRInputManager), "IsOn", new Type[] { typeof(GRInputManager.Type) })]
internal static class RuneBattleNativeOnPatch
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

	[HarmonyPostfix]
	[HarmonyPriority(0)]
	private static void Postfix(GRInputManager.Type __0, ref bool __result)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Invalid comparison between Unknown and I4
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		if ((int)__0 == 5)
		{
			RuneBattleNativeEmblemFlow.TraceCancelInput("IsOn", __result);
		}
		RuneBattleNativeEmblemFlow.AfterNativeInput(__0, ref __result);
	}
}
