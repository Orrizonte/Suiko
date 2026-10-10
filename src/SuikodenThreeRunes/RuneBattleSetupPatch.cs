using HarmonyLib;

namespace SuikodenThreeRunes;

// R163 : debut de combat (creation des monstres) : les personnages repartent sur l'emplacement 1.
[HarmonyPatch(typeof(BattleBase), "make_monster_data")]
internal static class RuneBattleSetupPatch
{
	[HarmonyPrefix]
	private static void Prefix()
	{
		RuneSlotService.NoteBattleSetup();
	}
}
