// ClassicalSharp copyright 2014-2016 UnknownShadow200 | Licensed under MIT
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using ClassicalSharp;
using Launcher.Gui.Views;
using Launcher.Gui.Widgets;

namespace Launcher.Gui.Screens {	
	public sealed class DirectConnectScreen : InputScreen {

        public DirectConnectView view;
		public DirectConnectScreen(LauncherWindow game) : base(game) {
			enterIndex = 3;
			view = new DirectConnectView(game);
			widgets = view.widgets;
		}

		public override void Init() {
			base.Init();
			view.Init();
			SetWidgetHandlers();
			Resize();
		}
		
		public override void Resize() {
			view.DrawAll();
			game.Dirty = true;
		}

        public void SetWidgetHandlers() {
			widgets[view.backIndex].OnClick = SwitchToMain;
			widgets[view.connectIndex].OnClick = StartClient;
			widgets[view.ccSkinsIndex].OnClick = UseClassicubeSkinsClick;			
			SetupInputHandlers();
			LoadSavedInfo();
		}

        public void SwitchToMain(int x, int y) { game.SetScreen(new MainScreen(game)); }

        public void SetStatus(string text) {
			LabelWidget widget = (LabelWidget)widgets[view.statusIndex];
			game.ResetArea(widget.X, widget.Y, widget.Width, widget.Height);
			widget.SetDrawData(drawer, text);
			RedrawWidget(widget);
		}

        public void UseClassicubeSkinsClick(int mouseX, int mouseY) {
			CheckboxWidget widget = (CheckboxWidget)widgets[view.ccSkinsIndex];
			widget.Value = !widget.Value;
			RedrawWidget(widget);
		}
		
		public override void Dispose() {
			StoreFields();
			base.Dispose();
		}

        public static string cachedUser, cachedAddress, cachedMppass;
        public static bool cachedSkins;

        public void LoadSavedInfo() {
			// restore what user last typed into the various fields
			if (cachedUser != null) {
				SetText(0, cachedUser);
				SetText(1, cachedAddress);
				SetText(2, cachedMppass);
				SetBool(cachedSkins);
			} else {
				LoadFromOptions();
			}
		}

        public void StoreFields() {
			cachedUser = Get(0);
			cachedAddress = Get(1);
			cachedMppass = Get(2);
			cachedSkins = ((CheckboxWidget)widgets[view.ccSkinsIndex]).Value;
		}

        public void LoadFromOptions() {
			if (!Options.Load()) return;
			
			string user = Options.Get("launcher-dc-username", "");
			string ip = Options.Get("launcher-dc-ip", "127.0.0.1");
			string port = Options.Get("launcher-dc-port", "25565");

			IPAddress address;
			if (!IPAddress.TryParse(ip, out address)) ip = "127.0.0.1";
			ushort portNum;
			if (!UInt16.TryParse(port, out portNum)) port = "25565";
			
			string mppass = Options.Get("launcher-dc-mppass", null);
			mppass = Secure.Decode(mppass, user);
			
			SetText(0, user);
			SetText(1, ip + ":" + port);
			SetText(2, mppass);
		}
		
		void SaveToOptions(ClientStartData data) {
			if (!Options.Load())
				return;
			
			Options.Set("launcher-dc-username", data.Username);
			Options.Set("launcher-dc-ip", data.Ip);
			Options.Set("launcher-dc-port", data.Port);
			Options.Set("launcher-dc-mppass", Secure.Encode(data.Mppass, data.Username));
			Options.Save();
		}

        public void SetText(int index, string text) {
			((InputWidget)widgets[index]).SetDrawData(drawer, text);
		}

        public void SetBool(bool value) {
			((CheckboxWidget)widgets[view.ccSkinsIndex]).Value = value;
		}

        public void StartClient(int mouseX, int mouseY) {
			string address = Get(1);
			int index = address.LastIndexOf(':');
			if (index <= 0 || index == address.Length - 1) {
				SetStatus("&eInvalid address"); return;
			}
			
			string ipPart = address.Substring(0, index);
			string portPart = address.Substring(index + 1, address.Length - index - 1);			
			ClientStartData data = GetStartData(Get(0), Get(2), ipPart, portPart);
			if (data == null) return;
			
			SaveToOptions(data);
			Client.Start(data, ref game.ShouldExit);
		}

        public static Random rnd = new Random();
        public static byte[] rndBytes = new byte[8];
        public ClientStartData GetStartData(string user, string mppass, string ip, string port) {
			SetStatus("");
			
			if (string.IsNullOrEmpty(user)) {
				SetStatus("&eUsername required"); return null;
			}
			
			IPAddress realIp;
			if (!IPAddress.TryParse(ip, out realIp) && ip != "localhost") {
				SetStatus("&eInvalid ip"); return null;
			}
			if (ip == "localhost") ip = "127.0.0.1";
			
			ushort realPort;
			if (!ushort.TryParse(port, out realPort)) {
				SetStatus("&eInvalid port"); return null;
			}
			
			if (string.IsNullOrEmpty(mppass))
				mppass = "(none)";
			
			ClientStartData data = new ClientStartData(user, mppass, ip, port, "");
			if (Utils.CaselessEquals(user, "rand()") || Utils.CaselessEquals(user, "random()")) {
				rnd.NextBytes(rndBytes);
				data.Username = Convert.ToBase64String(rndBytes).TrimEnd('=');
			}
			return data;
		}
	}
}
