﻿// ClassicalSharp copyright 2014-2016 UnknownShadow200 | Licensed under MIT
using System;
using System.IO;
using ClassicalSharp;
using ClassicalSharp.Network;
using SharpWave;

namespace Launcher.Patcher {
	
	public sealed class SoundPatcher {

		string[] files, hashes, identifiers;
		string prefix;
		public bool Done;
		
		public SoundPatcher(string[] files, string[] hashes, string prefix) {
			this.files = files;
			this.hashes = hashes;
			this.prefix = prefix;
		}
		
		public void FetchFiles(ResourceFetcher fetcher, bool allExist) {
			if (allExist) { Done = true; return; }
			
			identifiers = new string[files.Length];
			for (int i = 0; i < files.Length; i++) {
				identifiers[i] = prefix + files[i];
			}
			
			for (int i = 0; i < files.Length; i++) {
				string url = ResourceFetcher.assetsUri + hashes[i];
				fetcher.QueueItem(url, identifiers[i]);
			}
		}
		
		public bool CheckDownloaded(ResourceFetcher fetcher, Action<string> setStatus) {
			if (Done) return true;
			for (int i = 0; i < identifiers.Length; i++) {
				Request item;
				if (fetcher.downloader.TryGetItem(identifiers[i], out item)) {
					fetcher.FilesToDownload.RemoveAt(0);
					Utils.LogDebug("got sound " + identifiers[i]);
					
					if (item.Data == null) {
						setStatus("&cFailed to download " + identifiers[i]);
					} else {
						DecodeSound(files[i], (byte[])item.Data);
					}
					
					if (i == identifiers.Length - 1)
						Done = true;
					setStatus(fetcher.MakeNext());
				}
			}
			return true;
		}
		
		void DecodeSound(string name, byte[] rawData) {
			string path = Path.Combine("audio", prefix + name + ".wav");
			
			using (Stream dst = Platform.FileCreate(path))
				using (MemoryStream src = new MemoryStream(rawData))
			{
				dst.SetLength(44);
				VorbisCodec codec = new VorbisCodec();
				AudioFormat format = codec.ReadHeader(src);
				
				foreach (AudioChunk chunk in codec.StreamData(src)) {
					dst.Write(chunk.Data, 0, chunk.Length);
				}
				
				dst.Position = 0;
				BinaryWriter w = new BinaryWriter(dst);
				WriteWaveHeader(w, dst, format);
			}
		}
		
		void WriteWaveHeader(BinaryWriter w, Stream stream, AudioFormat format) {
			WriteFourCC(w, "RIFF");
			w.Write((int)(stream.Length - 8));
			WriteFourCC(w, "WAVE");
			
			WriteFourCC(w, "fmt ");
			w.Write(16);
			w.Write((ushort)1); // audio format, PCM
			w.Write((ushort)format.Channels);
			w.Write(format.SampleRate);
			w.Write((format.SampleRate * format.Channels * format.BitsPerSample) / 8); // byte rate
			w.Write((ushort)((format.Channels * format.BitsPerSample) / 8)); // block align
			w.Write((ushort)format.BitsPerSample);
			
			WriteFourCC(w, "data");
			w.Write((int)(stream.Length - 44));
		}
		
		void WriteFourCC(BinaryWriter w, string fourCC) {
			w.Write((byte)fourCC[0]); w.Write((byte)fourCC[1]);
			w.Write((byte)fourCC[2]); w.Write((byte)fourCC[3]);
		}
	}
}
