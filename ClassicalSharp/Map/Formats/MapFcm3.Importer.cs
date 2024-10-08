﻿// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
// Part of fCraft | Copyright (c) 2009-2014 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.IO;
using ClassicalSharp.Entities;
using Ionic.Zlib;

namespace ClassicalSharp.Map {

	/// <summary> Imports a world from a FCMv3 map file (fCraft server map) </summary>
	public sealed class MapFcm3Importer : IMapFormatImporter {
		
		const uint Identifier = 0x0FC2AF40;
		const byte Revision = 13;

		public byte[] Load(Stream stream, Game game, out int width, out int height, out int length) {
			BinaryReader r = new BinaryReader(stream);
			if (r.ReadInt32() != Identifier || r.ReadByte() != Revision)
				throw new InvalidDataException("Unexpected constant in .fcm file");

			width  = r.ReadInt16();
			height = r.ReadInt16();
			length = r.ReadInt16();

			LocalPlayer p = game.LocalPlayer;
			p.Spawn.X = r.ReadInt32() / 32f;
			p.Spawn.Y = r.ReadInt32() / 32f;
			p.Spawn.Z = r.ReadInt32() / 32f;
			p.SpawnRotY = (float)Utils.PackedToDegrees(r.ReadByte());
			p.SpawnHeadX = (float)Utils.PackedToDegrees(r.ReadByte());

			r.ReadUInt32(); // date modified
			r.ReadUInt32(); // date created
			game.World.Uuid = new Guid(r.ReadBytes(16));
			r.ReadBytes(26); // layer index
			int metaSize = r.ReadInt32();

			using (DeflateStream s = new DeflateStream(stream)) {
				r = new BinaryReader(s);
				for (int i = 0; i < metaSize; i++) {
					SkipString(r); // group
					SkipString(r); // key
					SkipString(r); // value
				}
				
				byte[] blocks = new byte[width * height * length];
				int read = s.Read(blocks, 0, blocks.Length);
				return blocks;
			}
		}
		
		static void SkipString(BinaryReader reader) {
			reader.ReadBytes(reader.ReadUInt16());
		}
	}
}