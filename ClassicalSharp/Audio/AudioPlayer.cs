﻿// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using System.IO;
using System.Threading;
using SharpWave;

namespace ClassicalSharp.Audio {
	
	public sealed partial class AudioPlayer : IGameComponent {
		
		IAudioOutput musicOut;
		IAudioOutput[] monoOutputs, stereoOutputs;
		string[] files, musicFiles;
		Thread musicThread;
		Game game;
		
		void IGameComponent.Init(Game game) {
			this.game = game;
			if (Platform.DirectoryExists("audio")) {
				files = Platform.DirectoryFiles("audio");
			} else {
				files = new string[0];
			}
			
			game.MusicVolume = GetVolume(OptionsKey.MusicVolume, OptionsKey.UseMusic);
			SetMusic(game.MusicVolume);
			game.SoundsVolume = GetVolume(OptionsKey.SoundsVolume, OptionsKey.UseSound);
			SetSounds(game.SoundsVolume);
			Events.BlockChanged += PlayBlockSound;
		}
		
		static int GetVolume(string volKey, string boolKey) {
			int volume = Options.GetInt(volKey, 0, 100, 0);
			if (volume != 0) return volume;
			
			volume = Options.GetBool(boolKey, false) ? 100 : 0;
			Options.Set(boolKey, null);
			return volume;
		}

		void IGameComponent.Ready(Game game) { }
		void IGameComponent.Reset(Game game) { }
		void IGameComponent.OnNewMap(Game game) { }
		void IGameComponent.OnNewMapLoaded(Game game) { }
		
		public void SetMusic(int volume) {
			if (volume > 0) InitMusic();
			else DisposeMusic();
		}
		
		void InitMusic() {
			if (musicThread != null) { musicOut.SetVolume(game.MusicVolume / 100.0f); return; }
			
			int musicCount = 0;
			for (int i = 0; i < files.Length; i++) {
				if (Utils.CaselessEnds(files[i], ".ogg")) musicCount++;
			}
			
			musicFiles = new string[musicCount];
			for (int i = 0, j = 0; i < files.Length; i++) {
				if (!Utils.CaselessEnds(files[i], ".ogg")) continue;
				musicFiles[j] = files[i]; j++;
			}

			disposingMusic = false;
			musicOut = MakeOutput(4);
			musicThread = MakeThread(DoMusicThread, "ClassicalSharp.DoMusic");
		}
		
		EventWaitHandle musicHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
		void DoMusicThread() {
			if (musicFiles.Length == 0) return;
			Random rnd = new Random();
			while (!disposingMusic) {
				string file = musicFiles[rnd.Next(0, musicFiles.Length)];
				Utils.LogDebug("playing music file: " + file);
				
				string path = Path.Combine("audio", file);
				using (Stream fs = Platform.FileOpen(path)) {
					try {
						musicOut.SetVolume(game.MusicVolume / 100.0f);
						musicOut.PlayStreaming(fs);
					} catch (InvalidOperationException ex) {
						HandleMusicError(ex);
						try { musicOut.Dispose(); } catch { }
						return;
					} catch (Exception ex) {
						ErrorHandler.LogError("AudioPlayer.DoMusicThread()", ex);
						game.Chat.Add("&cError while trying to play music file " + file);
						try { musicOut.Dispose(); } catch { }
						return;
					}
				}
				if (disposingMusic) break;
				
				int delay = 1000 * 120 + rnd.Next(0, 1000 * 300);
				musicHandle.WaitOne(delay, false);
			}
		}
		
		void HandleMusicError(InvalidOperationException ex) {
			ErrorHandler.LogError("AudioPlayer.DoMusicThread()", ex);
			if (ex.Message == "No audio devices found")
				game.Chat.Add("&cNo audio devices found, disabling music.");
			else
				game.Chat.Add("&cAn error occured when trying to play music, disabling music.");
			
			SetMusic(0);
			game.MusicVolume = 0;
		}
		
		bool disposingMusic;
		void IDisposable.Dispose() {
			DisposeMusic();
			DisposeSound();
			musicHandle.Close();
			Events.BlockChanged -= PlayBlockSound;
		}
		
		void DisposeMusic() {
			disposingMusic = true;
			musicHandle.Set();
			
			DisposeOf(ref musicOut, ref musicThread);
		}
		
		Thread MakeThread(ThreadStart func, string name) {
			Thread thread = new Thread(func);
			thread.Name = name;
			thread.IsBackground = true;
			thread.Start();
			return thread;
		}
		
		IAudioOutput MakeOutput(int buffers) {
			IAudioOutput output;
			if (OpenTK.Configuration.RunningOnWindows && !Options.GetBool(OptionsKey.ForceOpenAL, false)) {
				output = new WinMmOut();
			} else { output = new OpenALOut(); }
			
			output.Create(buffers);
			return output;
		}
		
		void DisposeOf(ref IAudioOutput output, ref Thread thread) {
			if (output == null) return;
			output.pendingStop = true;
			thread.Join();
			
			output.Dispose();
			output = null;
			thread = null;
		}
	}
}
