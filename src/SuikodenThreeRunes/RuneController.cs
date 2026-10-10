using System;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace SuikodenThreeRunes;

public sealed class RuneController : MonoBehaviour
{
	private enum SlotSelectionSurface
	{
		None,
		Shop,
		Battle,
		MainMenu
	}

	private int _repackFrame;

	private float _messageUntil;

	private string _message = string.Empty;

	private bool _loggedReady;

	private bool _controllerFaulted;

	private bool _guiFaulted;

	private bool _showShopSlots;

	private SlotSelectionSurface _selectorSurface;

	private PLAYER_BASE? _selectorPlayer;

	private int _selectorIndex;

	private bool _pauseBeforeSelector;

	private bool _resumePending;

	private bool _suppressConfirmUntilReleased;

	private PLAYER_BASE? _shopCompatibilityPlayer;

	private int _shopCompatibilityCharaNo = int.MinValue;

	private int _shopLockedCharaNo = int.MinValue;

	private bool _shopReturnPending;

	private bool _shopPlayerWindowWasActive;

	private string _shopSlots = string.Empty;

	private GUIStyle? _shopHeaderStyle;

	private GUIStyle? _shopRowStyle;

	private GUIStyle? _shopActiveStyle;

	private GUIStyle? _shopLockedStyle;

	private GUIStyle? _shopHintStyle;

	public RuneController(IntPtr pointer)
		: base(pointer)
	{
	}

	private void Update()
	{
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Expected O, but got Unknown
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0276: Expected O, but got Unknown
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d2: Expected O, but got Unknown
		//IL_0336: Unknown result type (might be due to invalid IL or missing references)
		//IL_033d: Expected O, but got Unknown
		//IL_03d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03df: Expected O, but got Unknown
		//IL_0477: Unknown result type (might be due to invalid IL or missing references)
		//IL_047e: Expected O, but got Unknown
		//IL_0519: Unknown result type (might be due to invalid IL or missing references)
		//IL_0520: Expected O, but got Unknown
		if (_controllerFaulted)
		{
			return;
		}
		try
		{
			// R159 : remet l'etat compact en memoire apres boutique / Pie-grieche (toutes les ~0,5 s).
			if (++_repackFrame >= 30)
			{
				_repackFrame = 0;
				if (!RuneBattleNativeEmblemFlow.IsBusy)
				{
					RuneSlotService.RepackAll();
				}
			}
			Keyboard current = Keyboard.current;
			Gamepad current2 = Gamepad.current;
			bool flag = (current != null && ((ButtonControl)current.escapeKey).wasPressedThisFrame) || (current2 != null && current2.buttonEast.wasPressedThisFrame);
			bool cancelHeld = (current != null && ((ButtonControl)current.escapeKey).isPressed) || (current2 != null && current2.buttonEast.isPressed);
			bool decidePressed = (current != null && (((ButtonControl)current.enterKey).wasPressedThisFrame || ((ButtonControl)current.numpadEnterKey).wasPressedThisFrame)) || (current2 != null && current2.buttonSouth.wasPressedThisFrame);
			bool upPressed = (current != null && ((ButtonControl)current.upArrowKey).wasPressedThisFrame) || (current2 != null && current2.dpad.up.wasPressedThisFrame);
			bool downPressed = (current != null && ((ButtonControl)current.downArrowKey).wasPressedThisFrame) || (current2 != null && current2.dpad.down.wasPressedThisFrame);
			RuneBattleNativeEmblemFlow.PollPhysicalInputDiagnostics(flag, cancelHeld, decidePressed, upPressed, downPressed);
			RuneBattleNativeEmblemFlow.PollDirectCancelRelease(cancelHeld);
			bool flag2 = RuneBattleNativeEmblemFlow.PollDirectMagicBack(flag);
			if (flag)
			{
				RuneBattleNativeEmblemFlow.LogPhysicalBackRouting(flag2);
			}
			if (flag2)
			{
				return;
			}
			if (_resumePending)
			{
				UpdatePendingResume();
				return;
			}
			if (_selectorSurface != 0)
			{
				UpdateSlotSelectorInput();
				return;
			}
			bool flag3 = default(bool);
			if (_shopCompatibilityPlayer != null && !RuneSlotService.IsRuneShopSelectionWindowVisible())
			{
				RuneSlotService.ExitRuneShopCompatibility(_shopCompatibilityPlayer);
				ManualLogSource modLog = Plugin.ModLog;
				BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(66, 1, out flag3);
				if (flag3)
				{
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R107 boutique fermee : stockage 3 runes restaure pour personnage ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_shopCompatibilityCharaNo);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
				}
				modLog.LogInfo(val);
				_shopCompatibilityPlayer = null;
				_shopCompatibilityCharaNo = int.MinValue;
			}
			bool num = RuneSlotService.IsRuneShopSelectionWindowVisible();
			bool flag4 = (current != null && (((ButtonControl)current.enterKey).wasPressedThisFrame || ((ButtonControl)current.numpadEnterKey).wasPressedThisFrame)) || (current2 != null && current2.buttonSouth.wasPressedThisFrame);
			bool flag5 = (current != null && (((ButtonControl)current.upArrowKey).wasPressedThisFrame || ((ButtonControl)current.downArrowKey).wasPressedThisFrame)) || (current2 != null && (current2.dpad.up.wasPressedThisFrame || current2.dpad.down.wasPressedThisFrame));
			if (!num)
			{
				_shopLockedCharaNo = int.MinValue;
				_shopReturnPending = false;
				_shopPlayerWindowWasActive = false;
			}
			if (_shopLockedCharaNo != int.MinValue && flag)
			{
				_shopReturnPending = true;
				ManualLogSource modLog2 = Plugin.ModLog;
				BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(123, 1, out flag3);
				if (flag3)
				{
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R107 boutique : retour depuis l'inventaire demande; personnage ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_shopLockedCharaNo);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" reste verrouille jusqu'au prochain mouvement de personnage.");
				}
				modLog2.LogInfo(val);
			}
			if (_shopLockedCharaNo != int.MinValue && _shopReturnPending && flag5)
			{
				ManualLogSource modLog3 = Plugin.ModLog;
				BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(75, 1, out flag3);
				if (flag3)
				{
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R107 boutique : retour a la selection personnages confirme; verrou ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_shopLockedCharaNo);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" libere.");
				}
				modLog3.LogInfo(val);
				_shopLockedCharaNo = int.MinValue;
				_shopReturnPending = false;
			}
			if (_shopLockedCharaNo == int.MinValue && RuneSlotService.SyncRuneShopHighlightedPlayer(out var previousSelected, out var currentIndex))
			{
				ManualLogSource modLog4 = Plugin.ModLog;
				BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(52, 2, out flag3);
				if (flag3)
				{
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R107 boutique : curseur personnage synchronise ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(previousSelected);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(" -> ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(currentIndex);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
				}
				modLog4.LogInfo(val);
			}
			if (!RuneSlotService.TryGetSelectedPlayer(_shopLockedCharaNo, out PLAYER_BASE player, out string context) || player == null)
			{
				_showShopSlots = false;
				return;
			}
			bool flag6 = context == "Boutique de runes" && RuneSlotService.IsRuneShopSelectionWindowVisible();
			if (!flag6 && _shopCompatibilityPlayer != null)
			{
				RuneSlotService.ExitRuneShopCompatibility(_shopCompatibilityPlayer);
				ManualLogSource modLog5 = Plugin.ModLog;
				BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(66, 1, out flag3);
				if (flag3)
				{
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R107 boutique fermee : stockage 3 runes restaure pour personnage ");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_shopCompatibilityCharaNo);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
				}
				modLog5.LogInfo(val);
				_shopCompatibilityPlayer = null;
				_shopCompatibilityCharaNo = int.MinValue;
			}
			if (flag6)
			{
				int chara_no = player.chara_no;
				if (_shopCompatibilityPlayer == null || _shopCompatibilityCharaNo != chara_no)
				{
					if (_shopCompatibilityPlayer != null)
					{
						RuneSlotService.ExitRuneShopCompatibility(_shopCompatibilityPlayer);
					}
					_shopCompatibilityPlayer = player;
					_shopCompatibilityCharaNo = chara_no;
					RuneSlotService.EnterRuneShopCompatibility(player);
					ManualLogSource modLog6 = Plugin.ModLog;
					BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(65, 1, out flag3);
					if (flag3)
					{
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R107 boutique : personnage suivi ");
						((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(chara_no);
						((BepInExLogInterpolatedStringHandler)val).AppendLiteral("; compatibilite Retirer activee.");
					}
					modLog6.LogInfo(val);
				}
				else
				{
					RuneSlotService.CaptureForRuneShop(player);
				}
			}
			else if (context != "Combat")
			{
				RuneSlotService.Capture(player);
			}
			bool flag7 = flag6 && RuneSlotService.IsRuneShopPlayerWindowVisible();
			if (_shopLockedCharaNo == int.MinValue && flag6 && flag7 && _shopPlayerWindowWasActive && flag4)
			{
				_shopLockedCharaNo = player.chara_no;
				_shopReturnPending = false;
				ManualLogSource modLog7 = Plugin.ModLog;
				BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(67, 1, out flag3);
				if (flag3)
				{
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral("R107 boutique : personnage valide pour l'inventaire; verrou chara=");
					((BepInExLogInterpolatedStringHandler)val).AppendFormatted<int>(_shopLockedCharaNo);
					((BepInExLogInterpolatedStringHandler)val).AppendLiteral(".");
				}
				modLog7.LogInfo(val);
			}
			_shopPlayerWindowWasActive = flag7;
			bool num2 = context == "Menu" && RuneMainMenuUiPatch.IsRuneMenuActive;
			_showShopSlots = context == "Boutique de runes" && RuneSlotService.IsRuneShopSelectionWindowVisible();
			if (flag7)
			{
				UpdateShopSlotNavigation(player);
			}
			if (num2)
			{
				UpdateMainMenuSlotNavigation(player);
				RuneSlotUi.DrawMainMenu(player, explain: true);
			}
			_shopSlots = (_showShopSlots ? RuneSlotUi.FormatShopOverlay(player, -1, selecting: false, flag7) : string.Empty);
			if (!_loggedReady)
			{
				_loggedReady = true;
				Plugin.ModLog.LogInfo((object)"Suikoden I detect: controle des emplacements de runes actif.");
			}
		}
		catch (Exception value)
		{
			ForceRestoreNativeInput();
			_controllerFaulted = true;
			_showShopSlots = false;
			Plugin.ModLog.LogError((object)("Controleur de runes desactive pour cette session apres une erreur afin " + $"d'eviter une boucle native: {value}"));
		}
	}

	private void UpdateShopSlotNavigation(PLAYER_BASE player)
	{
		Keyboard current = Keyboard.current;
		Gamepad current2 = Gamepad.current;
		bool num = (current != null && ((ButtonControl)current.leftArrowKey).wasPressedThisFrame) || (current2 != null && current2.dpad.left.wasPressedThisFrame);
		bool flag = (current != null && ((ButtonControl)current.rightArrowKey).wasPressedThisFrame) || (current2 != null && current2.dpad.right.wasPressedThisFrame);
		if (num != flag)
		{
			RuneCycleResult result = RuneSlotService.Cycle(player, flag ? 1 : (-1));
			_message = RuneSlotService.FormatStatus(player, "Boutique de runes", result);
			_messageUntil = Time.unscaledTime + 3f;
			_shopSlots = RuneSlotUi.FormatShopOverlay(player, -1, selecting: false, horizontalNavigation: true);
			Plugin.ModLog.LogInfo((object)_message);
		}
	}

	private void UpdateMainMenuSlotNavigation(PLAYER_BASE player)
	{
		Keyboard current = Keyboard.current;
		Gamepad current2 = Gamepad.current;
		bool num = (current != null && ((ButtonControl)current.leftArrowKey).wasPressedThisFrame) || (current2 != null && current2.dpad.left.wasPressedThisFrame);
		bool flag = (current != null && ((ButtonControl)current.rightArrowKey).wasPressedThisFrame) || (current2 != null && current2.dpad.right.wasPressedThisFrame);
		if (num != flag)
		{
			RuneCycleResult result = RuneSlotService.Cycle(player, flag ? 1 : (-1));
			RuneSlotUi.DrawMainMenu(player, explain: true);
			_message = RuneSlotService.FormatStatus(player, "Menu principal", result);
			_messageUntil = Time.unscaledTime + 3f;
			Plugin.ModLog.LogInfo((object)_message);
		}
	}

	private void OpenSlotSelector(SlotSelectionSurface surface, PLAYER_BASE player)
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected O, but got Unknown
		RuneState displayState = RuneSlotService.GetDisplayState(player);
		_selectorSurface = surface;
		_selectorPlayer = player;
		_selectorIndex = Math.Clamp(displayState.ActiveIndex, 0, RuneSlotService.GetUnlockedSlotCount(player) - 1);
		_pauseBeforeSelector = GRInputManager.Pause;
		GRInputManager.Pause = true;
		_suppressConfirmUntilReleased = IsConfirmHeld();
		DrawSlotSelector();
		ManualLogSource modLog = Plugin.ModLog;
		bool flag = default(bool);
		BepInExInfoLogInterpolatedStringHandler val = new BepInExInfoLogInterpolatedStringHandler(35, 1, out flag);
		if (flag)
		{
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("Selection d'emplacement ouverte (");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<string>(GetSelectorContext());
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral(").");
		}
		modLog.LogInfo(val);
	}

	private void UpdateSlotSelectorInput()
	{
		PLAYER_BASE selectorPlayer = _selectorPlayer;
		if (selectorPlayer == null)
		{
			CloseSlotSelector();
			return;
		}
		Keyboard current = Keyboard.current;
		Gamepad current2 = Gamepad.current;
		bool flag = (current != null && ((ButtonControl)current.upArrowKey).wasPressedThisFrame) || (current2 != null && current2.dpad.up.wasPressedThisFrame);
		bool flag2 = (current != null && ((ButtonControl)current.downArrowKey).wasPressedThisFrame) || (current2 != null && current2.dpad.down.wasPressedThisFrame);
		bool flag3 = (current != null && (((ButtonControl)current.enterKey).wasPressedThisFrame || ((ButtonControl)current.numpadEnterKey).wasPressedThisFrame)) || (current2 != null && current2.buttonSouth.wasPressedThisFrame);
		bool num = (current != null && ((ButtonControl)current.escapeKey).wasPressedThisFrame) || (current2 != null && current2.buttonEast.wasPressedThisFrame);
		bool flag4 = IsConfirmHeld();
		if (_suppressConfirmUntilReleased && !flag4)
		{
			_suppressConfirmUntilReleased = false;
		}
		if (num)
		{
			CloseSlotSelector();
			return;
		}
		int unlockedSlotCount = RuneSlotService.GetUnlockedSlotCount(selectorPlayer);
		if (flag != flag2)
		{
			int num2 = (flag2 ? 1 : (-1));
			_selectorIndex = (_selectorIndex + num2 + unlockedSlotCount) % unlockedSlotCount;
			DrawSlotSelector();
		}
		if (flag3 && !_suppressConfirmUntilReleased)
		{
			string selectorContext = GetSelectorContext();
			RuneCycleResult result = RuneSlotService.SelectSlot(selectorPlayer, _selectorIndex);
			RuneSlotUi.RefreshAfterSelection(selectorPlayer, selectorContext);
			_message = RuneSlotService.FormatStatus(selectorPlayer, selectorContext, result);
			_messageUntil = Time.unscaledTime + 4f;
			Plugin.ModLog.LogInfo((object)_message);
			if (_selectorSurface == SlotSelectionSurface.Shop)
			{
				_showShopSlots = true;
				_shopSlots = RuneSlotUi.FormatShopOverlay(selectorPlayer);
			}
			CloseSlotSelector();
		}
	}

	private void DrawSlotSelector()
	{
		PLAYER_BASE selectorPlayer = _selectorPlayer;
		if (selectorPlayer != null)
		{
			_shopSlots = RuneSlotUi.FormatShopOverlay(selectorPlayer, _selectorIndex, selecting: true);
			if (_selectorSurface == SlotSelectionSurface.Shop)
			{
				_showShopSlots = true;
			}
			else if (_selectorSurface == SlotSelectionSurface.Battle)
			{
				RuneSlotUi.DrawBattle(selectorPlayer, _selectorIndex, selecting: true);
			}
			else if (_selectorSurface == SlotSelectionSurface.MainMenu)
			{
				RuneSlotUi.DrawMainMenu(selectorPlayer, explain: false, _selectorIndex, selecting: true);
			}
		}
	}

	private void CloseSlotSelector()
	{
		if (_selectorSurface == SlotSelectionSurface.Shop && _selectorPlayer != null)
		{
			_showShopSlots = true;
			_shopSlots = RuneSlotUi.FormatShopOverlay(_selectorPlayer);
		}
		else if (_selectorSurface == SlotSelectionSurface.Battle && _selectorPlayer != null)
		{
			RuneSlotUi.DrawBattle(_selectorPlayer);
			_shopSlots = string.Empty;
		}
		else if (_selectorSurface == SlotSelectionSurface.MainMenu && _selectorPlayer != null)
		{
			RuneSlotUi.DrawMainMenu(_selectorPlayer, explain: false);
			_shopSlots = string.Empty;
		}
		_selectorSurface = SlotSelectionSurface.None;
		_selectorPlayer = null;
		_suppressConfirmUntilReleased = false;
		_resumePending = true;
	}

	private void UpdatePendingResume()
	{
		if (!IsSelectorInputHeld())
		{
			GRInputManager.Pause = _pauseBeforeSelector;
			_resumePending = false;
		}
	}

	private static bool IsConfirmHeld()
	{
		Keyboard current = Keyboard.current;
		Gamepad current2 = Gamepad.current;
		if (current == null || (!((ButtonControl)current.enterKey).isPressed && !((ButtonControl)current.numpadEnterKey).isPressed))
		{
			if (current2 != null)
			{
				return current2.buttonSouth.isPressed;
			}
			return false;
		}
		return true;
	}

	private static bool IsSelectorInputHeld()
	{
		Keyboard current = Keyboard.current;
		Gamepad current2 = Gamepad.current;
		bool num = current != null && (((ButtonControl)current.upArrowKey).isPressed || ((ButtonControl)current.downArrowKey).isPressed || ((ButtonControl)current.enterKey).isPressed || ((ButtonControl)current.numpadEnterKey).isPressed || ((ButtonControl)current.escapeKey).isPressed);
		bool flag = current2 != null && (current2.dpad.up.isPressed || current2.dpad.down.isPressed || current2.buttonSouth.isPressed || current2.buttonEast.isPressed);
		return num || flag;
	}

	private string GetSelectorContext()
	{
		return _selectorSurface switch
		{
			SlotSelectionSurface.Shop => "Boutique de runes", 
			SlotSelectionSurface.Battle => "Combat", 
			SlotSelectionSurface.MainMenu => "Menu", 
			_ => "Runes", 
		};
	}

	private void ForceRestoreNativeInput()
	{
		if (_selectorSurface != 0 || _resumePending)
		{
			GRInputManager.Pause = _pauseBeforeSelector;
		}
		_selectorSurface = SlotSelectionSurface.None;
		_selectorPlayer = null;
		_resumePending = false;
		if (_shopCompatibilityPlayer != null)
		{
			RuneSlotService.ExitRuneShopCompatibility(_shopCompatibilityPlayer);
			_shopCompatibilityPlayer = null;
			_shopCompatibilityCharaNo = int.MinValue;
		}
	}

	private void OnGUI()
	{
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		if (_guiFaulted)
		{
			return;
		}
		try
		{
			bool num = _selectorSurface != SlotSelectionSurface.None;
			bool flag = Plugin.ShowShopSlots.Value && _showShopSlots;
			if ((num || flag) && !string.IsNullOrEmpty(_shopSlots))
			{
				DrawShopPanel();
			}
			if (Plugin.ShowOverlay.Value && !(Time.unscaledTime > _messageUntil) && !string.IsNullOrEmpty(_message))
			{
				float num2 = Math.Min(760f, (float)Screen.width - 40f);
				GUI.Box(new Rect(((float)Screen.width - num2) / 2f, 24f, num2, 72f), _message);
			}
		}
		catch (Exception ex)
		{
			_guiFaulted = true;
			_showShopSlots = false;
			Plugin.ModLog.LogWarning((object)("Surimpression des runes desactivee pour cette session apres une erreur: " + ex.Message));
		}
	}

	private void DrawShopPanel()
	{
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_0323: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0280: Unknown result type (might be due to invalid IL or missing references)
		//IL_0299: Unknown result type (might be due to invalid IL or missing references)
		string[] array = _shopSlots.Split('\n');
		if (array.Length < 5)
		{
			return;
		}
		float num = Math.Clamp(Math.Min((float)Screen.width / 1280f, (float)Screen.height / 800f), 0.75f, 2f);
		float num2 = 300f * num;
		float num3 = 177f * num;
		float num4 = Math.Max(10f * num, (float)Screen.width - num2 - 20f * num);
		float num5 = 174f * num;
		float num6 = 2f * num;
		DrawSolidRect(new Rect(num4, num5, num2, num3), new Color(0.94f, 0.94f, 0.91f, 1f));
		DrawSolidRect(new Rect(num4 + num6, num5 + num6, num2 - num6 * 2f, num3 - num6 * 2f), new Color(0.015f, 0.015f, 0.015f, 0.96f));
		float num7 = 6f * num;
		float num8 = 1f * num;
		DrawSolidRect(new Rect(num4 + num7, num5 + num7, num2 - num7 * 2f, num3 - num7 * 2f), new Color(0.52f, 0.52f, 0.5f, 1f));
		DrawSolidRect(new Rect(num4 + num7 + num8, num5 + num7 + num8, num2 - (num7 + num8) * 2f, num3 - (num7 + num8) * 2f), new Color(0.015f, 0.015f, 0.015f, 1f));
		EnsureShopStyles(num);
		GUI.Box(new Rect(num4 + 67f * num, num5 + 10f * num, num2 - 82f * num, 30f * num), array[0], _shopHeaderStyle);
		DrawSolidRect(new Rect(num4 + 11f * num, num5 + 43f * num, num2 - 22f * num, 1f * num), new Color(0.78f, 0.78f, 0.75f, 1f));
		float num9 = num5 + 49f * num;
		float num10 = 30f * num;
		for (int i = 0; i < 3; i++)
		{
			string text = array[i + 1];
			bool flag = text.StartsWith(">", StringComparison.Ordinal);
			bool flag2 = text.Contains("Bloqué", StringComparison.Ordinal);
			float num11 = num9 + (float)i * num10;
			if (flag)
			{
				DrawSolidRect(new Rect(num4 + 10f * num, num11, num2 - 20f * num, 28f * num), new Color(0.38f, 0.31f, 0.015f, 1f));
			}
			GUI.Box(new Rect(num4 + 19f * num, num11 + 2f * num, num2 - 38f * num, 26f * num), text, flag ? _shopActiveStyle : (flag2 ? _shopLockedStyle : _shopRowStyle));
		}
		GUI.Box(new Rect(num4 + 19f * num, num5 + 141f * num, num2 - 38f * num, 24f * num), array[4], _shopHintStyle);
	}

	private void EnsureShopStyles(float scale)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		if (_shopHeaderStyle == null)
		{
			_shopHeaderStyle = CreateShopStyle(Color.white);
		}
		if (_shopRowStyle == null)
		{
			_shopRowStyle = CreateShopStyle(new Color(0.95f, 0.95f, 0.92f, 1f));
		}
		if (_shopActiveStyle == null)
		{
			_shopActiveStyle = CreateShopStyle(Color.white);
		}
		if (_shopLockedStyle == null)
		{
			_shopLockedStyle = CreateShopStyle(new Color(0.68f, 0.68f, 0.66f, 1f));
		}
		if (_shopHintStyle == null)
		{
			_shopHintStyle = CreateShopStyle(new Color(0.82f, 0.82f, 0.78f, 1f));
		}
		_shopHeaderStyle.fontSize = Math.Max(14, (int)Math.Round(18f * scale));
		int fontSize = Math.Max(15, (int)Math.Round(20f * scale));
		_shopRowStyle.fontSize = fontSize;
		_shopActiveStyle.fontSize = fontSize;
		_shopLockedStyle.fontSize = fontSize;
		_shopHintStyle.fontSize = Math.Max(11, (int)Math.Round(14f * scale));
	}

	private static GUIStyle CreateShopStyle(Color color)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		GUIStyle val = new GUIStyle();
		val.normal.textColor = color;
		return val;
	}

	private static void DrawSolidRect(Rect rect, Color color)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Color color2 = GUI.color;
		GUI.color = color;
		GUI.DrawTexture(rect, (Texture)(object)Texture2D.whiteTexture);
		GUI.color = color2;
	}

	private void OnApplicationQuit()
	{
		ForceRestoreNativeInput();
		RuneSlotService.NormalizeCurrentGame();
	}
}
