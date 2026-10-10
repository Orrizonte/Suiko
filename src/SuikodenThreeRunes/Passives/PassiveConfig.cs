using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace SuikodenThreeRunes.Passives;

// Ancien addon "SuikodenThreeRunesPassives", desormais integre au mod principal.
// Les reglages sont dans le meme fichier de config que le mod (sections Passifs / Passifs_IDs).
internal static class PassiveConfig
{
	internal const string LegacyAddonFile = "SuikodenThreeRunesPassives.dll";

	internal static ManualLogSource ModLog => global::SuikodenThreeRunes.Plugin.ModLog;

	// Faux si l'ancien addon separe est encore installe : il garde la main pour ne pas doubler les effets.
	internal static bool Active { get; private set; }

	internal static ConfigEntry<bool> EnableAll;

	internal static ConfigEntry<bool> LogFirstActivation;

	internal static ConfigEntry<bool> EnableExperimentalFieldPassives;

	internal static ConfigEntry<int> DoubleBeatRuneId;

	internal static ConfigEntry<int> KillerRuneId;

	internal static ConfigEntry<int> CounterRuneId;

	internal static ConfigEntry<int> SparkRuneId;

	internal static ConfigEntry<int> HazinessRuneId;

	internal static ConfigEntry<int> GaleRuneId;

	internal static ConfigEntry<int> SunbeamRuneId;

	internal static ConfigEntry<int> HolyRuneId;

	internal static ConfigEntry<int> FortuneRuneId;

	internal static ConfigEntry<int> ProsperityRuneId;

	internal static ConfigEntry<int> ChampionRuneId;

	internal static ConfigEntry<int> TurtleRuneId;

	internal static ConfigEntry<int> PheroRuneId;

	internal static ConfigEntry<int> TrueHolyRuneId;

	internal static void Bind(ConfigFile config)
	{
		EnableAll = config.Bind("Passifs", "Activer_passifs_3_slots", true, "Toutes les runes passives equipees dans un slot debloque sont prises en compte, sans dependance au slot actif.");
		LogFirstActivation = config.Bind("Passifs", "Journaliser_premiere_activation", true, "Ecrit une ligne R108 la premiere fois qu'une rune passive secondaire est routee vers son point d'effet natif.");
		EnableExperimentalFieldPassives = config.Bind("Passifs", "Passifs_terrain_experimentaux", true, "Diagnostic : liste dans le log les points natifs terrain candidats (Holy/True Holy/Champion/Sunbeam). Aucun patch n'est applique.");
		DoubleBeatRuneId = config.Bind("Passifs_IDs", "Rune_Double_Beat", 14, "ID natif Double-Beat/Double-Jab.");
		KillerRuneId = config.Bind("Passifs_IDs", "Rune_Tueur_Killer", 15, "ID natif Killer/Tueur.");
		CounterRuneId = config.Bind("Passifs_IDs", "Rune_Contre_Counter", 16, "ID natif Counter/Contre.");
		SparkRuneId = config.Bind("Passifs_IDs", "Rune_Spark", 17, "ID natif Spark.");
		HazinessRuneId = config.Bind("Passifs_IDs", "Rune_Brume_Haziness", 18, "ID natif Haziness/Brume.");
		GaleRuneId = config.Bind("Passifs_IDs", "Rune_Rafale_Gale", 19, "ID natif Gale/Rafale.");
		SunbeamRuneId = config.Bind("Passifs_IDs", "Rune_Sunbeam", 20, "ID natif Sunbeam.");
		HolyRuneId = config.Bind("Passifs_IDs", "Rune_Holy", 21, "ID natif Holy.");
		FortuneRuneId = config.Bind("Passifs_IDs", "Rune_Fortune", 22, "ID natif Fortune.");
		ProsperityRuneId = config.Bind("Passifs_IDs", "Rune_Prosperity", 23, "ID natif Prosperity.");
		ChampionRuneId = config.Bind("Passifs_IDs", "Rune_Champion", 24, "ID natif Champion's.");
		TurtleRuneId = config.Bind("Passifs_IDs", "Rune_Turtle", 25, "ID natif Turtle.");
		PheroRuneId = config.Bind("Passifs_IDs", "Rune_Phero", 26, "ID natif Phero.");
		TrueHolyRuneId = config.Bind("Passifs_IDs", "Rune_True_Holy", 33, "ID natif True Holy/High-Speed.");
	}

	// A appeler apres SafePatchAll (les patchs R108 par attribut sont dans cette assembly).
	internal static void Install(Harmony harmony)
	{
		string text = FindLegacyAddon();
		if (text != null)
		{
			Active = false;
			ModLog.LogWarning((object)("Passifs integres DESACTIVES : l'ancien addon separe est encore installe (" + text + "). Supprimez-le, les passifs font maintenant partie de SuikodenThreeRunes.dll."));
			return;
		}
		Active = true;
		PassiveDynamicHooks.Install(harmony);
		ModLog.LogInfo((object)"R108 : passifs 3 emplacements integres; lus dans TOUS les slots debloques (Double-Beat, Tueur, Contre, Tortue, Spark, Brume, Rafale, Fortune, Prosperity, Phero, Sunbeam).");
	}

	private static string? FindLegacyAddon()
	{
		try
		{
			string pluginPath = Paths.PluginPath;
			if (!string.IsNullOrEmpty(pluginPath) && Directory.Exists(pluginPath))
			{
				string[] files = Directory.GetFiles(pluginPath, LegacyAddonFile, SearchOption.AllDirectories);
				if (files.Length != 0)
				{
					return files[0];
				}
			}
		}
		catch (Exception ex)
		{
			ModLog.LogWarning((object)("Recherche de l'ancien addon des passifs impossible : " + ex.Message));
		}
		return null;
	}
}
