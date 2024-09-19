// ClassicalSharp copyright 2014-2016 UnknownShadow200 | Licensed under MIT
using System;
using System.Drawing;
using ClassicalSharp;
using Launcher.Gui.Widgets;

namespace Launcher.Gui.Views {
	public sealed class SettingsView : IView {
		internal int backIndex, modeIndex;
		internal int coloursIndex;
		
		public SettingsView(LauncherWindow game) : base(game) {
			widgets = new Widget[5];
		}

		public override void Init() {
			titleFont = new Font(game.FontName, 16, FontStyle.Bold);
			textFont = new Font(game.FontName, 14, FontStyle.Regular);
			MakeWidgets();
		}
		
		protected override void MakeWidgets() {
			widgetIndex = 0;

            modeIndex = widgetIndex;
            Makers.Button(this, "Mode", 110, 35, titleFont)
                .SetLocation(Anchor.Centre, Anchor.Centre, -135, -120);
            Makers.Label(this, "&eChange the enabled features", textFont)
                .SetLocation(Anchor.Centre, Anchor.Centre, 55, -120);

            coloursIndex = widgetIndex;
			Makers.Button(this, "Colours", 110, 35, titleFont)
				.SetLocation(Anchor.Centre, Anchor.Centre, -135, -70);
			Makers.Label(this, "&eChange how the launcher looks", textFont)
				.SetLocation(Anchor.Centre, Anchor.Centre, 65, -70);
			
			if (game.ClassicBackground) {
				widgets[coloursIndex].Visible = false;
				widgets[coloursIndex + 1].Visible = false;
			}
			
			backIndex = widgetIndex;
			Makers.Button(this, "Back", 80, 35, titleFont)
				.SetLocation(Anchor.Centre, Anchor.Centre, 0, 170);
		}
	}
}