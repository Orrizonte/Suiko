using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using Framework;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;
using Share.Battle;
using ShareUI;
using ShareUI.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace SuikodenThreeRunes;

internal static class RuneBattleNativeEmblemFlow
{
	private enum CombatStage
	{
		Idle,
		RuneSelector,
		Spells,
		ReturnToCommandRelease
	}

	private const int SlotCount = 3;

	private const int RuneCommandIndex = 2;

	private const byte ShrikeRuneId = 9;

	private static readonly string[] Labels = new string[3];

	private static readonly string[] Summaries = new string[3];

	private static readonly HashSet<byte> PassiveRuneIds = new HashSet<byte>
	{
		14, 15, 16, 17, 18, 19, 20, 21, 22, 23,
		24, 25, 26, 33
	};

	private static CombatStage _stage;

	private static int _playerNo = -1;

	private static int _playerCharacterNo = -1;

	private static string _playerSource = string.Empty;

	private static int _commandDepth;

	private static int _selectedSlot;

	private static int _lastCommandFrame = -1000;

	private static int _suppressDecideFrame = -1000;

	private static bool _confirmArmed;

	private static int _selectorIndexBeforeCommand = -1;

	private static int _selectorMoveDirection;

	private static bool _selectorUpHeld;

	private static bool _selectorDownHeld;

	private static bool _cancelBlockedUntilRelease;

	private static bool _selectorBackReleaseGuard;

	private static bool _nativeRuneCancelPending;

	private static bool _nativeRuneCancelInjected;

	private static bool _nativeRuneCancelCommandWindowSeen;

	private static int _nativeRuneCancelRequestFrame = -1000;

	private static int _nativeRuneCancelAttemptCount;

	private static bool _spellConfirmBlockedUntilRelease;

	private static bool _pendingRuneValidation;

	private static bool _returnToRunesRequested;

	private static bool _selectorOpenedFromMagicBack;

	private static bool _probingNativeInput;

	private static bool _rebuildingSpells;

	private static bool _windowMappingActive;

	private static bool _selectorLogged;

	private static bool _selectorNeedsSettledRebuild;

	private static bool _preRuneVisualPending;

	private static bool _selectorCarrierPrimed;

	private static int _selectorCarrierSlot = -1;

	private static byte _selectorCarrierRune;

	private static int _diagnosticTraceUntilFrame = -1000;

	private static int _diagBPressCount;

	private static int _diagAPressCount;

	private static int _diagUpPressCount;

	private static int _diagDownPressCount;

	private static int _diagFollowUntilFrame = -1000;

	private static int _diagLastFollowFrame = -1000;

	private static string _lastRenderedRowsSignature = string.Empty;

	private static bool _forceNativeSpellSelection;

	private static bool _directNativeRune;

	private static bool _spellHandlerRerouteLogged;

	private static int _magic01Depth;

	private static int _r52TraceBudget;

	private static bool _runeEntryStateCaptured;

	private static int _runeEntryWindowStep;

	private static int _runeEntryIndexLevel;

	private static int _runeEntryReturnValue;

	private static int _runeEntrySelectedItem;

	private static int _runeEntrySelectedMonster;

	private static bool _commandStateBeforeRuneCaptured;

	private static int _commandBeforeRuneWindowStep;

	private static int _commandBeforeRuneIndexLevel;

	private static int _commandBeforeRuneWindowIndex;

	private static int _commandBeforeRuneReturnValue;

	private static int _commandBeforeRuneSelectedItem;

	private static int _commandBeforeRuneSelectedMonster;

	private static int _commandBeforeRuneKoudou;

	private static int _commandBeforeRuneItem;

	private static int _commandBeforeRuneTarget;

	private static UIWindowList? _mappedWindowOwner;

	private static UIWindowList? _nativeMagicList;

	private static UIWindowBasis? _nativeMagicBasis;

	private static UISummary? _nativeMagicSummary;

	private static UIWindowList? _geometryOwner;

	private static Vector2 _originalWindowSize;

	private static Vector2 _originalContentsSize;

	private static float _originalUnfocusedHeight;

	private static float _expandedHeightDelta;

	private static readonly HashSet<int> CarrierWarningLogged = new HashSet<int>();

	// Vrai pendant le selecteur 3-runes / la liste des sorts / une presentation Pie-grieche.
	internal static bool IsBusy
	{
		get
		{
			if (_stage == CombatStage.Idle && !_selectorCarrierPrimed)
			{
				return _directNativeRune;
			}
			return true;
		}
	}

	private static bool IsPassiveRune(byte runeId)
	{
		return PassiveRuneIds.Contains(runeId);
	}

	private static bool IsDirectNativeRune(byte runeId)
	{
		return runeId == 9;
	}

	internal static void BeginCharacterCommand(int playerNo)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected O, but got Unknown
		if (_nativeRuneCancelPending && _nativeRuneCancelInjected)
		{
			_nativeRuneCancelCommandWindowSeen = true;
			if (TryGetPlayer(playerNo, out PLAYER_BASE player) && player != null)
			{
				_playerNo = playerNo;
				_playerCharacterNo = player.chara_no;
				_playerSource = "make_command_window/R156-native-cancel";
			}
			ManualLogSource modLog = Plugin.ModLog;
			bool flag = default(bool);
			BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(112, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R156 NATIVE-CANCEL: make_command_window(");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(playerNo);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(") observe pendant le Cancel natif; sortie Runes confirmee par le moteur.");
			}
			modLog.LogInfo(val);
		}
		else
		{
			Reset();
			if (TryGetPlayer(playerNo, out PLAYER_BASE player2) && player2 != null)
			{
				_playerNo = playerNo;
				_playerCharacterNo = player2.chara_no;
				_playerSource = "make_command_window";
			}
		}
	}

	internal static void EnterMagicMethod(MethodBase? method)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Expected O, but got Unknown
		bool flag = default(bool);
		if (_nativeRuneCancelPending && (object)method != null && _commandDepth == 0)
		{
			ManualLogSource modLog = Plugin.ModLog;
			BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(77, 6, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R156 NATIVE-CANCEL: entree ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(method.Name);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; tentative=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_nativeRuneCancelAttemptCount + 1);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; ");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("WINDOW(level=");
				BepInExInfoLogInterpolatedStringHandler obj = val;
				BattleBase.WINDOW_WORK window_work = BattleBase.window_work;
				((BepInExLogInterpolatedStringHandler)obj).AppendFormatted<int>((window_work != null) ? window_work.index_level : (-1));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",step=");
				BepInExInfoLogInterpolatedStringHandler obj2 = val;
				BattleBase.WINDOW_WORK window_work2 = BattleBase.window_work;
				((BepInExLogInterpolatedStringHandler)obj2).AppendFormatted<int>((window_work2 != null) ? window_work2.window_step : (-1));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("wi=");
				BepInExInfoLogInterpolatedStringHandler obj3 = val;
				BattleBase.WINDOW_WORK window_work3 = BattleBase.window_work;
				((BepInExLogInterpolatedStringHandler)obj3).AppendFormatted<int>((window_work3 != null) ? window_work3.window_index : (-1));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("); c_koudou=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(FmtDiag(ReadIntMember(GetPmEntry(_playerNo), "c_koudou")));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
			}
			modLog.LogInfo(val);
		}
		if (_stage == CombatStage.Spells && _r52TraceBudget > 0 && (object)method != null)
		{
			_r52TraceBudget--;
			ManualLogSource modLog2 = Plugin.ModLog;
			BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(74, 6, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R52 TRACE PASSIVE - ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(method.Name);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; player=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_playerNo);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; chara=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_playerCharacterNo);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; ");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("stage=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<CombatStage>(_stage);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; forceSpell=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(_forceNativeSpellSelection);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; window_index=");
				BepInExInfoLogInterpolatedStringHandler obj4 = val;
				BattleBase.WINDOW_WORK window_work4 = BattleBase.window_work;
				((BepInExLogInterpolatedStringHandler)obj4).AppendFormatted<int>((window_work4 != null) ? window_work4.window_index : (-1));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
			}
			modLog2.LogInfo(val);
		}
		if (method?.Name == "command_magic01")
		{
			_magic01Depth++;
		}
	}

	internal static void ExitMagicMethod(MethodBase? method)
	{
		if (method?.Name == "command_magic01" && _magic01Depth > 0)
		{
			_magic01Depth--;
		}
	}

	internal static bool ShouldRouteRuneSelectorCancelToMagic01(MethodBase? method)
	{
		string text = method?.Name;
		if (_nativeRuneCancelPending && _stage == CombatStage.RuneSelector)
		{
			if (!(text == "command_magic02"))
			{
				return text == "command_magic10";
			}
			return true;
		}
		return false;
	}

	internal static bool TryRunNativeRuneSelectorCancel(int stepLevel, MethodBase? sourceMethod, out int result)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected O, but got Unknown
		result = -1;
		try
		{
			MethodInfo methodInfo = AccessTools.Method(typeof(BattleBase), "command_magic01", new Type[1] { typeof(int) }, (Type[])null);
			if ((object)methodInfo == null)
			{
				return false;
			}
			ManualLogSource modLog = Plugin.ModLog;
			bool flag = default(bool);
			BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(96, 4, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R156 NATIVE-CANCEL ROUTE: ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(sourceMethod?.Name ?? "?");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" -> command_magic01 natif; ");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("WINDOW(level=");
				BattleBase.WINDOW_WORK window_work = BattleBase.window_work;
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>((window_work != null) ? window_work.index_level : (-1));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",step=");
				BattleBase.WINDOW_WORK window_work2 = BattleBase.window_work;
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>((window_work2 != null) ? window_work2.window_step : (-1));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("wi=");
				BattleBase.WINDOW_WORK window_work3 = BattleBase.window_work;
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>((window_work3 != null) ? window_work3.window_index : (-1));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("); aucun etat force.");
			}
			modLog.LogInfo(val);
			if (methodInfo.Invoke(null, new object[1] { stepLevel }) is int num)
			{
				result = num;
			}
			return true;
		}
		catch (Exception ex)
		{
			Plugin.ModLog.LogWarning((object)("R156 NATIVE-CANCEL ROUTE impossible; handler original conserve: " + ex.GetBaseException().Message));
			return false;
		}
	}

	internal static bool ShouldUseNativeSpellHandler(MethodBase? method)
	{
		string text = method?.Name;
		if ((text == "command_magic02" || text == "command_magic10") && _stage == CombatStage.Spells)
		{
			return _forceNativeSpellSelection;
		}
		return false;
	}

	internal static void NoteNativeSpellHandlerReroute()
	{
		if (!_spellHandlerRerouteLogged)
		{
			_spellHandlerRerouteLogged = true;
			Plugin.ModLog.LogInfo((object)"R101 R52-FIX - handler premature command_magic02/10 route vers command_magic01 tant qu'aucun sort VALIDE n'a ete choisi.");
		}
	}

	internal static bool TryRunNativeSpellSelection(int stepLevel, out int result)
	{
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Expected O, but got Unknown
		result = -1;
		try
		{
			MethodInfo methodInfo = AccessTools.Method(typeof(BattleBase), "command_magic01", new Type[1] { typeof(int) }, (Type[])null);
			if ((object)methodInfo == null)
			{
				return false;
			}
			BattleBase.WINDOW_WORK window_work = BattleBase.window_work;
			int num = ((window_work != null) ? window_work.selected_item : 0);
			BattleBase.WINDOW_WORK window_work2 = BattleBase.window_work;
			int num2 = ((window_work2 != null) ? window_work2.index_level : (-1));
			BattleBase.WINDOW_WORK window_work3 = BattleBase.window_work;
			int num3 = ((window_work3 != null) ? window_work3.window_step : (-1));
			if (methodInfo.Invoke(null, new object[1] { stepLevel }) is int num4)
			{
				result = num4;
			}
			BattleBase.WINDOW_WORK window_work4 = BattleBase.window_work;
			int num5 = ((window_work4 != null) ? window_work4.selected_item : 0);
			BattleBase.WINDOW_WORK window_work5 = BattleBase.window_work;
			int num6 = ((window_work5 != null) ? window_work5.index_level : (-1));
			BattleBase.WINDOW_WORK window_work6 = BattleBase.window_work;
			int num7 = ((window_work6 != null) ? window_work6.window_step : (-1));
			if (_forceNativeSpellSelection && num5 > 0 && (num5 != num || num6 != num2 || num7 != num3))
			{
				_forceNativeSpellSelection = false;
				ManualLogSource modLog = Plugin.ModLog;
				bool flag = default(bool);
				BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(111, 6, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R101 R52-FIX - sort valide confirme par le moteur: selected_item ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(num);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("->");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(num5);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", ");
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("level ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(num2);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("->");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(num6);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", step ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(num3);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("->");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(num7);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; ciblage vanilla libere.");
				}
				modLog.LogInfo(val);
			}
			return true;
		}
		catch (Exception ex)
		{
			Plugin.ModLog.LogWarning((object)("R101 R52-FIX - appel de command_magic01 impossible; command_magic10 natif conserve: " + ex.GetBaseException().Message));
			return false;
		}
	}

	internal static bool EnterMagicCommand()
	{
		_commandDepth++;
		if (_commandDepth != 1)
		{
			return true;
		}
		if (_stage == CombatStage.Idle)
		{
			RefreshCommandActorFromNativeState();
			TryPrimeDirectRuneSelectorCarrier("command_magic-prefix");
		}
		if (_directNativeRune && _playerNo >= 0)
		{
			EnsureDirectRuneCommandItemZero(_playerNo, "avant-command_magic");
		}
		_lastCommandFrame = Time.frameCount;
		return BeforeNativeCommand();
	}

	internal static void ExitMagicCommand()
	{
		if (_commandDepth > 0)
		{
			_commandDepth--;
		}
		if (_commandDepth == 0)
		{
			if (_nativeRuneCancelPending && _nativeRuneCancelInjected)
			{
				ObserveNativeRuneCancelAfterCommand();
			}
			else if (_pendingRuneValidation)
			{
				_pendingRuneValidation = false;
				ValidateRuneAndOpenSpells();
				Plugin.ModLog.LogInfo((object)"Combat natif - transition Emblem -> Magic effectuee en postfix; retour natif conserve.");
			}
			else if (_returnToRunesRequested)
			{
				ReturnFromSpellsToRunes();
			}
			else if (_stage == CombatStage.RuneSelector)
			{
				CorrectNativeSelection();
			}
		}
	}

	internal static void PrepareMagicOpen(ref bool openSummary)
	{
		if (!_rebuildingSpells && _stage != CombatStage.Spells)
		{
			if (_stage == CombatStage.RuneSelector)
			{
				openSummary = true;
			}
			else if ((_commandDepth > 0 || Time.frameCount - _lastCommandFrame <= 1) && _playerNo >= 0 && OpenRuneSelector(_playerNo))
			{
				openSummary = true;
			}
		}
	}

	internal static void FinishMagicOpen()
	{
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Expected O, but got Unknown
		if (_stage != CombatStage.RuneSelector || !_windowMappingActive)
		{
			return;
		}
		UIBattleManager instance = Singleton<UIBattleManager>.Instance;
		object obj;
		if (instance == null)
		{
			obj = null;
		}
		else
		{
			UIBattleMain battleWindow = instance.battleWindow;
			obj = ((battleWindow != null) ? battleWindow.m_emblem : null);
		}
		UIWindowList val = (UIWindowList)obj;
		if (instance == null || val == null)
		{
			return;
		}
		instance.battleWindow.FocusWindow((WindowType)8, _selectedSlot);
		SetNativeCursor(_selectedSlot);
		EnsureThreeRowHeight(val);
		if (TryGetPlayer(_playerNo, out PLAYER_BASE player) && player != null)
		{
			BuildEmblemRows(player, val, recreateCells: false);
		}
		if (!_selectorLogged)
		{
			_selectorLogged = true;
			int num = val.cellList?.Count ?? 0;
			ManualLogSource modLog = Plugin.ModLog;
			bool flag = default(bool);
			BepInExInfoLogInterpolatedStringHandler val2 = new BepInExInfoLogInterpolatedStringHandler(96, 5, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("Combat natif - Emblem ouverte pour l'index ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_playerNo);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(" ");
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("(chara ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_playerCharacterNo);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(", source ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(_playerSource);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("): ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(num);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(" cellules, hauteur augmentee de ");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<float>(_expandedHeightDelta, "0.##");
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(".");
			}
			modLog.LogInfo(val2);
		}
	}

	internal static bool TryOverrideInput(GRInputManager.Type input, out bool value)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Invalid comparison between Unknown and I4
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Invalid comparison between Unknown and I4
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Invalid comparison between Unknown and I4
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Invalid comparison between Unknown and I4
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Invalid comparison between Unknown and I4
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Expected O, but got Unknown
		value = false;
		if (_probingNativeInput)
		{
			return false;
		}
		if ((int)input == 5 && _nativeRuneCancelPending && _stage == CombatStage.RuneSelector && _commandDepth > 0)
		{
			value = true;
			if (!_nativeRuneCancelInjected)
			{
				_nativeRuneCancelInjected = true;
				_nativeRuneCancelAttemptCount++;
				ManualLogSource modLog = Plugin.ModLog;
				bool flag = default(bool);
				BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(101, 4, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R156 NATIVE-CANCEL INJECTE: B#");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_diagBPressCount);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; tentative=");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_nativeRuneCancelAttemptCount);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; ");
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("frame=");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(Time.frameCount);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; commandDepth=");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_commandDepth);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; aucun etat natif ecrit par le mod.");
				}
				modLog.LogInfo(val);
			}
			return true;
		}
		if ((int)input == 5 && _selectorBackReleaseGuard)
		{
			return true;
		}
		if (_commandDepth <= 0)
		{
			return false;
		}
		if ((int)input == 4 && (_suppressDecideFrame == Time.frameCount || (_stage == CombatStage.RuneSelector && !_confirmArmed) || (_stage == CombatStage.Spells && _spellConfirmBlockedUntilRelease)))
		{
			return true;
		}
		if ((int)input == 5 && _stage == CombatStage.RuneSelector && _cancelBlockedUntilRelease)
		{
			return true;
		}
		if ((int)input == 5 && _stage == CombatStage.Spells && _directNativeRune && _returnToRunesRequested)
		{
			return true;
		}
		return false;
	}

	internal static void AfterNativeInput(GRInputManager.Type input, ref bool value)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Invalid comparison between Unknown and I4
		if (!_probingNativeInput && _commandDepth > 0 && _stage == CombatStage.Spells && (int)input == 5 && value)
		{
			BattleBase.WINDOW_WORK window_work = BattleBase.window_work;
			if (_directNativeRune && window_work != null && window_work.selected_item == 0 && window_work.index_level == 2)
			{
				value = false;
				_returnToRunesRequested = true;
				Plugin.ModLog.LogInfo((object)"R140 DIRECT-B natif consomme dans Pie-grieche; retour 3-runes differe au postfix courant.");
			}
			else if (_forceNativeSpellSelection && IsRealMagicWindowActive())
			{
				value = false;
				_returnToRunesRequested = true;
			}
		}
	}

	internal static void PollPhysicalInputDiagnostics(bool cancelPressed, bool cancelHeld, bool decidePressed, bool upPressed, bool downPressed)
	{
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Expected O, but got Unknown
		bool flag = false;
		string text = string.Empty;
		if (cancelPressed)
		{
			_diagBPressCount++;
			text += $" B#{_diagBPressCount}";
			flag = true;
		}
		if (decidePressed)
		{
			_diagAPressCount++;
			text += $" A#{_diagAPressCount}";
			flag = true;
			CaptureNativeCommandStateBeforeRuneIfApplicable();
		}
		if (upPressed)
		{
			_diagUpPressCount++;
			text += $" UP#{_diagUpPressCount}";
			flag = true;
		}
		if (downPressed)
		{
			_diagDownPressCount++;
			text += $" DOWN#{_diagDownPressCount}";
			flag = true;
		}
		if (flag)
		{
			ManualLogSource modLog = Plugin.ModLog;
			bool flag2 = default(bool);
			BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(36, 3, out flag2);
			if (flag2)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R156 INPUT PHYSIQUE");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(text);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; frame=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(Time.frameCount);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; Bheld=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(cancelHeld);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
			}
			modLog.LogInfo(val);
			LogR140Snapshot("INPUT-PRE");
			_diagFollowUntilFrame = Math.Max(_diagFollowUntilFrame, Time.frameCount + 8);
			_diagLastFollowFrame = Time.frameCount;
		}
		else if (Time.frameCount <= _diagFollowUntilFrame && Time.frameCount != _diagLastFollowFrame)
		{
			_diagLastFollowFrame = Time.frameCount;
			LogR140Snapshot("INPUT-POST");
		}
	}

	private static bool TryPrimeDirectRuneSelectorCarrier(string source)
	{
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Expected O, but got Unknown
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Expected O, but got Unknown
		if (_selectorCarrierPrimed || _stage != 0 || _playerNo < 0)
		{
			return _selectorCarrierPrimed;
		}
		int? num = ReadIntMember(GetPmEntry(_playerNo), "c_koudou");
		BattleBase.WINDOW_WORK window_work = BattleBase.window_work;
		if (((window_work != null) ? window_work.window_index : (-1)) != 2 && num != 2)
		{
			return false;
		}
		if (!TryGetPlayer(_playerNo, out PLAYER_BASE player) || player == null)
		{
			return false;
		}
		RuneState runeState = RuneSlotService.PeekStoredCombatState(player);
		int unlockedSlotCount = RuneSlotService.GetUnlockedSlotCount(player);
		int num2 = Math.Clamp(runeState.ActiveIndex, 0, 2);
		byte b = runeState.RuneIds[num2];
		if (!IsDirectNativeRune(b))
		{
			return false;
		}
		EnsureDirectRuneCommandItemZero(_playerNo, "entree-runes-carrier");
		int num3 = -1;
		for (int i = 0; i < Math.Min(unlockedSlotCount, 3); i++)
		{
			if (i != num2)
			{
				byte b2 = runeState.RuneIds[i];
				if (b2 != 0 && !IsPassiveRune(b2) && !IsDirectNativeRune(b2))
				{
					num3 = i;
					break;
				}
			}
		}
		bool flag = default(bool);
		if (num3 < 0)
		{
			if (!CarrierWarningLogged.Add(player.chara_no))
			{
				return false;
			}
			ManualLogSource modLog = Plugin.ModLog;
			BepInExWarningLogInterpolatedStringHandler val = new BepInExWarningLogInterpolatedStringHandler(123, 3, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R156 DIRECT-CARRIER: aucune rune magique secondaire disponible pour ouvrir le selecteur 3-runes; ");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("player=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_playerNo);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", chara=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(player.chara_no);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", storage=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(RuneSlotService.DescribeStoredCombatState(player));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
			}
			modLog.LogWarning(val);
			return false;
		}
		if (!RuneSlotService.PresentStoredCombatSlotNativeOnly(player, num3, out var runeId))
		{
			return false;
		}
		_selectorCarrierPrimed = true;
		_selectorCarrierSlot = num3;
		_selectorCarrierRune = runeId;
		ManualLogSource modLog2 = Plugin.ModLog;
		BepInExInfoLogInterpolatedStringHandler val2 = new BepInExInfoLogInterpolatedStringHandler(128, 7, out flag);
		if (flag)
		{
			((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("R156 DIRECT-CARRIER PRIME (");
			((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(source);
			((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("): active stocke slot=");
			((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(num2 + 1);
			((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(" rune=");
			((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<byte>(b);
			((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("; ");
			((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("carrier natif temporaire slot=");
			((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(num3 + 1);
			((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(" rune=");
			((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<byte>(runeId);
			((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("; ");
			((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("storage INCHANGE=");
			((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(RuneSlotService.DescribeStoredCombatState(player));
			((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("; nativeMonsyo=");
			PLAYER_MONSYO monsyo_data = player.monsyo_data;
			((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<byte>((byte)((monsyo_data != null) ? monsyo_data.monsyo_id : 0));
			((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(".");
		}
		modLog2.LogInfo(val2);
		return true;
	}

	private static void ClearDirectRuneSelectorCarrier(bool restoreStoredNative, string source)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected O, but got Unknown
		if (!_selectorCarrierPrimed)
		{
			return;
		}
		if (restoreStoredNative && TryGetPlayer(_playerNo, out PLAYER_BASE player) && player != null)
		{
			RuneState runeState = RuneSlotService.RestoreStoredCombatState(player);
			ManualLogSource modLog = Plugin.ModLog;
			bool flag = default(bool);
			BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(108, 4, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R156 DIRECT-CARRIER RESTORE (");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(source);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("): rune active stockee representee au moteur; ");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("active=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(runeState.ActiveIndex + 1);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", nativeMonsyo=");
				PLAYER_MONSYO monsyo_data = player.monsyo_data;
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<byte>((byte)((monsyo_data != null) ? monsyo_data.monsyo_id : 0));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", ");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("storage=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(RuneSlotService.DescribeStoredCombatState(player));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
			}
			modLog.LogInfo(val);
		}
		_selectorCarrierPrimed = false;
		_selectorCarrierSlot = -1;
		_selectorCarrierRune = 0;
	}

	private static void CaptureNativeCommandStateBeforeRuneIfApplicable()
	{
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Expected O, but got Unknown
		if (_stage != 0 || _playerNo < 0)
		{
			return;
		}
		BattleBase.WINDOW_WORK window_work = BattleBase.window_work;
		UIBattleManager instance = Singleton<UIBattleManager>.Instance;
		object obj;
		if (instance == null)
		{
			obj = null;
		}
		else
		{
			UIBattleMain battleWindow = instance.battleWindow;
			obj = ((battleWindow != null) ? battleWindow.m_command : null);
		}
		UIWindowList val = (UIWindowList)obj;
		if (window_work != null && instance != null && val != null && window_work.window_index == 2 && instance.IsOpenWindowUI((WindowType)1))
		{
			object? pmEntry = GetPmEntry(_playerNo);
			_commandStateBeforeRuneCaptured = true;
			_commandBeforeRuneWindowStep = window_work.window_step;
			_commandBeforeRuneIndexLevel = window_work.index_level;
			_commandBeforeRuneWindowIndex = window_work.window_index;
			_commandBeforeRuneReturnValue = window_work.return_value;
			_commandBeforeRuneSelectedItem = window_work.selected_item;
			_commandBeforeRuneSelectedMonster = window_work.selected_monster;
			_commandBeforeRuneKoudou = ReadIntMember(pmEntry, "c_koudou") ?? (-1);
			_commandBeforeRuneItem = ReadIntMember(pmEntry, "c_item").GetValueOrDefault();
			_commandBeforeRuneTarget = ReadIntMember(pmEntry, "c_mokuhyo").GetValueOrDefault();
			ManualLogSource modLog = Plugin.ModLog;
			bool flag = default(bool);
			BepInExInfoLogInterpolatedStringHandler val2 = new BepInExInfoLogInterpolatedStringHandler(123, 12, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("R156 PRE-RUNE COMMAND CAPTURE A#");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_diagAPressCount);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(": player=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_playerNo);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(", chara=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_playerCharacterNo);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("; ");
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("WINDOW(level=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_commandBeforeRuneIndexLevel);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(",step=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_commandBeforeRuneWindowStep);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(",wi=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_commandBeforeRuneWindowIndex);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(",");
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("item=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_commandBeforeRuneSelectedItem);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(",monster=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_commandBeforeRuneSelectedMonster);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(",ret=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_commandBeforeRuneReturnValue);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("); ");
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("PM(koudou=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_commandBeforeRuneKoudou);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(",item=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_commandBeforeRuneItem);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(",target=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_commandBeforeRuneTarget);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(").");
			}
			modLog.LogInfo(val2);
			PrepareRuneSelectorVisualBeforeNativeOpen("physical-A");
			TryPrimeDirectRuneSelectorCarrier($"A#{_diagAPressCount}");
		}
	}

	private static void PrepareRuneSelectorVisualBeforeNativeOpen(string source)
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Expected O, but got Unknown
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		if (_playerNo < 0 || !TryGetPlayer(_playerNo, out PLAYER_BASE player) || player == null)
		{
			return;
		}
		UIBattleManager instance = Singleton<UIBattleManager>.Instance;
		object obj;
		if (instance == null)
		{
			obj = null;
		}
		else
		{
			UIBattleMain battleWindow = instance.battleWindow;
			obj = ((battleWindow != null) ? battleWindow.m_emblem : null);
		}
		UIWindowList val = (UIWindowList)obj;
		if (instance == null || val == null)
		{
			return;
		}
		try
		{
			CaptureEmblemGeometry(val);
			BuildEmblemRows(player, val);
			EnsureThreeRowHeight(val);
			_preRuneVisualPending = true;
			ManualLogSource modLog = Plugin.ModLog;
			bool flag = default(bool);
			BepInExInfoLogInterpolatedStringHandler val2 = new BepInExInfoLogInterpolatedStringHandler(106, 13, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("R156 PRE-ARM EMBLEM (");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(source);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("): frame=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(Time.frameCount);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("; player=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_playerNo);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(", chara=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(player.chara_no);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("; ");
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("rows=[");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(Labels[0]);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("] | [");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(Labels[1]);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("] | [");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(Labels[2]);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("]; ");
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("window=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<float>(val.m_rectTransform.sizeDelta.x, "0.##");
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("x");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<float>(val.m_rectTransform.sizeDelta.y, "0.##");
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("; ");
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("contents=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<float>(val.m_contents.sizeDelta.x, "0.##");
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("x");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<float>(val.m_contents.sizeDelta.y, "0.##");
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("; ");
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("open=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<bool>(((UIWindowBasis)val).IsOpen);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("; opening=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<bool>(((UIWindowBasis)val).IsOpening);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(".");
			}
			modLog.LogInfo(val2);
		}
		catch (Exception ex)
		{
			_preRuneVisualPending = false;
			Plugin.ModLog.LogWarning((object)("R156 PRE-ARM EMBLEM impossible; flux natif conserve: " + ex.GetBaseException().Message));
		}
	}

	internal static void PrepareNativeEmblemOpen()
	{
		if (_preRuneVisualPending && _stage == CombatStage.Idle && _playerNo >= 0)
		{
			PrepareRuneSelectorVisualBeforeNativeOpen("OpenWindowUI-Emblem");
		}
	}

	internal static void LogPhysicalBackRouting(bool handledByMod)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		ManualLogSource modLog = Plugin.ModLog;
		bool flag = default(bool);
		BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(154, 10, out flag);
		if (flag)
		{
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R156 B ROUTAGE B#");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_diagBPressCount);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(": handledByMod=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(handledByMod);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; stage=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<CombatStage>(_stage);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; ");
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("returnRunes=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(_returnToRunesRequested);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; direct=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(_directNativeRune);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; cancelBlock=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(_cancelBlockedUntilRelease);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; ");
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("selectorBackGuard=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(_selectorBackReleaseGuard);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; nativeCancelPending=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(_nativeRuneCancelPending);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; ");
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("nativeCancelInjected=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(_nativeRuneCancelInjected);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; attempts=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_nativeRuneCancelAttemptCount);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
		}
		modLog.LogInfo(val);
		LogR140Snapshot("B-ROUTAGE");
	}

	private static void LogR140Snapshot(string phase)
	{
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Expected O, but got Unknown
		try
		{
			BattleBase.WINDOW_WORK window_work = BattleBase.window_work;
			UIBattleManager instance = Singleton<UIBattleManager>.Instance;
			object obj;
			if (instance == null)
			{
				obj = null;
			}
			else
			{
				UIBattleMain battleWindow = instance.battleWindow;
				obj = ((battleWindow != null) ? battleWindow.m_command : null);
			}
			UIWindowList window = (UIWindowList)obj;
			object obj2;
			if (instance == null)
			{
				obj2 = null;
			}
			else
			{
				UIBattleMain battleWindow2 = instance.battleWindow;
				obj2 = ((battleWindow2 != null) ? battleWindow2.m_emblem : null);
			}
			UIWindowList window2 = (UIWindowList)obj2;
			object obj3;
			if (instance == null)
			{
				obj3 = null;
			}
			else
			{
				UIBattleMain battleWindow3 = instance.battleWindow;
				obj3 = ((battleWindow3 != null) ? battleWindow3.m_magic : null);
			}
			UIWindowList window3 = (UIWindowList)obj3;
			int num = ReadIntMember(window_work, "selected_player") ?? (-1);
			object? pmEntry = GetPmEntry((_playerNo >= 0) ? _playerNo : num);
			int? value = ReadIntMember(pmEntry, "c_koudou");
			int? value2 = ReadIntMember(pmEntry, "c_item");
			int? value3 = ReadIntMember(pmEntry, "c_mokuhyo");
			int? value4 = ReadIntMember(pmEntry, "finished_flg");
			string text = "n/a";
			string text2 = "n/a";
			if (TryGetPlayer(_playerNo, out PLAYER_BASE player) && player != null)
			{
				text = RuneSlotService.DescribeStoredCombatState(player);
				PLAYER_MONSYO monsyo_data = player.monsyo_data;
				text2 = ((byte)((monsyo_data != null) ? monsyo_data.monsyo_id : 0)).ToString();
			}
			string text3 = $"[{Labels[0]}] | [{Labels[1]}] | [{Labels[2]}]";
			ManualLogSource modLog = Plugin.ModLog;
			bool flag = default(bool);
			BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(419, 38, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R156 SNAP ");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(phase);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(": frame=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(Time.frameCount);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; stage=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<CombatStage>(_stage);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; player=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_playerNo);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; ");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("nativePlayer=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(num);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; chara=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_playerCharacterNo);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; source=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(_playerSource);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; ");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("slot=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_selectedSlot + 1);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; depth=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_commandDepth);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; mapMagicToEmblem=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(_windowMappingActive);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; ");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("flags(cancelBlock=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(_cancelBlockedUntilRelease);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",selectorBackGuard=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(_selectorBackReleaseGuard);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",returnRunes=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(_returnToRunesRequested);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("forceSpell=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(_forceNativeSpellSelection);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",direct=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(_directNativeRune);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",carrier=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(_selectorCarrierPrimed);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(":slot");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_selectorCarrierSlot + 1);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("/rune");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<byte>(_selectorCarrierRune);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("nativeCancelPending=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(_nativeRuneCancelPending);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",nativeCancelInjected=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(_nativeRuneCancelInjected);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",nativeCancelAttempts=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_nativeRuneCancelAttemptCount);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("); ");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("WINDOW(level=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>((window_work != null) ? window_work.index_level : (-1));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",step=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>((window_work != null) ? window_work.window_step : (-1));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",wi=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>((window_work != null) ? window_work.window_index : (-1));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("item=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>((window_work != null) ? window_work.selected_item : (-1));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",monster=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>((window_work != null) ? window_work.selected_monster : (-1));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",magicKazu=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>((window_work != null) ? window_work.magic_kazu : (-1));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",ret=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>((window_work != null) ? window_work.return_value : (-1));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("); ");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("PM(koudou=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(FmtDiag(value));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",item=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(FmtDiag(value2));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",target=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(FmtDiag(value3));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",finished=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(FmtDiag(value4));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("); ");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("UI(Command=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(WindowDiag(instance, (WindowType)1, window));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("Emblem=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(WindowDiag(instance, (WindowType)5, window2));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("Magic=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(WindowDiag(instance, (WindowType)8, window3));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("); ");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("STORAGE(");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(text);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("); nativeMonsyo=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(text2);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; lastRows=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(text3);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
			}
			modLog.LogInfo(val);
		}
		catch (Exception ex)
		{
			Plugin.ModLog.LogWarning((object)("R156 SNAP impossible: " + ex.GetBaseException().Message));
		}
	}

	private static string WindowDiag(UIBattleManager? battleUi, WindowType type, UIWindowList? window)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		bool value = false;
		try
		{
			value = battleUi != null && battleUi.IsOpenWindowUI(type);
		}
		catch
		{
		}
		return $"mgr={value},open={window != null && ((UIWindowBasis)window).IsOpen},opening={window != null && ((UIWindowBasis)window).IsOpening},focus={window != null && window.IsFocus},cells={((window == null) ? null : window.cellList?.Count).GetValueOrDefault()}";
	}

	private static string FmtDiag(int? value)
	{
		if (!value.HasValue)
		{
			return "?";
		}
		return value.Value.ToString();
	}

	private static int? ReadIntMember(object? obj, string name)
	{
		if (obj == null)
		{
			return null;
		}
		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		try
		{
			PropertyInfo property = obj.GetType().GetProperty(name, bindingAttr);
			if ((object)property != null && property.CanRead)
			{
				return Convert.ToInt32(property.GetValue(obj));
			}
		}
		catch
		{
		}
		try
		{
			FieldInfo field = obj.GetType().GetField(name, bindingAttr);
			if ((object)field != null)
			{
				return Convert.ToInt32(field.GetValue(obj));
			}
		}
		catch
		{
		}
		return null;
	}

	internal static bool PollDirectMagicBack(bool cancelPressed)
	{
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Expected O, but got Unknown
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Expected O, but got Unknown
		if (!cancelPressed)
		{
			return false;
		}
		if (_stage == CombatStage.RuneSelector)
		{
			ArmNativeRuneSelectorCancel();
			return true;
		}
		if (_stage != CombatStage.Spells)
		{
			return false;
		}
		bool flag = default(bool);
		BepInExInfoLogInterpolatedStringHandler val;
		if (_directNativeRune)
		{
			BattleBase.WINDOW_WORK window_work = BattleBase.window_work;
			if (window_work != null && window_work.selected_item == 0 && window_work.index_level == 2)
			{
				_returnToRunesRequested = true;
				ManualLogSource modLog = Plugin.ModLog;
				val = new BepInExInfoLogInterpolatedStringHandler(75, 1, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R156 B#");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_diagBPressCount);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" Pie-grieche: retour 3-runes arme; aucune action sur B du selecteur.");
				}
				modLog.LogInfo(val);
				return true;
			}
			return false;
		}
		if (!_forceNativeSpellSelection || !IsRealMagicWindowActive())
		{
			return false;
		}
		ManualLogSource modLog2 = Plugin.ModLog;
		val = new BepInExInfoLogInterpolatedStringHandler(49, 1, out flag);
		if (flag)
		{
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R156 B#");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_diagBPressCount);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" liste Magic: retour immediat vers Emblem.");
		}
		modLog2.LogInfo(val);
		ReturnFromSpellsToRunes();
		return true;
	}

	private static void ArmNativeRuneSelectorCancel()
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Expected O, but got Unknown
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		bool flag = default(bool);
		BepInExInfoLogInterpolatedStringHandler val;
		if (_nativeRuneCancelPending)
		{
			ManualLogSource modLog = Plugin.ModLog;
			val = new BepInExInfoLogInterpolatedStringHandler(58, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R156 B#");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_diagBPressCount);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(": Cancel natif deja en attente; aucun etat modifie.");
			}
			modLog.LogInfo(val);
			return;
		}
		BattleBase.WINDOW_WORK window_work = BattleBase.window_work;
		int? value = ReadIntMember(GetPmEntry(_playerNo), "c_koudou");
		_nativeRuneCancelPending = true;
		_nativeRuneCancelInjected = false;
		_nativeRuneCancelCommandWindowSeen = false;
		_nativeRuneCancelRequestFrame = Time.frameCount;
		_nativeRuneCancelAttemptCount = 0;
		ManualLogSource modLog2 = Plugin.ModLog;
		val = new BepInExInfoLogInterpolatedStringHandler(160, 6, out flag);
		if (flag)
		{
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R156 B#");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_diagBPressCount);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" RUNES->COMMANDES ARME: aucun BattleBase.WINDOW_WORK/pm_data/UI ecrit; ");
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("attente du prochain command_magic natif. avant: level=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>((window_work != null) ? window_work.index_level : (-1));
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",step=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>((window_work != null) ? window_work.window_step : (-1));
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",");
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("wi=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>((window_work != null) ? window_work.window_index : (-1));
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",c_koudou=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(FmtDiag(value));
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",mapMagicToEmblem=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(_windowMappingActive);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
		}
		modLog2.LogInfo(val);
		LogR140Snapshot("R156-NATIVE-CANCEL-ARME");
	}

	private static void ObserveNativeRuneCancelAfterCommand()
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected O, but got Unknown
		BattleBase.WINDOW_WORK window_work = BattleBase.window_work;
		UIBattleManager instance = Singleton<UIBattleManager>.Instance;
		int? value = ReadIntMember(GetPmEntry(_playerNo), "c_koudou");
		bool flag = false;
		try
		{
			flag = instance != null && instance.IsOpenWindowUI((WindowType)8);
		}
		catch
		{
		}
		bool flag2 = _nativeRuneCancelCommandWindowSeen || (value.HasValue && value.Value != 2) || (window_work != null && window_work.index_level == 0 && !flag);
		ManualLogSource modLog = Plugin.ModLog;
		bool flag3 = default(bool);
		BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(147, 11, out flag3);
		if (flag3)
		{
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R156 NATIVE-CANCEL APRES HANDLER: tentative=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_nativeRuneCancelAttemptCount);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; ");
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("exit=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(flag2);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; commandWindowSeen=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(_nativeRuneCancelCommandWindowSeen);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; ");
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("WINDOW(level=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>((window_work != null) ? window_work.index_level : (-1));
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",step=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>((window_work != null) ? window_work.window_step : (-1));
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",wi=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>((window_work != null) ? window_work.window_index : (-1));
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",");
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("item=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>((window_work != null) ? window_work.selected_item : (-1));
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",monster=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>((window_work != null) ? window_work.selected_monster : (-1));
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(",ret=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>((window_work != null) ? window_work.return_value : (-1));
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("); ");
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("c_koudou=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(FmtDiag(value));
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; mappedMagicOpen=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(flag);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
		}
		modLog.LogInfo(val);
		if (!flag2)
		{
			_nativeRuneCancelInjected = false;
			if (Time.frameCount - _nativeRuneCancelRequestFrame > 120 || _nativeRuneCancelAttemptCount >= 8)
			{
				Plugin.ModLog.LogWarning((object)"R156 NATIVE-CANCEL: le moteur n'a pas quitte Runes apres plusieurs handlers; abandon propre de l'injection sans modifier BattleBase.WINDOW_WORK.");
				_nativeRuneCancelPending = false;
				_nativeRuneCancelAttemptCount = 0;
			}
		}
		else
		{
			CompleteNativeRuneSelectorCancel();
		}
	}

	private static void CompleteNativeRuneSelectorCancel()
	{
		UIBattleManager instance = Singleton<UIBattleManager>.Instance;
		_nativeRuneCancelPending = false;
		_nativeRuneCancelInjected = false;
		_nativeRuneCancelCommandWindowSeen = false;
		_nativeRuneCancelAttemptCount = 0;
		RestoreNativeWindowMapping();
		RestoreEmblemGeometry();
		ClearDirectRuneSelectorCarrier(restoreStoredNative: true, "cancel-runes-vers-commandes");
		try
		{
			if (instance != null && instance.IsOpenWindowUI((WindowType)5))
			{
				instance.CloseWindowUI((WindowType)5);
			}
		}
		catch
		{
		}
		_pendingRuneValidation = false;
		_returnToRunesRequested = false;
		_forceNativeSpellSelection = false;
		_directNativeRune = false;
		_selectorOpenedFromMagicBack = false;
		_cancelBlockedUntilRelease = false;
		_selectorBackReleaseGuard = true;
		_stage = CombatStage.ReturnToCommandRelease;
		_playerSource = "native-command-cancel/R151";
		BattleBase.WINDOW_WORK window_work = BattleBase.window_work;
		Plugin.ModLog.LogInfo((object)($"R156 RUNES->COMMANDES CONFIRME PAR NATIF: player={_playerNo}, chara={_playerCharacterNo}; WINDOW(level={((window_work != null) ? window_work.index_level : (-1))},step={((window_work != null) ? window_work.window_step : (-1))},wi={((window_work != null) ? window_work.window_index : (-1))}); PM(koudou={FmtDiag(ReadIntMember(GetPmEntry(_playerNo), "c_koudou"))},item={FmtDiag(ReadIntMember(GetPmEntry(_playerNo), "c_item"))},target={FmtDiag(ReadIntMember(GetPmEntry(_playerNo), "c_mokuhyo"))}); " + "aucun etat natif force par le mod; meme B masque jusqu'au relachement."));
		LogR140Snapshot("R156-NATIVE-CANCEL-COMPLETE");
	}

	internal static void PollDirectCancelRelease(bool cancelHeld)
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected O, but got Unknown
		if (_nativeRuneCancelPending && !_nativeRuneCancelInjected && Time.frameCount - _nativeRuneCancelRequestFrame > 120)
		{
			Plugin.ModLog.LogWarning((object)"R156 NATIVE-CANCEL: aucun command_magic n'a lu le Cancel pendant 120 frames; injection annulee sans modifier l'etat du jeu.");
			_nativeRuneCancelPending = false;
			_nativeRuneCancelAttemptCount = 0;
		}
		if (_selectorBackReleaseGuard && !cancelHeld)
		{
			_selectorBackReleaseGuard = false;
			if (_stage == CombatStage.ReturnToCommandRelease)
			{
				_stage = CombatStage.Idle;
			}
			ManualLogSource modLog = Plugin.ModLog;
			bool flag = default(bool);
			BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(91, 2, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R144 B relache apres Runes -> Commandes; Cancel natif de nouveau autorise. player=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_playerNo);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", chara=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_playerCharacterNo);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
			}
			modLog.LogInfo(val);
			LogR140Snapshot("R156-COMMAND-RELEASE");
		}
		if (_stage == CombatStage.RuneSelector && _cancelBlockedUntilRelease && !cancelHeld)
		{
			_cancelBlockedUntilRelease = false;
			Plugin.ModLog.LogInfo((object)"R101 R52-FIX - B relache apres retour Magic -> Runes; Cancel natif de nouveau autorise.");
		}
	}

	internal static void OnCloseAllWindows()
	{
		if (_nativeRuneCancelPending)
		{
			Plugin.ModLog.LogInfo((object)"R156 NATIVE-CANCEL: CloseAllWindowUI observe; Reset du mod ignore jusqu au postfix natif.");
		}
		else if (_rebuildingSpells)
		{
			Plugin.ModLog.LogInfo((object)"Combat natif - CloseAll interne ignore pendant la reconstruction de Magic.");
		}
		else
		{
			Reset();
		}
	}

	internal static void Reset()
	{
		bool selectorBackReleaseGuard = _selectorBackReleaseGuard;
		ClearDirectRuneSelectorCarrier(restoreStoredNative: true, "Reset");
		RestoreNativeWindowMapping();
		RestoreEmblemGeometry();
		_stage = CombatStage.Idle;
		_playerNo = -1;
		_playerCharacterNo = -1;
		_playerSource = string.Empty;
		_commandDepth = 0;
		_selectedSlot = 0;
		_lastCommandFrame = -1000;
		_suppressDecideFrame = -1000;
		_confirmArmed = false;
		_selectorIndexBeforeCommand = -1;
		_selectorMoveDirection = 0;
		_selectorUpHeld = false;
		_selectorDownHeld = false;
		_cancelBlockedUntilRelease = false;
		_selectorBackReleaseGuard = selectorBackReleaseGuard;
		_nativeRuneCancelPending = false;
		_nativeRuneCancelInjected = false;
		_nativeRuneCancelCommandWindowSeen = false;
		_nativeRuneCancelRequestFrame = -1000;
		_nativeRuneCancelAttemptCount = 0;
		_spellConfirmBlockedUntilRelease = false;
		_pendingRuneValidation = false;
		_returnToRunesRequested = false;
		_selectorOpenedFromMagicBack = false;
		_probingNativeInput = false;
		_rebuildingSpells = false;
		_selectorLogged = false;
		_selectorNeedsSettledRebuild = false;
		_preRuneVisualPending = false;
		_selectorCarrierPrimed = false;
		_selectorCarrierSlot = -1;
		_selectorCarrierRune = 0;
		_diagnosticTraceUntilFrame = -1000;
		_forceNativeSpellSelection = false;
		_directNativeRune = false;
		_spellHandlerRerouteLogged = false;
		_magic01Depth = 0;
		_r52TraceBudget = 0;
		_runeEntryStateCaptured = false;
		_runeEntryWindowStep = 0;
		_runeEntryIndexLevel = 0;
		_runeEntryReturnValue = 0;
		_runeEntrySelectedItem = 0;
		_runeEntrySelectedMonster = 0;
		_commandStateBeforeRuneCaptured = false;
		_commandBeforeRuneWindowStep = 0;
		_commandBeforeRuneIndexLevel = 0;
		_commandBeforeRuneWindowIndex = 2;
		_commandBeforeRuneReturnValue = 0;
		_commandBeforeRuneSelectedItem = 0;
		_commandBeforeRuneSelectedMonster = 0;
		_commandBeforeRuneKoudou = -1;
		_commandBeforeRuneItem = 0;
		_commandBeforeRuneTarget = 0;
	}

	private static bool BeforeNativeCommand()
	{
		if (_stage == CombatStage.Spells && _spellConfirmBlockedUntilRelease && !ProbeIsOn((GRInputManager.Type)4))
		{
			_spellConfirmBlockedUntilRelease = false;
		}
		if (_stage != CombatStage.RuneSelector)
		{
			return true;
		}
		UIBattleManager instance = Singleton<UIBattleManager>.Instance;
		object obj;
		if (instance == null)
		{
			obj = null;
		}
		else
		{
			UIBattleMain battleWindow = instance.battleWindow;
			obj = ((battleWindow != null) ? battleWindow.m_emblem : null);
		}
		UIWindowList val = (UIWindowList)obj;
		if (val != null)
		{
			EnsureThreeRowHeight(val);
		}
		if (!_confirmArmed && !ProbeIsOn((GRInputManager.Type)4))
		{
			_confirmArmed = true;
		}
		if (_cancelBlockedUntilRelease && !ProbeIsOn((GRInputManager.Type)5))
		{
			_cancelBlockedUntilRelease = false;
		}
		BattleBase.WINDOW_WORK window_work = BattleBase.window_work;
		_selectorIndexBeforeCommand = Math.Clamp((window_work != null) ? window_work.window_index : _selectedSlot, 0, 2);
		_selectorMoveDirection = 0;
		bool num = ProbeIsOn((GRInputManager.Type)0);
		bool flag = ProbeIsOn((GRInputManager.Type)1);
		bool flag2 = num && !_selectorUpHeld;
		bool flag3 = flag && !_selectorDownHeld;
		_selectorUpHeld = num;
		_selectorDownHeld = flag;
		if (flag2 != flag3)
		{
			_selectorMoveDirection = (flag3 ? 1 : (-1));
		}
		if (_pendingRuneValidation)
		{
			return true;
		}
		if (!_confirmArmed || !ProbeIsPress((GRInputManager.Type)4))
		{
			return true;
		}
		_suppressDecideFrame = Time.frameCount;
		_pendingRuneValidation = true;
		Plugin.ModLog.LogInfo((object)"Combat natif - A sur Emblem capture; Decide masque pour cette image, commande native conservee.");
		return true;
	}

	private static bool OpenRuneSelector(int playerNo)
	{
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Expected O, but got Unknown
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_0318: Unknown result type (might be due to invalid IL or missing references)
		//IL_031f: Expected O, but got Unknown
		_preRuneVisualPending = false;
		if (!TryGetPlayer(playerNo, out PLAYER_BASE player) || player == null)
		{
			return false;
		}
		UIBattleManager instance = Singleton<UIBattleManager>.Instance;
		object obj;
		if (instance == null)
		{
			obj = null;
		}
		else
		{
			UIBattleMain battleWindow = instance.battleWindow;
			obj = ((battleWindow != null) ? battleWindow.m_emblem : null);
		}
		UIWindowList val = (UIWindowList)obj;
		if (instance == null || val == null)
		{
			return false;
		}
		try
		{
			RuneState runeState = (_selectorCarrierPrimed ? RuneSlotService.PeekStoredCombatState(player) : RuneSlotService.RestoreStoredCombatState(player));
			bool flag = default(bool);
			BepInExInfoLogInterpolatedStringHandler val2;
			if (_selectorCarrierPrimed)
			{
				ManualLogSource modLog = Plugin.ModLog;
				val2 = new BepInExInfoLogInterpolatedStringHandler(92, 5, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("R156 DIRECT-CARRIER SELECTEUR: carrier slot=");
					((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_selectorCarrierSlot + 1);
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(" rune=");
					((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<byte>(_selectorCarrierRune);
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("; ");
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("active stocke=");
					((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(runeState.ActiveIndex + 1);
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("; storage=");
					((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(RuneSlotService.DescribeStoredCombatState(player));
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("; ");
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("nativeMonsyo=");
					BepInExInfoLogInterpolatedStringHandler obj2 = val2;
					PLAYER_MONSYO monsyo_data = player.monsyo_data;
					((BepInExLogInterpolatedStringHandler)obj2).AppendFormatted<byte>((byte)((monsyo_data != null) ? monsyo_data.monsyo_id : 0));
					((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(".");
				}
				modLog.LogInfo(val2);
			}
			CaptureEmblemGeometry(val);
			BuildEmblemRows(player, val);
			EnsureThreeRowHeight(val);
			ManualLogSource modLog2 = Plugin.ModLog;
			val2 = new BepInExInfoLogInterpolatedStringHandler(84, 8, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("R156 PRE-OPEN GEOMETRY: player=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(playerNo);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(", chara=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(player.chara_no);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("; ");
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("window=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<float>(val.m_rectTransform.sizeDelta.x, "0.##");
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("x");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<float>(val.m_rectTransform.sizeDelta.y, "0.##");
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("; ");
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("contents=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<float>(val.m_contents.sizeDelta.x, "0.##");
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("x");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<float>(val.m_contents.sizeDelta.y, "0.##");
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("; ");
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("unfocused=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<float>(val.m_unFocusedHeight, "0.##");
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("; opening=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<bool>(((UIWindowBasis)val).IsOpening);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(".");
			}
			modLog2.LogInfo(val2);
			MapNativeMagicWindowToEmblem(instance, val);
			int unlockedSlotCount = RuneSlotService.GetUnlockedSlotCount(player);
			_selectedSlot = FirstSelectableSlot(runeState, unlockedSlotCount, runeState.ActiveIndex);
			BattleBase.WINDOW_WORK window_work = BattleBase.window_work;
			if (window_work != null)
			{
				if (!_runeEntryStateCaptured)
				{
					_runeEntryStateCaptured = true;
					_runeEntryWindowStep = window_work.window_step;
					_runeEntryIndexLevel = window_work.index_level;
					_runeEntryReturnValue = window_work.return_value;
					_runeEntrySelectedItem = window_work.selected_item;
					_runeEntrySelectedMonster = window_work.selected_monster;
					ManualLogSource modLog3 = Plugin.ModLog;
					val2 = new BepInExInfoLogInterpolatedStringHandler(79, 4, out flag);
					if (flag)
					{
						((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("R101 R52-FIX - etat natif d'entree Runes memorise: ");
						((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("level=");
						((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_runeEntryIndexLevel);
						((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("/step=");
						((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_runeEntryWindowStep);
						((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("/");
						((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("item=");
						((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_runeEntrySelectedItem);
						((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("/monster=");
						((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_runeEntrySelectedMonster);
						((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(".");
					}
					modLog3.LogInfo(val2);
				}
				window_work.magic_kazu = 3;
				window_work.window_index = _selectedSlot;
			}
			_stage = CombatStage.RuneSelector;
			_geometryCorrections = 0;
			_confirmArmed = !ProbeIsOn((GRInputManager.Type)4);
			_selectorUpHeld = ProbeIsOn((GRInputManager.Type)0);
			_selectorDownHeld = ProbeIsOn((GRInputManager.Type)1);
			_cancelBlockedUntilRelease = false;
			_spellConfirmBlockedUntilRelease = false;
			_pendingRuneValidation = false;
			_returnToRunesRequested = false;
			_selectorOpenedFromMagicBack = false;
			_selectorLogged = false;
			_selectorNeedsSettledRebuild = true;
			return true;
		}
		catch (Exception ex)
		{
			Plugin.ModLog.LogWarning((object)("Combat natif - ouverture minimale d'Emblem impossible; flux original conserve: " + ex));
			Reset();
			return false;
		}
	}

	private static void BuildEmblemRows(PLAYER_BASE player, UIWindowList emblem, bool recreateCells = true)
	{
		//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Expected O, but got Unknown
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Expected O, but got Unknown
		RuneState runeState = RuneSlotService.PeekStoredCombatState(player);
		int unlockedSlotCount = RuneSlotService.GetUnlockedSlotCount(player);
		Il2CppSystem.Collections.Generic.List<Il2CppStringArray> val = new Il2CppSystem.Collections.Generic.List<Il2CppStringArray>();
		Il2CppSystem.Collections.Generic.List<string> val2 = new Il2CppSystem.Collections.Generic.List<string>();
		Il2CppSystem.Collections.Generic.List<bool> val3 = new Il2CppSystem.Collections.Generic.List<bool>();
		Il2CppSystem.Collections.Generic.List<int> val4 = new Il2CppSystem.Collections.Generic.List<int>();
		Il2CppSystem.Collections.Generic.List<string> val5 = new Il2CppSystem.Collections.Generic.List<string>();
		for (int i = 0; i < 3; i++)
		{
			bool num = i < unlockedSlotCount;
			byte b = runeState.RuneIds[i];
			bool flag = num && b != 0 && !IsPassiveRune(b);
			string text = ((!num) ? $"{i + 1}. Bloqué — MAG {RequiredMagic(i)}" : ((b == 0) ? $"{i + 1}. — (vide)" : $"{i + 1}. {RuneSlotService.GetRuneName(b)}"));
			string text2 = ((!num) ? $"Nécessite {RequiredMagic(i)} en MAG." : ((b == 0) ? "Aucune rune associée à cet emplacement." : h_monsyo_c.GetMonsyoHelp((int)b)));
			Labels[i] = text;
			Summaries[i] = text2;
			val2.Add(text);
			val3.Add(flag);
			val4.Add(0);
			val5.Add(text2);
			Il2CppStringArray val6 = new Il2CppStringArray(2L);
			((Il2CppArrayBase<string>)(object)val6)[0] = text;
			((Il2CppArrayBase<string>)(object)val6)[1] = string.Empty;
			val.Add(val6);
		}
		if (recreateCells)
		{
			emblem.SetData(val);
		}
		emblem.SetGrayout(!HasSelectableSlot(runeState, unlockedSlotCount));
		Singleton<UIBattleManager>.Instance.battleWindow.UpdateCommandText((WindowType)5, val2, val3, val4);
		Singleton<UIBattleManager>.Instance.battleWindow.UpdateSummaryText((WindowType)5, val2, val5);
		emblem.m_enableCellNum = 3;
		for (int j = 0; j < 3; j++)
		{
			emblem.SetCellText(j, val[j]);
		}
		string text3 = $"{player.chara_no}:{Labels[0]}||{Labels[1]}||{Labels[2]}";
		if (recreateCells || !string.Equals(text3, _lastRenderedRowsSignature, StringComparison.Ordinal))
		{
			_lastRenderedRowsSignature = text3;
			ManualLogSource modLog = Plugin.ModLog;
			bool flag2 = default(bool);
			BepInExInfoLogInterpolatedStringHandler val7 = new BepInExInfoLogInterpolatedStringHandler(108, 10, out flag2);
			if (flag2)
			{
				((BepInExLogInterpolatedStringHandler)val7).AppendLiteral("R156 UI-ROWS ECRITES frame=");
				((BepInExLogInterpolatedStringHandler)val7).AppendFormatted<int>(Time.frameCount);
				((BepInExLogInterpolatedStringHandler)val7).AppendLiteral("; chara=");
				((BepInExLogInterpolatedStringHandler)val7).AppendFormatted<int>(player.chara_no);
				((BepInExLogInterpolatedStringHandler)val7).AppendLiteral("; player=");
				((BepInExLogInterpolatedStringHandler)val7).AppendFormatted<int>(_playerNo);
				((BepInExLogInterpolatedStringHandler)val7).AppendLiteral("; ");
				((BepInExLogInterpolatedStringHandler)val7).AppendLiteral("rows=[");
				((BepInExLogInterpolatedStringHandler)val7).AppendFormatted<string>(Labels[0]);
				((BepInExLogInterpolatedStringHandler)val7).AppendLiteral("] | [");
				((BepInExLogInterpolatedStringHandler)val7).AppendFormatted<string>(Labels[1]);
				((BepInExLogInterpolatedStringHandler)val7).AppendLiteral("] | [");
				((BepInExLogInterpolatedStringHandler)val7).AppendFormatted<string>(Labels[2]);
				((BepInExLogInterpolatedStringHandler)val7).AppendLiteral("]; ");
				((BepInExLogInterpolatedStringHandler)val7).AppendLiteral("storage=");
				((BepInExLogInterpolatedStringHandler)val7).AppendFormatted<string>(RuneSlotService.DescribeStoredCombatState(player));
				((BepInExLogInterpolatedStringHandler)val7).AppendLiteral("; ");
				((BepInExLogInterpolatedStringHandler)val7).AppendLiteral("nativeMonsyo=");
				PLAYER_MONSYO monsyo_data = player.monsyo_data;
				((BepInExLogInterpolatedStringHandler)val7).AppendFormatted<byte>((byte)((monsyo_data != null) ? monsyo_data.monsyo_id : 0));
				((BepInExLogInterpolatedStringHandler)val7).AppendLiteral("; cells=");
				((BepInExLogInterpolatedStringHandler)val7).AppendFormatted<int>(emblem.cellList?.Count ?? 0);
				((BepInExLogInterpolatedStringHandler)val7).AppendLiteral("; recreate=");
				((BepInExLogInterpolatedStringHandler)val7).AppendFormatted<bool>(recreateCells);
				((BepInExLogInterpolatedStringHandler)val7).AppendLiteral(".");
			}
			modLog.LogInfo(val7);
		}
	}

	private static void MapNativeMagicWindowToEmblem(UIBattleManager battleUi, UIWindowList emblem)
	{
		UIBattleMain battleWindow = battleUi.battleWindow;
		if (_windowMappingActive)
		{
			UIWindowList? mappedWindowOwner = _mappedWindowOwner;
			if (((mappedWindowOwner != null) ? new IntPtr?(((Il2CppObjectBase)mappedWindowOwner).Pointer) : null) == ((Il2CppObjectBase)emblem).Pointer)
			{
				return;
			}
		}
		RestoreNativeWindowMapping();
		_mappedWindowOwner = emblem;
		_nativeMagicList = battleWindow.m_windowListDict[(WindowType)8];
		_nativeMagicBasis = battleWindow.m_windowBasisDict[(WindowType)8];
		_nativeMagicSummary = battleWindow.m_summaryDict[(WindowType)8];
		battleWindow.m_windowListDict[(WindowType)8] = emblem;
		battleWindow.m_windowBasisDict[(WindowType)8] = (UIWindowBasis)(object)emblem;
		battleWindow.m_summaryDict[(WindowType)8] = battleWindow.m_summaryEmblem;
		_windowMappingActive = true;
	}

	private static void RestoreNativeWindowMapping()
	{
		if (!_windowMappingActive)
		{
			return;
		}
		try
		{
			UIBattleManager instance = Singleton<UIBattleManager>.Instance;
			UIBattleMain val = ((instance != null) ? instance.battleWindow : null);
			if (val != null)
			{
				if (_nativeMagicList != null)
				{
					val.m_windowListDict[(WindowType)8] = _nativeMagicList;
				}
				if (_nativeMagicBasis != null)
				{
					val.m_windowBasisDict[(WindowType)8] = _nativeMagicBasis;
				}
				if (_nativeMagicSummary != null)
				{
					val.m_summaryDict[(WindowType)8] = _nativeMagicSummary;
				}
			}
		}
		catch (Exception ex)
		{
			Plugin.ModLog.LogWarning((object)("Restauration de la fenêtre Magic impossible: " + ex.Message));
		}
		finally
		{
			_windowMappingActive = false;
			_mappedWindowOwner = null;
			_nativeMagicList = null;
			_nativeMagicBasis = null;
			_nativeMagicSummary = null;
		}
	}

	private static void ValidateRuneAndOpenSpells()
	{
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Expected O, but got Unknown
		//IL_0369: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Expected O, but got Unknown
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Expected O, but got Unknown
		//IL_0608: Unknown result type (might be due to invalid IL or missing references)
		//IL_060f: Expected O, but got Unknown
		_suppressDecideFrame = Time.frameCount;
		if (!TryGetPlayer(_playerNo, out PLAYER_BASE player) || player == null)
		{
			return;
		}
		BattleBase.WINDOW_WORK window_work = BattleBase.window_work;
		int num = Math.Clamp((window_work != null) ? window_work.window_index : _selectedSlot, 0, 2);
		RuneState state = RuneSlotService.PeekStoredCombatState(player);
		int unlockedSlotCount = RuneSlotService.GetUnlockedSlotCount(player);
		if (!IsSelectable(state, unlockedSlotCount, num))
		{
			SetNativeCursor(_selectedSlot);
			return;
		}
		RuneState runeState = RuneSlotService.SelectStoredCombatSlot(player, num);
		_selectedSlot = num;
		byte b = runeState.RuneIds[num];
		bool flag = default(bool);
		BepInExInfoLogInterpolatedStringHandler val;
		if (_selectorCarrierPrimed)
		{
			ManualLogSource modLog = Plugin.ModLog;
			val = new BepInExInfoLogInterpolatedStringHandler(80, 3, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R156 DIRECT-CARRIER VALIDATION: slot reel=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(num + 1);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" rune=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<byte>(b);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; ");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("carrier retire, nativeMonsyo=");
				BepInExInfoLogInterpolatedStringHandler obj = val;
				PLAYER_MONSYO monsyo_data = player.monsyo_data;
				((BepInExLogInterpolatedStringHandler)obj).AppendFormatted<byte>((byte)((monsyo_data != null) ? monsyo_data.monsyo_id : 0));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
			}
			modLog.LogInfo(val);
			_selectorCarrierPrimed = false;
			_selectorCarrierSlot = -1;
			_selectorCarrierRune = 0;
		}
		_directNativeRune = IsDirectNativeRune(b);
		if (_directNativeRune)
		{
			EnsureDirectRuneCommandItemZero(_playerNo, "validation-rune-directe");
			DirectRuneDiagnostic.Arm(_playerNo, player.chara_no, b, num);
			if (RuneSlotService.PrepareDirectNativeRuntime(player, b, out var previousExp))
			{
				ManualLogSource modLog2 = Plugin.ModLog;
				val = new BepInExInfoLogInterpolatedStringHandler(79, 5, out flag);
				if (flag)
				{
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R156 DIRECT-RUNTIME PREPARE: player=");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_playerNo);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", chara=");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(player.chara_no);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", rune=");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<byte>(b);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; ");
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("monsyo_exp=");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<ushort>(previousExp);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("->0; stockage=");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(RuneSlotService.DescribeStoredCombatState(player));
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
				}
				modLog2.LogInfo(val);
			}
		}
		UIBattleManager instance = Singleton<UIBattleManager>.Instance;
		if (instance == null)
		{
			return;
		}
		RestoreNativeWindowMapping();
		_stage = CombatStage.Spells;
		_spellConfirmBlockedUntilRelease = true;
		window_work = BattleBase.window_work;
		bool flag2 = false;
		int num2 = ((window_work != null) ? window_work.index_level : (-1));
		int num3 = ((window_work != null) ? window_work.window_step : (-1));
		if (_selectorOpenedFromMagicBack && !_directNativeRune && window_work != null && _runeEntryStateCaptured && window_work.index_level == _runeEntryIndexLevel && window_work.window_step == _runeEntryWindowStep)
		{
			window_work.index_level = 2;
			window_work.window_step = 0;
			window_work.selected_item = 0;
			window_work.selected_monster = 0;
			window_work.return_value = 0;
			flag2 = true;
		}
		PLAYER_MONSYO monsyo_data2 = player.monsyo_data;
		byte b2 = (byte)((monsyo_data2 != null) ? monsyo_data2.monsyo_id : 0);
		_selectorOpenedFromMagicBack = false;
		_rebuildingSpells = true;
		int num4 = -1;
		try
		{
			num4 = BattleBase.make_magic_window(_playerNo, true);
			if (!_directNativeRune)
			{
				UIWindowList magic = instance.battleWindow.m_magic;
				if (!((UIWindowBasis)magic).IsOpen && !((UIWindowBasis)magic).IsOpening)
				{
					instance.OpenWindowUI((WindowType)8, true);
				}
				instance.battleWindow.FocusWindow((WindowType)8, 0);
				magic.SetSelectCell(0);
			}
		}
		catch (Exception ex)
		{
			Plugin.ModLog.LogWarning((object)("R101 - reconstruction native des magies impossible: " + ex));
			_stage = CombatStage.Idle;
		}
		finally
		{
			_rebuildingSpells = false;
		}
		if (_directNativeRune && RuneSlotService.PrepareDirectNativeRuntime(player, b, out var previousExp2) && previousExp2 != 0)
		{
			ManualLogSource modLog3 = Plugin.ModLog;
			val = new BepInExInfoLogInterpolatedStringHandler(68, 2, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R156 DIRECT-RUNTIME AFTER-BUILD: monsyo_exp=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<ushort>(previousExp2);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("->0; make_magic_window=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(num4);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
			}
			modLog3.LogInfo(val);
		}
		if (_directNativeRune)
		{
			EnsureDirectRuneCommandItemZero(_playerNo, "apres-make_magic_window");
		}
		window_work = BattleBase.window_work;
		if (window_work != null)
		{
			window_work.window_index = 0;
			window_work.selected_item = 0;
			window_work.selected_monster = 0;
			window_work.return_value = 0;
			// R158 : le selecteur avait force magic_kazu=3 (3 emplacements). make_magic_window renvoie le
			// nombre de sorts sans mettre a jour magic_kazu : sans cette ligne le curseur bouclait apres le
			// 3e sort et le 4e sort etait inaccessible.
			if (!_directNativeRune && num4 > 0)
			{
				window_work.magic_kazu = num4;
			}
			if (_directNativeRune)
			{
				window_work.magic_kazu = 1;
				window_work.index_level = 2;
				window_work.window_step = 0;
			}
		}
		if (!_directNativeRune)
		{
			instance.SetCursorUI((WindowType)8, 0);
		}
		_forceNativeSpellSelection = _stage == CombatStage.Spells && !_directNativeRune;
		_spellHandlerRerouteLogged = false;
		_r52TraceBudget = 12;
		UIBattleMain battleWindow = instance.battleWindow;
		UIWindowList val2 = ((battleWindow != null) ? battleWindow.m_magic : null);
		if (_directNativeRune)
		{
			Plugin.ModLog.LogInfo((object)($"R128 DIRECT-NATIVE - rune {num + 1} id={b}; aucune Win_Magic ouverte; make_magic_window={num4}; etat vanilla force uniquement sur les valeurs prouvees magic_kazu={((window_work != null) ? window_work.magic_kazu : (-1))}/item={((window_work != null) ? window_work.selected_item : (-1))}/level={((window_work != null) ? window_work.index_level : (-1))}/step={((window_work != null) ? window_work.window_step : (-1))}. " + "command_magic02/10/99 restent 100 % natifs."));
			Plugin.ModLog.LogInfo((object)($"R128 DIRECT-DIAG BUILD - player={_playerNo}, chara={player.chara_no}, slot={num + 1}, rune={b}; " + DirectRuneDiagnostic.State()));
			return;
		}
		ManualLogSource modLog4 = Plugin.ModLog;
		val = new BepInExInfoLogInterpolatedStringHandler(83, 8, out flag);
		if (flag)
		{
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R133 - Rune->Magic: slot=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(num + 1);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", rune=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<byte>(b);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", ");
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("monsyo_id_avant_build=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<byte>(b2);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", ");
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("etat=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(num2);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("/");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(num3);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("->");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>((window_work != null) ? window_work.index_level : (-1));
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("/");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>((window_work != null) ? window_work.window_step : (-1));
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", ");
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("sync_apres_B=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<bool>(flag2);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
		}
		modLog4.LogInfo(val);
		Plugin.ModLog.LogInfo((object)($"R101 R52-FIX - rune {num + 1} activee; make_magic_window={num4}, magicCells={((val2 == null) ? null : val2.cellList?.Count).GetValueOrDefault()}, nativeStateConserve=level={((window_work != null) ? window_work.index_level : (-1))}/step={((window_work != null) ? window_work.window_step : (-1))}; " + "command_magic02/10 sont gardes hors de la cible jusqu'a une vraie validation de sort."));
	}

	private static void ReturnFromSpellsToRunes()
	{
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Expected O, but got Unknown
		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ed: Expected O, but got Unknown
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Expected O, but got Unknown
		bool directNativeRune = _directNativeRune;
		_returnToRunesRequested = false;
		_forceNativeSpellSelection = false;
		_directNativeRune = false;
		_spellHandlerRerouteLogged = false;
		UIBattleManager instance = Singleton<UIBattleManager>.Instance;
		if (instance == null || !TryGetPlayer(_playerNo, out PLAYER_BASE player) || player == null)
		{
			Reset();
			return;
		}
		try
		{
			if (instance.IsOpenWindowUI((WindowType)8))
			{
				instance.CloseWindowUI((WindowType)8);
			}
			SetPmInt(_playerNo, "c_item", 0);
			SetPmInt(_playerNo, "c_mokuhyo", 0);
			RuneState runeState = RuneSlotService.RestoreStoredCombatState(player);
			UIWindowList emblem = instance.battleWindow.m_emblem;
			CaptureEmblemGeometry(emblem);
			BuildEmblemRows(player, emblem);
			MapNativeMagicWindowToEmblem(instance, emblem);
			int unlockedSlotCount = RuneSlotService.GetUnlockedSlotCount(player);
			_selectedSlot = FirstSelectableSlot(runeState, unlockedSlotCount, runeState.ActiveIndex);
			BattleBase.WINDOW_WORK window_work = BattleBase.window_work;
			bool flag = default(bool);
			BepInExInfoLogInterpolatedStringHandler val;
			if (window_work != null)
			{
				if (directNativeRune)
				{
					if (_runeEntryStateCaptured)
					{
						window_work.window_step = _runeEntryWindowStep;
						window_work.index_level = _runeEntryIndexLevel;
						window_work.return_value = _runeEntryReturnValue;
						window_work.selected_item = _runeEntrySelectedItem;
						window_work.selected_monster = _runeEntrySelectedMonster;
					}
					else
					{
						window_work.selected_item = 0;
						window_work.selected_monster = 0;
						window_work.return_value = 0;
					}
				}
				else
				{
					window_work.index_level = 1;
					window_work.window_step = 2;
					window_work.return_value = 0;
					window_work.selected_item = 0;
					window_work.selected_monster = (_runeEntryStateCaptured ? _runeEntrySelectedMonster : 0);
					ManualLogSource modLog = Plugin.ModLog;
					val = new BepInExInfoLogInterpolatedStringHandler(81, 2, out flag);
					if (flag)
					{
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R156 MAGIC-BACK STABLE-SELECTOR: normal magic -> level=1/step=2; ");
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral("slot=");
						((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_selectedSlot + 1);
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; storage=");
						((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(RuneSlotService.DescribeStoredCombatState(player));
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
					}
					modLog.LogInfo(val);
				}
				window_work.magic_kazu = 3;
				window_work.window_index = _selectedSlot;
			}
			_stage = CombatStage.RuneSelector;
			_geometryCorrections = 0;
			_selectorOpenedFromMagicBack = true;
			_spellConfirmBlockedUntilRelease = false;
			_confirmArmed = !ProbeIsOn((GRInputManager.Type)4);
			_selectorUpHeld = ProbeIsOn((GRInputManager.Type)0);
			_selectorDownHeld = ProbeIsOn((GRInputManager.Type)1);
			_cancelBlockedUntilRelease = true;
			_selectorLogged = false;
			_selectorNeedsSettledRebuild = true;
			EnsureThreeRowHeight(emblem);
			instance.OpenWindowUI((WindowType)8, true);
			ManualLogSource modLog2 = Plugin.ModLog;
			val = new BepInExInfoLogInterpolatedStringHandler(88, 4, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R132 - retour B: stockage 3 runes preserve avant reconstruction Emblem; ");
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("active=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(runeState.ActiveIndex + 1);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(", ids=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<byte>(runeState.RuneIds[0]);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("/");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<byte>(runeState.RuneIds[1]);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("/");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<byte>(runeState.RuneIds[2]);
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
			}
			modLog2.LogInfo(val);
			ManualLogSource modLog3 = Plugin.ModLog;
			val = new BepInExInfoLogInterpolatedStringHandler(162, 3, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R156 - B depuis Magic/direct: retour a Emblem; stockage=");
				((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(RuneSlotService.DescribeStoredCombatState(player));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; etat natif Runes restaure level=");
				BepInExInfoLogInterpolatedStringHandler obj = val;
				BattleBase.WINDOW_WORK window_work2 = BattleBase.window_work;
				((BepInExLogInterpolatedStringHandler)obj).AppendFormatted<int>((window_work2 != null) ? window_work2.index_level : (-1));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("/step=");
				BepInExInfoLogInterpolatedStringHandler obj2 = val;
				BattleBase.WINDOW_WORK window_work3 = BattleBase.window_work;
				((BepInExLogInterpolatedStringHandler)obj2).AppendFormatted<int>((window_work3 != null) ? window_work3.window_step : (-1));
				((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; prochain B du selecteur restaure l etat Command capture avant A.");
			}
			modLog3.LogInfo(val);
			LogR140Snapshot("APRES-RETOUR-MAGIC-VERS-RUNES");
		}
		catch (Exception ex)
		{
			Plugin.ModLog.LogWarning((object)("Retour natif vers Emblem impossible: " + ex));
			Reset();
		}
	}

	private static void CorrectNativeSelection()
	{
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Expected O, but got Unknown
		if (!TryGetPlayer(_playerNo, out PLAYER_BASE player) || player == null)
		{
			return;
		}
		UIBattleManager instance = Singleton<UIBattleManager>.Instance;
		object obj;
		if (instance == null)
		{
			obj = null;
		}
		else
		{
			UIBattleMain battleWindow = instance.battleWindow;
			obj = ((battleWindow != null) ? battleWindow.m_emblem : null);
		}
		UIWindowList val = (UIWindowList)obj;
		BattleBase.WINDOW_WORK window_work = BattleBase.window_work;
		if (instance == null || val == null || window_work == null || !instance.IsOpenWindowUI((WindowType)8))
		{
			return;
		}
		EnsureThreeRowHeight(val);
		RuneState state = RuneSlotService.PeekStoredCombatState(player);
		int unlockedSlotCount = RuneSlotService.GetUnlockedSlotCount(player);
		bool flag = _selectorNeedsSettledRebuild && !((UIWindowBasis)val).IsOpening;
		BuildEmblemRows(player, val, flag);
		if (flag)
		{
			_selectorNeedsSettledRebuild = false;
			instance.battleWindow.FocusWindow((WindowType)8, _selectedSlot);
			SetNativeCursor(_selectedSlot);
			EnsureThreeRowHeight(val);
			ManualLogSource modLog = Plugin.ModLog;
			bool flag2 = default(bool);
			BepInExInfoLogInterpolatedStringHandler val2 = new BepInExInfoLogInterpolatedStringHandler(108, 8, out flag2);
			if (flag2)
			{
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("R156 UI-ROWS STABILISEES APRES OUVERTURE: frame=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(Time.frameCount);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("; ");
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("player=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_playerNo);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(", chara=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(_playerCharacterNo);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(", cells=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(val.cellList?.Count ?? 0);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral(", ");
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("enabledCells=");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<int>(val.m_enableCellNum);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("; rows=[");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(Labels[0]);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("] | [");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(Labels[1]);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("] | [");
				((BepInExLogInterpolatedStringHandler)val2).AppendFormatted<string>(Labels[2]);
				((BepInExLogInterpolatedStringHandler)val2).AppendLiteral("].");
			}
			modLog.LogInfo(val2);
		}
		int num = Math.Clamp(window_work.window_index, 0, 2);
		int num2 = num;
		if (!IsSelectable(state, unlockedSlotCount, num))
		{
			bool flag3 = ProbeIsPress((GRInputManager.Type)0) || ProbeIsRepeat((GRInputManager.Type)0);
			bool flag4 = ProbeIsPress((GRInputManager.Type)1) || ProbeIsRepeat((GRInputManager.Type)1);
			num2 = FindNextSelectable(state, unlockedSlotCount, num, (flag4 && !flag3) ? 1 : (-1));
		}
		else if (_selectorMoveDirection != 0 && _selectorIndexBeforeCommand >= 0 && num == _selectorIndexBeforeCommand)
		{
			int num3 = FindNextSelectable(state, unlockedSlotCount, num, _selectorMoveDirection);
			if (num3 != num)
			{
				num2 = num3;
				Plugin.ModLog.LogInfo((object)($"R131 - navigation Emblem edge fallback: slot {num + 1}->{num2 + 1}; " + "un seul deplacement pour ce front Haut/Bas."));
			}
		}
		_selectedSlot = num2;
		window_work.window_index = num2;
		SetNativeCursor(num2);
		_selectorIndexBeforeCommand = num2;
		_selectorMoveDirection = 0;
	}

	private static void SetNativeCursor(int slot)
	{
		UIBattleManager instance = Singleton<UIBattleManager>.Instance;
		object obj;
		if (instance == null)
		{
			obj = null;
		}
		else
		{
			UIBattleMain battleWindow = instance.battleWindow;
			obj = ((battleWindow != null) ? battleWindow.m_emblem : null);
		}
		UIWindowList val = (UIWindowList)obj;
		if (instance != null && val != null && slot >= 0 && slot < 3)
		{
			val.SetSelectCell(slot);
			instance.SetCursorUI((WindowType)8, slot);
			UISummary summaryEmblem = instance.battleWindow.m_summaryEmblem;
			if (summaryEmblem != null)
			{
				summaryEmblem.Set(Labels[slot], Summaries[slot]);
			}
		}
	}

	private static void CaptureEmblemGeometry(UIWindowList emblem)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		UIWindowList? geometryOwner = _geometryOwner;
		if (!(((geometryOwner != null) ? new IntPtr?(((Il2CppObjectBase)geometryOwner).Pointer) : null) == ((Il2CppObjectBase)emblem).Pointer))
		{
			_geometryOwner = emblem;
			// R160 : la taille native n'est memorisee qu'une fois par fenetre. La re-memoriser alors que la
			// fenetre etait encore agrandie (321 au lieu de 214 dans le log) faussait toutes les hauteurs.
			IntPtr pointer = ((Il2CppObjectBase)emblem).Pointer;
			if (!BaseGeometry.TryGetValue(pointer, out var value))
			{
				if (BaseGeometry.Count > 32)
				{
					BaseGeometry.Clear();
				}
				value = (emblem.m_rectTransform.sizeDelta.y, emblem.m_contents.sizeDelta.y, emblem.m_unFocusedHeight);
				BaseGeometry[pointer] = value;
			}
			_originalWindowSize = new Vector2(emblem.m_rectTransform.sizeDelta.x, value.Window);
			_originalContentsSize = new Vector2(emblem.m_contents.sizeDelta.x, value.Contents);
			_originalUnfocusedHeight = value.Unfocused;
			_expandedHeightDelta = 0f;
		}
	}

	private static readonly System.Collections.Generic.Dictionary<IntPtr, (float Window, float Contents, float Unfocused)> BaseGeometry = new System.Collections.Generic.Dictionary<IntPtr, (float, float, float)>();

	private static float _rowHeight;

	private static int _geometryCorrections;

	// R160 : appele a chaque image (LateUpdate, apres les animations du jeu). Quand la fenetre Runes reprend
	// le focus (retour par B depuis les sorts), l'animation native lui redonne sa hauteur d'une seule rune et
	// les 3 lignes se superposaient. On re-impose la hauteur 3 lignes tant que le selecteur est ouvert.
	internal static void MaintainSelectorGeometry()
	{
		if (_stage != CombatStage.RuneSelector || _geometryOwner == null || _expandedHeightDelta <= 0f)
		{
			return;
		}
		try
		{
			UIWindowList geometryOwner = _geometryOwner;
			float num = _originalWindowSize.y + _expandedHeightDelta;
			float num2 = _originalContentsSize.y + _expandedHeightDelta;
			Vector2 sizeDelta = geometryOwner.m_rectTransform.sizeDelta;
			Vector2 sizeDelta2 = geometryOwner.m_contents.sizeDelta;
			if (Math.Abs(sizeDelta.y - num) < 0.5f && Math.Abs(sizeDelta2.y - num2) < 0.5f)
			{
				return;
			}
			geometryOwner.m_rectTransform.sizeDelta = new Vector2(sizeDelta.x, num);
			geometryOwner.m_contents.sizeDelta = new Vector2(sizeDelta2.x, num2);
			geometryOwner.m_unFocusedHeight = _originalUnfocusedHeight + _expandedHeightDelta;
			LayoutRebuilder.ForceRebuildLayoutImmediate(geometryOwner.m_contents);
			if (_geometryCorrections < 5)
			{
				_geometryCorrections++;
				Plugin.ModLog.LogInfo((object)$"R160 GEOMETRIE Runes re-imposee : fenetre {sizeDelta.y:0.##}->{num:0.##}, contenu {sizeDelta2.y:0.##}->{num2:0.##}.");
			}
		}
		catch
		{
		}
	}

	private static void EnsureThreeRowHeight(UIWindowList emblem)
	{
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		UIWindowList? geometryOwner = _geometryOwner;
		if (((geometryOwner != null) ? new IntPtr?(((Il2CppObjectBase)geometryOwner).Pointer) : null) != ((Il2CppObjectBase)emblem).Pointer)
		{
			CaptureEmblemGeometry(emblem);
		}
		Il2CppSystem.Collections.Generic.List<UIWindowListCell> cellList = emblem.cellList;
		if (cellList == null || cellList.Count < 3)
		{
			return;
		}
		Canvas.ForceUpdateCanvases();
		LayoutRebuilder.ForceRebuildLayoutImmediate(emblem.m_contents);
		float num = cellList[0].height;
		if (num <= 0.01f)
		{
			Rect rect = cellList[0].rectTransform.rect;
			num = rect.height;
		}
		if (num <= 0.01f)
		{
			UIBattleManager instance = Singleton<UIBattleManager>.Instance;
			object obj;
			if (instance == null)
			{
				obj = null;
			}
			else
			{
				UIBattleMain battleWindow = instance.battleWindow;
				obj = ((battleWindow != null) ? battleWindow.m_magic : null);
			}
			UIWindowList val = (UIWindowList)obj;
			if (((val == null) ? null : val.cellList?.Count).GetValueOrDefault() > 0)
			{
				num = val.cellList[0].height;
			}
		}
		// R160 : juste apres la recreation des cellules (retour par B), leur hauteur peut etre fausse.
		// On garde la plus grande hauteur de ligne valide deja mesuree.
		if (num > 0.01f && num > _rowHeight)
		{
			_rowHeight = num;
		}
		else if (_rowHeight > 0.01f)
		{
			num = _rowHeight;
		}
		if (num <= 0.01f)
		{
			num = Math.Max(1f, _originalUnfocusedHeight * 0.55f);
		}
		float num2 = ((emblem.m_contentsLayout != null) ? ((HorizontalOrVerticalLayoutGroup)emblem.m_contentsLayout).spacing : ((emblem.m_layout != null) ? ((HorizontalOrVerticalLayoutGroup)emblem.m_layout).spacing : 0f));
		_expandedHeightDelta = Math.Max(0f, 2f * (num + num2));
		// R158 : la largeur reste celle calculee par le jeu pour le personnage courant (longueur des noms).
		// Reprendre la largeur memorisee sur un autre personnage faisait deborder/superposer le texte.
		emblem.m_rectTransform.sizeDelta = new Vector2(emblem.m_rectTransform.sizeDelta.x, _originalWindowSize.y + _expandedHeightDelta);
		emblem.m_contents.sizeDelta = new Vector2(emblem.m_contents.sizeDelta.x, _originalContentsSize.y + _expandedHeightDelta);
		emblem.m_unFocusedHeight = _originalUnfocusedHeight + _expandedHeightDelta;
		LayoutRebuilder.ForceRebuildLayoutImmediate(emblem.m_contents);
		Canvas.ForceUpdateCanvases();
	}

	private static void RestoreEmblemGeometry()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		UIWindowList geometryOwner = _geometryOwner;
		if (geometryOwner == null)
		{
			return;
		}
		try
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(geometryOwner.m_contents);
			geometryOwner.m_rectTransform.sizeDelta = new Vector2(geometryOwner.m_rectTransform.sizeDelta.x, _originalWindowSize.y);
			geometryOwner.m_contents.sizeDelta = new Vector2(geometryOwner.m_contents.sizeDelta.x, _originalContentsSize.y);
			geometryOwner.m_unFocusedHeight = _originalUnfocusedHeight;
		}
		catch
		{
		}
		finally
		{
			_geometryOwner = null;
			_expandedHeightDelta = 0f;
		}
	}

	private static bool IsRealMagicWindowActive()
	{
		UIBattleManager instance = Singleton<UIBattleManager>.Instance;
		object obj;
		if (instance == null)
		{
			obj = null;
		}
		else
		{
			UIBattleMain battleWindow = instance.battleWindow;
			obj = ((battleWindow != null) ? battleWindow.m_magic : null);
		}
		UIWindowList val = (UIWindowList)obj;
		if (instance != null && val != null && !_windowMappingActive)
		{
			if (!instance.IsOpenWindowUI((WindowType)8) && !((UIWindowBasis)val).IsOpen)
			{
				return ((UIWindowBasis)val).IsOpening;
			}
			return true;
		}
		return false;
	}

	internal static bool IsDiagnosticTraceActive()
	{
		return Time.frameCount <= _diagnosticTraceUntilFrame;
	}

	internal static void TraceMagicMethod(string phase, MethodBase? method, int stepLevel, int nativeResult)
	{
		if (IsDiagnosticTraceActive())
		{
			string value = method?.Name ?? "?";
			LogDiagnosticSnapshot($"R41 TRACE {phase} {value}(step={stepLevel}) result={nativeResult}");
		}
	}

	internal static void TraceCancelInput(string source, bool value)
	{
		if (IsDiagnosticTraceActive() && value)
		{
			LogDiagnosticSnapshot("R41 TRACE Cancel TRUE via " + source);
		}
	}

	private static void LogDiagnosticSnapshot(string label)
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected O, but got Unknown
		try
		{
			UIBattleManager instance = Singleton<UIBattleManager>.Instance;
			object obj;
			if (instance == null)
			{
				obj = null;
			}
			else
			{
				UIBattleMain battleWindow = instance.battleWindow;
				obj = ((battleWindow != null) ? battleWindow.m_magic : null);
			}
			UIWindowList val = (UIWindowList)obj;
			object obj2;
			if (instance == null)
			{
				obj2 = null;
			}
			else
			{
				UIBattleMain battleWindow2 = instance.battleWindow;
				obj2 = ((battleWindow2 != null) ? battleWindow2.m_emblem : null);
			}
			UIWindowList val2 = (UIWindowList)obj2;
			BattleBase.WINDOW_WORK window_work = BattleBase.window_work;
			ManualLogSource modLog = Plugin.ModLog;
			bool flag = default(bool);
			BepInExInfoLogInterpolatedStringHandler val3 = new BepInExInfoLogInterpolatedStringHandler(115, 13, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<string>(label);
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral(" | frame=");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<int>(Time.frameCount);
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral(" stage=");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<CombatStage>(_stage);
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral(" player=");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<int>(_playerNo);
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral(" slot=");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<int>(_selectedSlot);
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral(" ");
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral("window_index=");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<string>(((window_work != null) ? window_work.window_index.ToString() : null) ?? "null");
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral(" magic_kazu=");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<string>(((window_work != null) ? window_work.magic_kazu.ToString() : null) ?? "null");
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral(" ");
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral("Magic(open=");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<bool>(val != null && ((UIWindowBasis)val).IsOpen);
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral(",opening=");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<bool>(val != null && ((UIWindowBasis)val).IsOpening);
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral(",focus=");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<bool>(val != null && val.IsFocus);
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral(") ");
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral("Emblem(open=");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<bool>(val2 != null && ((UIWindowBasis)val2).IsOpen);
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral(",opening=");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<bool>(val2 != null && ((UIWindowBasis)val2).IsOpening);
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral(",focus=");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<bool>(val2 != null && val2.IsFocus);
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral(")");
			}
			modLog.LogInfo(val3);
		}
		catch (Exception ex)
		{
			Plugin.ModLog.LogDebug((object)("R41 TRACE snapshot impossible: " + ex.Message));
		}
	}

	private static bool ProbeIsPress(GRInputManager.Type input)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		_probingNativeInput = true;
		try
		{
			return GRInputManager.IsPress(input);
		}
		finally
		{
			_probingNativeInput = false;
		}
	}

	private static bool ProbeIsRepeat(GRInputManager.Type input)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		_probingNativeInput = true;
		try
		{
			return GRInputManager.IsRepeat(input);
		}
		finally
		{
			_probingNativeInput = false;
		}
	}

	private static bool ProbeIsOn(GRInputManager.Type input)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		_probingNativeInput = true;
		try
		{
			return GRInputManager.IsOn(input);
		}
		finally
		{
			_probingNativeInput = false;
		}
	}

	private static int FirstSelectableSlot(RuneState state, int unlocked, int preferred)
	{
		if (IsSelectable(state, unlocked, preferred))
		{
			return preferred;
		}
		for (int i = 0; i < 3; i++)
		{
			if (IsSelectable(state, unlocked, i))
			{
				return i;
			}
		}
		return 0;
	}

	private static int FindNextSelectable(RuneState state, int unlocked, int current, int direction)
	{
		int num = ((direction >= 0) ? 1 : (-1));
		for (int i = 1; i <= 3; i++)
		{
			int num2 = (current + num * i + 6) % 3;
			if (IsSelectable(state, unlocked, num2))
			{
				return num2;
			}
		}
		return current;
	}

	private static bool IsSelectable(RuneState state, int unlocked, int slot)
	{
		if (slot >= 0 && slot < 3 && slot < unlocked && state.RuneIds[slot] != 0)
		{
			return !IsPassiveRune(state.RuneIds[slot]);
		}
		return false;
	}

	private static bool HasSelectableSlot(RuneState state, int unlocked)
	{
		for (int i = 0; i < 3; i++)
		{
			if (IsSelectable(state, unlocked, i))
			{
				return true;
			}
		}
		return false;
	}

	internal static void EnsureDirectRuneCommandItemZero(int playerNo, string source)
	{
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Expected O, but got Unknown
		int valueOrDefault = ReadIntMember(GetPmEntry(playerNo), "c_item").GetValueOrDefault();
		if (valueOrDefault == 0)
		{
			return;
		}
		if (SetPmInt(playerNo, "c_item", 0))
		{
			Plugin.ModLog.LogInfo((object)($"R156 DIRECT-CITEM ZERO ({source}): player={playerNo}; c_item={valueOrDefault}->0; " + "aucun autre champ de commande modifie."));
			return;
		}
		ManualLogSource modLog = Plugin.ModLog;
		bool flag = default(bool);
		BepInExWarningLogInterpolatedStringHandler val = new BepInExWarningLogInterpolatedStringHandler(61, 3, out flag);
		if (flag)
		{
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R156 DIRECT-CITEM ZERO (");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(source);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(") impossible: player=");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(playerNo);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; c_item reste ");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(valueOrDefault);
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
		}
		modLog.LogWarning(val);
	}

	private static bool SetPmInt(int playerNo, string memberName, int value)
	{
		object pmEntry = GetPmEntry(playerNo);
		if (pmEntry == null)
		{
			return false;
		}
		Type type = pmEntry.GetType();
		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		try
		{
			PropertyInfo property = type.GetProperty(memberName, bindingAttr);
			if ((object)property != null && property.CanWrite)
			{
				object value2 = Convert.ChangeType(value, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
				property.SetValue(pmEntry, value2);
				return true;
			}
		}
		catch
		{
		}
		try
		{
			FieldInfo field = type.GetField(memberName, bindingAttr);
			if ((object)field != null)
			{
				object value3 = Convert.ChangeType(value, Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType);
				field.SetValue(pmEntry, value3);
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static object? GetPmEntry(int playerNo)
	{
		try
		{
			object obj = ReadMemberObject(BattleBase.battle_work, "pm_data");
			if (obj == null || playerNo < 0)
			{
				return null;
			}
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			PropertyInfo[] properties = obj.GetType().GetProperties(bindingAttr);
			foreach (PropertyInfo propertyInfo in properties)
			{
				ParameterInfo[] indexParameters = propertyInfo.GetIndexParameters();
				if (propertyInfo.CanRead && indexParameters.Length == 1)
				{
					object obj2 = Convert.ChangeType(playerNo, Nullable.GetUnderlyingType(indexParameters[0].ParameterType) ?? indexParameters[0].ParameterType);
					return propertyInfo.GetValue(obj, new object[1] { obj2 });
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static object? ReadMemberObject(object? obj, string name)
	{
		if (obj == null)
		{
			return null;
		}
		Type type = obj.GetType();
		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		try
		{
			PropertyInfo property = type.GetProperty(name, bindingAttr);
			if ((object)property != null && property.CanRead && property.GetIndexParameters().Length == 0)
			{
				return property.GetValue(obj);
			}
		}
		catch
		{
		}
		try
		{
			FieldInfo field = type.GetField(name, bindingAttr);
			if ((object)field != null)
			{
				return field.GetValue(obj);
			}
		}
		catch
		{
		}
		return null;
	}

	private static int RequiredMagic(int slot)
	{
		if (slot > 0)
		{
			if (slot != 1)
			{
				return Plugin.ThirdSlotMag.Value;
			}
			return Plugin.SecondSlotMag.Value;
		}
		return 0;
	}

	private static bool TryGetPlayer(int playerNo, out PLAYER_BASE? player)
	{
		player = null;
		try
		{
			BATTLE_WORK battle_work = BattleBase.battle_work;
			if (((battle_work != null) ? battle_work.p_battle_data : null) == null || playerNo < 0 || playerNo >= ((Il2CppArrayBase<P_BATTLE_DATA>)(object)battle_work.p_battle_data).Length)
			{
				return false;
			}
			P_BATTLE_DATA obj = ((Il2CppArrayBase<P_BATTLE_DATA>)(object)battle_work.p_battle_data)[playerNo];
			PLAYER_DATA val = ((obj != null) ? obj.player_data : null);
			if (((val != null) ? val.player_base : null) == null)
			{
				return false;
			}
			player = val.player_base;
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void RefreshCommandActorFromNativeState()
	{
		try
		{
			BattleBase.WINDOW_WORK window_work = BattleBase.window_work;
			if (_playerNo >= 0 && TryGetPlayer(_playerNo, out PLAYER_BASE player) && player != null)
			{
				_playerCharacterNo = player.chara_no;
				if (string.IsNullOrEmpty(_playerSource))
				{
					_playerSource = "make_command_window";
				}
				return;
			}
			PLAYER_DATA val = ((window_work != null) ? window_work.selected_player_data : null);
			if (((val != null) ? val.player_base : null) != null && TryFindBattlePlayer(val, out int playerNo, out player) && player != null)
			{
				_playerNo = playerNo;
				_playerCharacterNo = player.chara_no;
				_playerSource = "selected_player_data";
			}
			else
			{
				if (window_work == null)
				{
					return;
				}
				int[] array = new int[2] { window_work.window_player_index, window_work.selected_player };
				foreach (int num in array)
				{
					if (TryGetPlayer(num, out player) && player != null)
					{
						_playerNo = num;
						_playerCharacterNo = player.chara_no;
						_playerSource = ((num == window_work.window_player_index) ? "window_player_index" : "selected_player");
						break;
					}
				}
			}
		}
		catch (Exception ex)
		{
			Plugin.ModLog.LogWarning((object)("Identification du personnage actif impossible; index natif conservé: " + ex.Message));
		}
	}

	private static bool TryFindBattlePlayer(PLAYER_DATA selected, out int playerNo, out PLAYER_BASE? player)
	{
		playerNo = -1;
		player = null;
		BATTLE_WORK battle_work = BattleBase.battle_work;
		if (((battle_work != null) ? battle_work.p_battle_data : null) == null || selected.player_base == null)
		{
			return false;
		}
		PLAYER_BASE player_base = selected.player_base;
		for (int i = 0; i < ((Il2CppArrayBase<P_BATTLE_DATA>)(object)battle_work.p_battle_data).Length; i++)
		{
			P_BATTLE_DATA obj = ((Il2CppArrayBase<P_BATTLE_DATA>)(object)battle_work.p_battle_data)[i];
			PLAYER_DATA val = ((obj != null) ? obj.player_data : null);
			PLAYER_BASE val2 = ((val != null) ? val.player_base : null);
			if (val != null && val2 != null)
			{
				bool num = ((Il2CppObjectBase)val).Pointer == ((Il2CppObjectBase)selected).Pointer;
				bool flag = ((Il2CppObjectBase)val2).Pointer == ((Il2CppObjectBase)player_base).Pointer;
				bool flag2 = val2.chara_no == player_base.chara_no;
				if (num || flag || flag2)
				{
					playerNo = i;
					player = val2;
					return true;
				}
			}
		}
		return false;
	}
}
