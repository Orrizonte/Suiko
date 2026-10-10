using System;
using HarmonyLib;

namespace SuikodenThreeRunes;

[HarmonyPatch(typeof(BattleBase), "make_command_window", new Type[] { typeof(int) })]
internal static class RuneBattleNativeCommandWindowPatch
{
	[HarmonyPrefix]
	private static void Prefix(int __0)
	{
		RuneBattleNativeEmblemFlow.BeginCharacterCommand(__0);
	}
}
