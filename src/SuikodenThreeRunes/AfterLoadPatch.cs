using System;
using HarmonyLib;

namespace SuikodenThreeRunes;

[HarmonyPatch(typeof(SaveDataManager), "CopySaveDataToGameWork")]
internal static class AfterLoadPatch
{
	private static void Prefix()
	{
		RuneSlotService.BeginLoad();
	}

	private static void Postfix(GAME_WORK game_work)
	{
		try
		{
			RuneBattleNativeEmblemFlow.Reset();
			RuneSlotService.PrepareGame(game_work);
		}
		catch (Exception ex)
		{
			Plugin.ModLog.LogError((object)("Lecture de l'etat 3 runes au chargement impossible: " + ex));
		}
	}

	// La copie native a echoue : ne pas rester bloque en mode chargement.
	private static Exception? Finalizer(Exception? __exception)
	{
		RuneSlotService.EndLoad();
		return __exception;
	}
}
