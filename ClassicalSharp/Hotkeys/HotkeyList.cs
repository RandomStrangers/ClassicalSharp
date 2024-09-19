﻿// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
#if !ANDROID
using System;
using System.Collections.Generic;
using OpenTK.Input;

namespace ClassicalSharp.Hotkeys {

	/// <summary> Maintains the list of hotkeys defined by the client and by SetTextHotkey packets. </summary>
	public static class HotkeyList {
		
		public static List<Hotkey> Hotkeys = new List<Hotkey>();
		
		public static void Add(Key trigger, byte flags, string text, bool more) {
			if (!UpdateExistingHotkey(trigger, flags, text, more))
				AddNewHotkey(trigger, flags, text, more);
		}
		
		public static bool Remove(Key trigger, byte flags) {
			for (int i = 0; i < Hotkeys.Count; i++) {
				Hotkey hKey = Hotkeys[i];
				if (hKey.Trigger == trigger && hKey.Flags == flags) {
					Hotkeys.RemoveAt(i);
					return true;
				}
			}
			return false;
		}
		
		static bool UpdateExistingHotkey(Key trigger, byte flags, string text, bool more) {
			for (int i = 0; i < Hotkeys.Count; i++) {
				Hotkey hKey = Hotkeys[i];
				if (hKey.Trigger == trigger && hKey.Flags == flags) {
					hKey.Text = text;
					hKey.StaysOpen = more;
					Hotkeys[i] = hKey;
					return true;
				}
			}
			return false;
		}
		
		static void AddNewHotkey(Key trigger, byte flags, string text, bool more) {
			Hotkey hotkey;
			hotkey.Trigger = trigger;
			hotkey.Flags = flags;
			hotkey.Text = text;
			hotkey.StaysOpen = more;
			
			Hotkeys.Add(hotkey);
			// sort so that hotkeys with largest modifiers are first
			Hotkeys.Sort(compareHotkeys);
		}
		static int CompareHotkeys(Hotkey a, Hotkey b) { return b.Flags.CompareTo(a.Flags); }
		static Comparison<Hotkey> compareHotkeys = CompareHotkeys;
		
		/// <summary> Determines whether a hotkey is active based on the given key,
		/// and the currently active control, alt, and shift modifiers </summary>
		public static int FindPartial(Key key, InputHandler input) {
			byte flags = 0;
			if (input.ControlDown) flags |= 1;
			if (input.ShiftDown)   flags |= 2;
			if (input.AltDown)     flags |= 4;
			
			for (int i = 0; i < Hotkeys.Count; i++) {
				Hotkey hKey = Hotkeys[i];
				// e.g. If holding Ctrl and Shift, a hotkey with only Ctrl flags matches
				if ((hKey.Flags & flags) == hKey.Flags && hKey.Trigger == key) return i;
			}			
			return -1;
		}
		
		const string prefix = "hotkey-";
		public static void LoadSavedHotkeys() {
			for (int i = 0; i < Options.OptionsKeys.Count; i++) {
				string key = Options.OptionsKeys[i];
				if (!Utils.CaselessStarts(key, prefix)) continue;			
				int keySplit = key.IndexOf('&', prefix.Length);
				if (keySplit < 0) continue; // invalid key
				
				string strKey   = key.Substring(prefix.Length, keySplit - prefix.Length);
				string strFlags = key.Substring(keySplit + 1, key.Length - keySplit - 1);
				
				string value = Options.OptionsValues[i];
				int valueSplit = value.IndexOf('&', 0);
				if (valueSplit < 0) continue; // invalid value
				
				string strMoreInput = value.Substring(0, valueSplit - 0);
				string strText      = value.Substring(valueSplit + 1, value.Length - valueSplit - 1);
				
				// Then try to parse the key and value
				Key hotkey; byte flags; bool moreInput;
				if (!Utils.TryParseEnum(strKey, Key.None, out hotkey) ||
				   !Byte.TryParse(strFlags, out flags) ||
				   !Boolean.TryParse(strMoreInput, out moreInput) ||
				   strText.Length == 0) {
					Utils.LogDebug("Hotkey {0} has invalid arguments", key);
					continue;
				}
				Add(hotkey, flags, strText, moreInput);
			}
		}
		
		public static void UserRemovedHotkey(Key trigger, byte flags) {
			string key = "hotkey-" + trigger + "&" + flags;
			Options.Set(key, null);
		}
		
		public static void UserAddedHotkey(Key trigger, byte flags, bool moreInput, string text) {
			string key = "hotkey-" + trigger + "&" + flags;
			string value = moreInput + "&" + text;
			Options.Set(key, value);
		}
	}
	
	public struct Hotkey {
		public Key Trigger;
		public byte Flags; // ctrl 1, shift 2, alt 4
		public bool StaysOpen; // whether the user is able to enter further input
		public string Text; // contents to copy directly into the input bar
	}
}
#endif
