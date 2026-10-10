using System;
using System.IO;
using System.Security.Cryptography;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace SuikodenThreeRunes;

[BepInPlugin("fr.suikodenhd.threerunes", "Suikoden I - Trois runes selon la MAG", "0.9.6-r156-prearm-emblem")]
[BepInDependency("d3xMachina.suikoden_fix", BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BasePlugin
{
	public const string Guid = "fr.suikodenhd.threerunes";

	public const string Name = "Suikoden I - Trois runes selon la MAG";

	public const string Version = "0.9.6-r156-prearm-emblem";

	private const string SupportedGameAssemblySha256 = "e093f175eaf3734d293ca0a236c652dd7fcc72ea5fa10d7b21103edf12f11c3a";

	internal static ManualLogSource ModLog;

	internal static ConfigEntry<int> SecondSlotMag;

	internal static ConfigEntry<int> ThirdSlotMag;

	internal static ConfigEntry<bool> ShowOverlay;

	internal static ConfigEntry<bool> ShowShopSlots;

	private Harmony? _harmony;

	public override void Load()
	{
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Expected O, but got Unknown
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Expected O, but got Unknown
		ModLog = ((BasePlugin)this).Log;
		if (IsSupportedGameBuild())
		{
			SecondSlotMag = ((BasePlugin)this).Config.Bind<int>("Emplacements", "MAG_Deuxieme_Rune", 60, "Statistique MAG minimale pour ouvrir le deuxieme emplacement (0 a 255).");
			ThirdSlotMag = ((BasePlugin)this).Config.Bind<int>("Emplacements", "MAG_Troisieme_Rune", 120, "Statistique MAG minimale pour ouvrir le troisieme emplacement (0 a 255).");
			ShowOverlay = ((BasePlugin)this).Config.Bind<bool>("Affichage", "Afficher_Indication", true, "Affiche brievement la rune active, les trois emplacements et la MAG.");
			ShowShopSlots = ((BasePlugin)this).Config.Bind<bool>("Affichage", "Afficher_3_Emplacements_Boutique", true, "Affiche les trois emplacements et leur selection dans la boutique de runes.");
			NormalizeThresholds();
			RuneSlotService.Reset();
			_harmony = new Harmony("fr.suikodenhd.threerunes");
			_harmony.PatchAll(typeof(Plugin).Assembly);
			((BasePlugin)this).AddComponent<RuneController>();
			ManualLogSource modLog = ModLog;
			bool flag = default(bool);
			BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(24, 4, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>("Suikoden I - Trois runes selon la MAG");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>("0.9.6-r156-prearm-emblem");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" charge. Seuils MAG: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(SecondSlotMag.Value);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("/");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(ThirdSlotMag.Value);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
			}
			modLog.LogInfo(val);
			ModLog.LogInfo((object)"R156 CLEAN : UI R148/R151 conservee; aucune nouvelle fenetre. Le meme m_emblem natif reste le selecteur 3-runes.");
			ModLog.LogInfo((object)"R156 CLEAN : Pie-grieche active utilise temporairement une rune magique secondaire comme carrier natif pour rouvrir le meme selecteur 3-runes; stockage et ActiveIndex restent inchanges.");
			ModLog.LogInfo((object)"R156 CLEAN : si le selecteur attend B mais que le dispatcher est sur command_magic02/10, ce seul handler est route vers command_magic01 natif afin que Cancel soit reellement lu.");
			ModLog.LogInfo((object)"R156 CLEAN : le vrai m_emblem est pre-arme des le A sur Runes puis revalide au prefix OpenWindowUI(Emblem), avant la toute premiere animation; retour Eau/Magic inchange.");
			ModLog.LogInfo((object)"R156 DIRECT-RUNTIME : Pie-grieche garde le stockage 3-runes en memoire mais presente monsyo_exp=0 au moteur natif pendant ciblage/execution; garde player_magic_sub installee.");
			ModLog.LogInfo((object)"R156 PIE-GRIECHE : c_item est maintenu a 0 pendant le chemin direct natif; aucune autre donnee de commande n'est forcee. UI R148/R151 conservee.");
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
			ModLog.LogError((object)("Version du jeu non prise en charge. Le mod Trois runes reste desactive pour proteger la partie (GameAssembly SHA-256: " + text + ")."));
		}
		catch (Exception ex)
		{
			ManualLogSource modLog = ModLog;
			bool flag = default(bool);
			BepInExErrorLogInterpolatedStringHandler val = new BepInExErrorLogInterpolatedStringHandler(61, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("Verification de la version du jeu impossible; mod desactive: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(ex.Message);
			}
			modLog.LogError(val);
		}
		return false;
	}

	private static void NormalizeThresholds()
	{
		SecondSlotMag.Value = Math.Clamp(SecondSlotMag.Value, 0, 255);
		ThirdSlotMag.Value = Math.Clamp(ThirdSlotMag.Value, SecondSlotMag.Value, 255);
	}
}
