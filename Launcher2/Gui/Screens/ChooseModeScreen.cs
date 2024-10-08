﻿// ClassicalSharp copyright 2014-2016 UnknownShadow200 | Licensed under MIT
using System;
using ClassicalSharp;
using Launcher.Gui.Views;
using Launcher.Gui.Widgets;
using OpenTK.Input;

namespace Launcher.Gui.Screens {	
	public sealed class ChooseModeScreen : Screen {
		
		ChooseModeView view;
		public ChooseModeScreen(LauncherWindow game, bool firstTime) : base(game) {
			view = new ChooseModeView(game);
			view.FirstTime = firstTime;
			widgets = view.widgets;
		}

		public override void Init() {
			base.Init();
			view.Init();
			
			widgets[view.nIndex].OnClick = UseModeEnhanced;
			widgets[view.clIndex].OnClick = UseModeClassicHax;
			widgets[view.clHaxIndex].OnClick = UseModeClassic;		
			if (view.backIndex >= 0) {
				widgets[view.backIndex].OnClick = SwitchToSettings;
			}
			Resize();
		}
		
		void UseModeEnhanced(int x, int y) { ModeClick(false, false); }
		void UseModeClassicHax(int x, int y) { ModeClick(true, false); }
		void UseModeClassic(int x, int y) { ModeClick(true, true); }
		void SwitchToSettings(int x, int y) { game.SetScreen(new SettingsScreen(game)); }
		
		public override void Tick() { }

		public override void Resize() {
			view.DrawAll();
			game.Dirty = true;
		}
		
		void ModeClick(bool classic, bool classicHacks) {
			game.ClassicBackground = classic;
			Options.Load();
			Options.Set(OptionsKey.ClassicMode, classic);
			if (classic)
				Options.Set(OptionsKey.ClassicHacks, classicHacks);
			
			Options.Set("nostalgia-classicbg", classic);
			Options.Set(OptionsKey.CustomBlocks,   !classic);
			Options.Set(OptionsKey.CPE,            !classic);
			Options.Set(OptionsKey.ServerTextures, !classic);
			Options.Set(OptionsKey.ClassicTabList,  classic);
			Options.Set(OptionsKey.ClassicOptions,  classic);
			Options.Save();
			
			game.SetScreen(new MainScreen(game));
		}

		public override void Dispose() {
			base.Dispose();
			view.Dispose();
		}
	}
}
