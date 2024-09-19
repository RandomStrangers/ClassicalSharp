﻿// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
#if !ANDROID
using System;
using System.Drawing;
using ClassicalSharp.Gui.Widgets;
using ClassicalSharp.Hotkeys;
using OpenTK.Input;

namespace ClassicalSharp.Gui.Screens {
	// TODO: Hotkey added event for CPE
	public class HotkeyListScreen : ListScreen {
		
		public HotkeyListScreen(Game game) : base(game) {
			titleText = "Modify hotkeys";
			int count = HotkeyList.Hotkeys.Count;
			entries = new string[count + items];
			
			for (int i = 0; i < count; i++) {
				Hotkey hKey = HotkeyList.Hotkeys[i];
				string entry = hKey.Trigger.ToString();
				
				if (hKey.Flags != 0) {
					entry += " +" + MakeFlagsString(hKey.Flags);
				}
				entries[i] = entry;
			}
			for (int i = 0; i < items; i++)
				entries[count + i] = empty;
		}
		
		internal static string MakeFlagsString(byte flags) {
			return
				((flags & 1) == 0 ? "" : " Ctrl")  +
				((flags & 2) == 0 ? "" : " Shift") +
				((flags & 4) == 0 ? "" : " Alt");
		}
		
		protected override void EntryClick(Game game, Widget widget) {
			string text = GetCur(widget);
			Hotkey original = default(Hotkey);
			if (text != empty) original = Parse(text);
			game.Gui.SetNewScreen(new EditHotkeyScreen(game, original));
		}
		
		Hotkey Parse(string text) {
			int sepIndex = text.IndexOf('+');
			string key = text, value;
			byte flags = 0;
			
			if (sepIndex >= 0) {
				key   = text.Substring(0, sepIndex - 1);
				value = text.Substring(sepIndex + 1);
				
				if (value.Contains("Ctrl"))  flags |= 1;
				if (value.Contains("Shift")) flags |= 2;
				if (value.Contains("Alt"))   flags |= 4;
			}
			
			Key baseKey = (Key)Enum.Parse(typeof(Key), key);
			for (int i = 0; i < HotkeyList.Hotkeys.Count; i++) {
				Hotkey h = HotkeyList.Hotkeys[i];
				if (h.Trigger == baseKey && h.Flags == flags) return h;
			}
			return default(Hotkey);
		}
	}
}
#endif