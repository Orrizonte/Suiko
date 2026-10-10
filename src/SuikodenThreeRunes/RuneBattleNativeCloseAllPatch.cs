using System;
using HarmonyLib;

namespace SuikodenThreeRunes;

[HarmonyPatch(typeof(UIBattleManager), "CloseAllWindowUI", new Type[] { })]
internal static class RuneBattleNativeCloseAllPatch
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		RuneBattleNativeEmblemFlow.OnCloseAllWindows();
	}
}
