using System;
using HarmonyLib;
using UnityEngine;

namespace SuikodenThreeRunes;

[HarmonyPatch(typeof(G_cmd_c), "make_monsyo_window", new Type[]
{
	typeof(int),
	typeof(bool)
})]
internal static class RuneMainMenuUiPatch
{
	private static int _lastActiveFrame = -1000;

	internal static bool IsRuneMenuActive => Time.frameCount - _lastActiveFrame <= 3;

	internal static void MarkActive()
	{
		_lastActiveFrame = Time.frameCount;
	}

	[HarmonyPostfix]
	[HarmonyPriority(0)]
	private static void Postfix(int __0, bool __1)
	{
		MarkActive();
		G_cmd_c.COMMAND_WORK command_work = G_cmd_c.command_work;
		if (command_work != null && RuneSlotUi.TryGetPlayer(command_work.player_data, __0, out PLAYER_BASE player))
		{
			RuneSlotUi.DrawMainMenu(player, __1);
		}
	}
}
