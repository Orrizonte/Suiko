using System;
using HarmonyLib;
using Share.Battle;

namespace SuikodenThreeRunes;

[HarmonyPatch(typeof(UIBattleManager), "OpenWindowUI", new Type[]
{
	typeof(WindowType),
	typeof(bool)
})]
internal static class RuneBattleNativeWindowOpenPatch
{
	[HarmonyPrefix]
	[HarmonyPriority(800)]
	private static void Prefix(WindowType __0, ref bool __1)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Invalid comparison between Unknown and I4
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		if ((int)__0 == 5)
		{
			RuneBattleNativeEmblemFlow.PrepareNativeEmblemOpen();
		}
		else if ((int)__0 == 8)
		{
			RuneBattleNativeEmblemFlow.PrepareMagicOpen(ref __1);
		}
	}

	[HarmonyPostfix]
	[HarmonyPriority(0)]
	private static void Postfix(WindowType __0)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Invalid comparison between Unknown and I4
		if ((int)__0 == 8)
		{
			RuneBattleNativeEmblemFlow.FinishMagicOpen();
		}
	}
}
