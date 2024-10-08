//
//
//  xCSCarbon
//
//  Created by Erik Ylvisaker on 3/17/08.
//  Copyright 2008 __MyCompanyName__. All rights reserved.
//
//

using System;

namespace OpenTK.Platform.MacOS {
	static class Application {
		static bool mInitialized;
		static IntPtr uppHandler;
		static MacOSEventHandler handler = EventHandler;
		internal static CarbonWindow WindowEventHandler;

		static Application() { Initialize(); }

		internal static void Initialize() {
			if (mInitialized) return;	
			API.AcquireRootMenu();
			ConnectEvents();
			int osMajor, osMinor, osBugfix;
			
			API.Gestalt(GestaltSelector.SystemVersionMajor, out osMajor);
			API.Gestalt(GestaltSelector.SystemVersionMinor, out osMinor);
			API.Gestalt(GestaltSelector.SystemVersionBugFix, out osBugfix);
			Debug.Print("Running on Mac OS X {0}.{1}.{2}.", osMajor, osMinor, osBugfix);

			TransformProcessToForeground();
		}

		static void TransformProcessToForeground() {
			ProcessSerialNumber psn = new ProcessSerialNumber();
			Debug.Print("Setting process to be foreground application.");

			API.GetCurrentProcess(ref psn);
			API.TransformProcessType(ref psn, ProcessApplicationTransformState.kProcessTransformToForegroundApplication);
			API.SetFrontProcess(ref psn);
		}

		static void ConnectEvents() {
			EventTypeSpec[] eventTypes = new EventTypeSpec[] {
				new EventTypeSpec(EventClass.Application, AppEventKind.AppActivated),
				new EventTypeSpec(EventClass.Application, AppEventKind.AppDeactivated),
				new EventTypeSpec(EventClass.Application, AppEventKind.AppQuit),

				new EventTypeSpec(EventClass.Mouse, MouseEventKind.MouseDown),
				new EventTypeSpec(EventClass.Mouse, MouseEventKind.MouseUp),
				new EventTypeSpec(EventClass.Mouse, MouseEventKind.MouseMoved),
				new EventTypeSpec(EventClass.Mouse, MouseEventKind.MouseDragged),
				new EventTypeSpec(EventClass.Mouse, MouseEventKind.MouseEntered),
				new EventTypeSpec(EventClass.Mouse, MouseEventKind.MouseExited),
				new EventTypeSpec(EventClass.Mouse, MouseEventKind.WheelMoved),
				
				new EventTypeSpec(EventClass.Keyboard, KeyboardEventKind.RawKeyDown),
				new EventTypeSpec(EventClass.Keyboard, KeyboardEventKind.RawKeyRepeat),
				new EventTypeSpec(EventClass.Keyboard, KeyboardEventKind.RawKeyUp),
				new EventTypeSpec(EventClass.Keyboard, KeyboardEventKind.RawKeyModifiersChanged),

				new EventTypeSpec(EventClass.AppleEvent, AppleEventKind.AppleEvent),
			};

			uppHandler = API.NewEventHandlerUPP(handler);
			API.InstallApplicationEventHandler(
				uppHandler, eventTypes, IntPtr.Zero, IntPtr.Zero);		
			mInitialized = true;
		}

		static OSStatus EventHandler(IntPtr inCaller, IntPtr inEvent, IntPtr userData) {
			EventInfo evt = new EventInfo(inEvent);		
			switch (evt.EventClass) {
				case EventClass.AppleEvent:
					// only event here is the apple event.
					Debug.Print("Processing apple event.");
					API.ProcessAppleEvent(inEvent);
					break;

				case EventClass.Keyboard:
				case EventClass.Mouse:
					if (WindowEventHandler != null) {
						return WindowEventHandler.DispatchEvent(inCaller, inEvent, evt, userData);
					}
					break;
			}
			return OSStatus.EventNotHandled;
		}
	}
}