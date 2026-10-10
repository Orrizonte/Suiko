using HarmonyLib;

namespace SuikodenThreeRunes;

[HarmonyPatch(typeof(SaveDataManager), "CopyGameWorkToSaveData")]
internal static class BeforeSavePatch
{
	private static bool _runtimeRestoreLogged;

	private static void Prefix(GAME_WORK game_work, out RuneGameSnapshot __state)
	{
		__state = RuneSlotService.SnapshotAndNormalizeForSave(game_work);
	}

	private static void Postfix(GAME_WORK game_work, RuneGameSnapshot __state)
	{
		if (RuneSlotService.RestoreAfterSaveCopy(game_work, __state) > 0 && !_runtimeRestoreLogged)
		{
			_runtimeRestoreLogged = true;
			Plugin.ModLog.LogInfo((object)"Copie de sauvegarde terminee : l'emplacement de rune actif a ete restaure en jeu.");
		}
	}
}
