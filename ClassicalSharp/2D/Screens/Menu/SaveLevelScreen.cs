﻿// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using System.Drawing;
using System.IO;
using ClassicalSharp.Gui.Widgets;
using ClassicalSharp.Map;
using OpenTK.Input;

namespace ClassicalSharp.Gui.Screens {
	public class SaveLevelScreen : MenuScreen {
		
		public SaveLevelScreen(Game game) : base(game) {
		}
		
		InputWidget input;
		const int overwriteIndex = 2;
		static PackedCol grey = new PackedCol(150, 150, 150);
		
		void SaveMap(string path) {
			bool cw = path.EndsWith(".cw");
			try {
				using (Stream fs = Platform.FileCreate(path)) {
					IMapFormatExporter exporter = null;
					if (cw) exporter = new MapCwExporter();
					else exporter = new MapSchematicExporter();
					exporter.Save(fs, game);
				}
			} catch (Exception ex) {
				ErrorHandler.LogError("saving map", ex);
				MakeDescWidget("&cError while trying to save map");
				return;
			}
			
			game.Chat.Add("&eSaved map to: " + path);
			game.Gui.SetNewScreen(new PauseScreen(game));
		}
		
		public override void Render(double delta) {
			base.Render(delta);			
			int cX = game.Width / 2, cY = game.Height / 2;
			game.Graphics.Draw2DQuad(cX - 250, cY + 90, 500, 2, grey);
		}
		
		public override bool HandlesKeyPress(char key) {
			RemoveOverwrites();
			return input.HandlesKeyPress(key);
		}
		
		public override bool HandlesKeyDown(Key key) {
			RemoveOverwrites();
			if (input.HandlesKeyDown(key)) return true;
			return base.HandlesKeyDown(key);
		}
		
		public override bool HandlesKeyUp(Key key) {
			return input.HandlesKeyUp(key);
		}
		
		public override void Init() {
			base.Init();
			Keyboard.KeyRepeat = true;
			ContextRecreated();
		}
		
		protected override void ContextRecreated() {
			input = MenuInputWidget.Create(game, 500, 30, "", textFont, new PathValidator())
				.SetLocation(Anchor.Centre, Anchor.Centre, 0, -30);
			input.ShowCaret = true;
			
			widgets = new Widget[] {
				ButtonWidget.Create(game, 300, "Save", titleFont, SaveClassic)
					.SetLocation(Anchor.Centre, Anchor.Centre, 0, 20),
				ButtonWidget.Create(game, 200, "Save schematic", titleFont, SaveSchematic)
					.SetLocation(Anchor.Centre, Anchor.Centre, -150, 120),
				TextWidget.Create(game, "&eCan be imported into MCEdit", textFont)
					.SetLocation(Anchor.Centre, Anchor.Centre, 110, 120),				
				MakeBack("Cancel", titleFont, SwitchPause),
				input,
				null, // description widget placeholder				
			};
		}
		
		public override void Dispose() {
			Keyboard.KeyRepeat = false;
			base.Dispose();
		}
		
		void SaveClassic(Game game, Widget widget) { DoSave(widget, ".cw"); }	
		void SaveSchematic(Game game, Widget widget) { DoSave(widget, ".schematic"); }
		
		void DoSave(Widget widget, string ext) {
			string file = input.Text.ToString();
			if (file.Length == 0) {
				MakeDescWidget("&ePlease enter a filename"); return;
			}
			
			string path = Path.Combine("maps", file + ext);
			ButtonWidget btn = (ButtonWidget)widget;
			
			if (Platform.FileExists(path) && btn.OptName == null) {
				btn.Set("&cOverwrite existing?", titleFont);
				btn.OptName = "O";
			} else {
				RemoveOverwrites();
				SaveMap(path);
			}
		}
		
		void RemoveOverwrites() {
			RemoveOverwrite(widgets[0], "Save"); 
			RemoveOverwrite(widgets[1], "Save schematic");
		}
		
		void RemoveOverwrite(Widget widget, string defaultText) {
			ButtonWidget button = (ButtonWidget)widget;
			if (button.OptName == null) return;
			
			button.OptName = null;
			button.Set(defaultText, titleFont);
		}
		
		void MakeDescWidget(string text) {
			if (widgets[widgets.Length - 1] != null) {
				widgets[widgets.Length - 1].Dispose();
			}
			
			widgets[widgets.Length - 1] = TextWidget.Create(game, text, textFont)
				.SetLocation(Anchor.Centre, Anchor.Centre, 0, 65);
		}
	}
}