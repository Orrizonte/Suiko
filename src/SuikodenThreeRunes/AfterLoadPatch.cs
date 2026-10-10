using HarmonyLib;

namespace SuikodenThreeRunes;

[HarmonyPatch(typeof(SaveDataManager), "CopySaveDataToGameWork")]
internal static class AfterLoadPatch
{
	private static void Postfix(GAME_WORK game_work)
	{
		RuneSlotService.Reset();
		RuneSlotService.PrepareGame(game_work);
	}
}
