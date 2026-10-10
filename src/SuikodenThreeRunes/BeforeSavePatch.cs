using System;
using HarmonyLib;

namespace SuikodenThreeRunes;

[HarmonyPatch(typeof(SaveDataManager), "CopyGameWorkToSaveData")]
internal static class BeforeSavePatch
{
	private static bool _runtimeRestoreLogged;

	private static void Prefix(GAME_WORK game_work, out RuneGameSnapshot? __state)
	{
		__state = null;
		try
		{
			__state = RuneSlotService.SnapshotAndNormalizeForSave(game_work);
		}
		catch (Exception ex)
		{
			Plugin.ModLog.LogError((object)("Preparation de la sauvegarde 3 runes impossible: " + ex));
		}
	}

	// Finalizer (et non Postfix) : l'etat en jeu est restaure meme si la copie native echoue.
	private static Exception? Finalizer(Exception? __exception, GAME_WORK game_work, RuneGameSnapshot? __state)
	{
		try
		{
			if (RuneSlotService.RestoreAfterSaveCopy(game_work, __state) > 0 && !_runtimeRestoreLogged)
			{
				_runtimeRestoreLogged = true;
				Plugin.ModLog.LogInfo((object)"Copie de sauvegarde terminee : l'emplacement de rune actif a ete restaure en jeu.");
			}
		}
		catch (Exception ex)
		{
			Plugin.ModLog.LogError((object)("Restauration apres sauvegarde 3 runes impossible: " + ex));
		}
		return __exception;
	}
}
