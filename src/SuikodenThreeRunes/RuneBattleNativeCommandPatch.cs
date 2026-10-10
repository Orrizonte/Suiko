using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace SuikodenThreeRunes;

[HarmonyPatch]
internal static class RuneBattleNativeCommandPatch
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		string[] array = new string[6] { "command_magic", "command_magic01", "command_magic02", "command_magic10", "command_magic20", "command_magic99" };
		string[] array2 = array;
		foreach (string text in array2)
		{
			MethodInfo methodInfo = AccessTools.Method(typeof(BattleBase), text, new Type[1] { typeof(int) }, (Type[])null);
			if ((object)methodInfo != null)
			{
				yield return methodInfo;
			}
		}
	}

	[HarmonyPrefix]
	[HarmonyPriority(800)]
	private static bool Prefix(MethodBase __originalMethod, int __0, ref int __result, out DirectRuneDiagnostic.Snap __state)
	{
		__state = DirectRuneDiagnostic.Begin(__originalMethod);
		RuneBattleNativeEmblemFlow.EnterMagicMethod(__originalMethod);
		RuneBattleNativeEmblemFlow.EnterMagicCommand();
		if (RuneBattleNativeEmblemFlow.ShouldRouteRuneSelectorCancelToMagic01(__originalMethod) && RuneBattleNativeEmblemFlow.TryRunNativeRuneSelectorCancel(__0, __originalMethod, out var result))
		{
			__result = result;
			return false;
		}
		if (RuneBattleNativeEmblemFlow.ShouldUseNativeSpellHandler(__originalMethod) && RuneBattleNativeEmblemFlow.TryRunNativeSpellSelection(__0, out var result2))
		{
			RuneBattleNativeEmblemFlow.NoteNativeSpellHandlerReroute();
			__result = result2;
			return false;
		}
		return true;
	}

	[HarmonyPostfix]
	[HarmonyPriority(0)]
	private static void Postfix(MethodBase __originalMethod, int __0, int __result, DirectRuneDiagnostic.Snap __state)
	{
		RuneBattleNativeEmblemFlow.ExitMagicCommand();
		RuneBattleNativeEmblemFlow.ExitMagicMethod(__originalMethod);
		DirectRuneDiagnostic.End(__originalMethod, __result, __state);
	}
}
