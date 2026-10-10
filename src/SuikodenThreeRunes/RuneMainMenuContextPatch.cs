using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace SuikodenThreeRunes;

[HarmonyPatch]
internal static class RuneMainMenuContextPatch
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		string[] array = new string[6] { "command_monsyo00", "command_monsyo01", "command_monsyo02", "command_monsyo03", "command_monsyo10", "command_monsyo99" };
		string[] array2 = array;
		foreach (string text in array2)
		{
			MethodInfo methodInfo = AccessTools.Method(typeof(G_cmd_c), text, Type.EmptyTypes, (Type[])null);
			if ((object)methodInfo != null)
			{
				yield return methodInfo;
			}
		}
	}

	[HarmonyPrefix]
	private static void Prefix()
	{
		RuneMainMenuUiPatch.MarkActive();
	}
}
