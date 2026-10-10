using System;
using System.IO;
using System.Security.Cryptography;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace SuikodenThreeRunesPassives;

[BepInPlugin("fr.suikodenhd.threerunes.passives", "Suikoden I - Trois runes - Passifs 3 slots", "0.4.6-r156-passive-router")]
[BepInDependency("fr.suikodenhd.threerunes", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("d3xMachina.suikoden_fix", BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BasePlugin
{
	public const string Guid = "fr.suikodenhd.threerunes.passives";

	public const string Name = "Suikoden I - Trois runes - Passifs 3 slots";

	public const string Version = "0.4.6-r156-passive-router";

	private const string SupportedGameAssemblySha256 = "e093f175eaf3734d293ca0a236c652dd7fcc72ea5fa10d7b21103edf12f11c3a";

	internal static ManualLogSource ModLog;

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

	private Harmony? _harmony;

	public override void Load()
	{
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0256: Expected O, but got Unknown
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_028b: Expected O, but got Unknown
		ModLog = ((BasePlugin)this).Log;
		if (IsSupportedGameBuild())
		{
			EnableAll = ((BasePlugin)this).Config.Bind<bool>("General", "Activer_passifs_3_slots", true, "Toutes les runes passives equipees dans un slot debloque sont prises en compte, sans dependance au slot actif.");
			LogFirstActivation = ((BasePlugin)this).Config.Bind<bool>("Diagnostic", "Journaliser_premiere_activation", true, "Ecrit une ligne R108 la premiere fois qu'une rune passive secondaire est routee vers son point d'effet natif.");
			EnableExperimentalFieldPassives = ((BasePlugin)this).Config.Bind<bool>("General", "Passifs_terrain_experimentaux", true, "Active le routage terrain pour Holy/True Holy/Champion/Sunbeam quand un point natif compatible est detecte. Aucun patch binaire permanent n'est effectue.");
			DoubleBeatRuneId = ((BasePlugin)this).Config.Bind<int>("IDs", "Rune_Double_Beat", 14, "ID natif Double-Beat/Double-Jab.");
			KillerRuneId = ((BasePlugin)this).Config.Bind<int>("IDs", "Rune_Tueur_Killer", 15, "ID natif Killer/Tueur.");
			CounterRuneId = ((BasePlugin)this).Config.Bind<int>("IDs", "Rune_Contre_Counter", 16, "ID natif Counter/Contre.");
			SparkRuneId = ((BasePlugin)this).Config.Bind<int>("IDs", "Rune_Spark", 17, "ID natif Spark.");
			HazinessRuneId = ((BasePlugin)this).Config.Bind<int>("IDs", "Rune_Brume_Haziness", 18, "ID natif Haziness/Brume.");
			GaleRuneId = ((BasePlugin)this).Config.Bind<int>("IDs", "Rune_Rafale_Gale", 19, "ID natif Gale/Rafale.");
			SunbeamRuneId = ((BasePlugin)this).Config.Bind<int>("IDs", "Rune_Sunbeam", 20, "ID natif Sunbeam.");
			HolyRuneId = ((BasePlugin)this).Config.Bind<int>("IDs", "Rune_Holy", 21, "ID natif Holy.");
			FortuneRuneId = ((BasePlugin)this).Config.Bind<int>("IDs", "Rune_Fortune", 22, "ID natif Fortune.");
			ProsperityRuneId = ((BasePlugin)this).Config.Bind<int>("IDs", "Rune_Prosperity", 23, "ID natif Prosperity.");
			ChampionRuneId = ((BasePlugin)this).Config.Bind<int>("IDs", "Rune_Champion", 24, "ID natif Champion's.");
			TurtleRuneId = ((BasePlugin)this).Config.Bind<int>("IDs", "Rune_Turtle", 25, "ID natif Turtle.");
			PheroRuneId = ((BasePlugin)this).Config.Bind<int>("IDs", "Rune_Phero", 26, "ID natif Phero.");
			TrueHolyRuneId = ((BasePlugin)this).Config.Bind<int>("IDs", "Rune_True_Holy", 33, "ID natif True Holy/High-Speed.");
			PassiveRuneSlots.RefreshThresholdsFromMainPlugin();
			_harmony = new Harmony("fr.suikodenhd.threerunes.passives");
			_harmony.PatchAll(typeof(Plugin).Assembly);
			PassiveDynamicHooks.Install(_harmony);
			ManualLogSource modLog = ModLog;
			bool flag = default(bool);
			BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(9, 2, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>("Suikoden I - Trois runes - Passifs 3 slots");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>("0.4.6-r156-passive-router");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" charge.");
			}
			modLog.LogInfo(val);
			ModLog.LogInfo((object)"R108 : R107 n'est pas remplace. Le menu 3 runes, la boutique et Rune -> Magic restent ceux du mod principal.");
			ModLog.LogInfo((object)"R108 : les passifs sont lus dans TOUS les slots debloques; le slot selectionne ne sert plus a determiner si un passif existe.");
			ModLog.LogInfo((object)"R108 : routeurs natifs installes pour Double-Beat, Killer, Counter, Spark, Brume, Rafale, Fortune, Prosperity; Sunbeam combat est complete par un fallback sans doubler l'effet natif.");
		}
	}

	private static bool IsSupportedGameBuild()
	{
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected O, but got Unknown
		try
		{
			using FileStream inputStream = File.OpenRead(Path.Combine(Paths.GameRootPath, "GameAssembly.dll"));
			using SHA256 sHA = SHA256.Create();
			string text = Convert.ToHexString(sHA.ComputeHash(inputStream)).ToLowerInvariant();
			if (text == "e093f175eaf3734d293ca0a236c652dd7fcc72ea5fa10d7b21103edf12f11c3a")
			{
				return true;
			}
			ModLog.LogError((object)("Version du jeu non prise en charge. L'addon passifs 3 slots reste desactive pour proteger la partie (GameAssembly SHA-256: " + text + ")."));
		}
		catch (Exception ex)
		{
			ManualLogSource modLog = ModLog;
			bool flag = default(bool);
			BepInExErrorLogInterpolatedStringHandler val = new BepInExErrorLogInterpolatedStringHandler(63, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("Verification de la version du jeu impossible; addon desactive: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
			}
			modLog.LogError(val);
		}
		return false;
	}
}
