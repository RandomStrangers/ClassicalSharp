﻿// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using ClassicalSharp.Entities;
using ClassicalSharp.GraphicsAPI;
using ClassicalSharp.Map;
using ClassicalSharp.Textures;
using OpenTK;
using BlockID = System.UInt16;

namespace ClassicalSharp.Renderers {
	
	/// <summary> Manages the process of building/deleting chunk meshes,
	/// in addition to calculating the visibility of chunks. </summary>
	public sealed class ChunkUpdater : IDisposable {
		
		Game game;
		internal ChunkMeshBuilder builder;
		
		internal int[] distances;
		internal Vector3I chunkPos = new Vector3I(int.MaxValue);
		int elementsPerBitmap = 0;
		MapRenderer renderer;
		
		public void Update(double deltaTime) {
			if (renderer.chunks == null) return;
			UpdateSortOrder();
			UpdateChunks(deltaTime);
		}

		public void SetMeshBuilder(ChunkMeshBuilder newBuilder) {
			if (builder != null) builder.Dispose();			
			builder = newBuilder;
			builder.Init(game);
			builder.OnNewMapLoaded();
		}

		public ChunkMeshBuilder DefaultMeshBuilder() {
			if (game.SmoothLighting)
				return new AdvLightingMeshBuilder();
			return new NormalMeshBuilder();
		}
		
		public ChunkUpdater(Game game) {
			this.game = game;
			this.renderer = game.MapRenderer;
			
			Events.TerrainAtlasChanged += TerrainAtlasChanged;
			Events.OnNewMap            += OnNewMap;
			Events.OnNewMapLoaded      += OnNewMapLoaded;
			Events.EnvVariableChanged  += EnvVariableChanged;
			
			Events.BlockDefinitionChanged += BlockDefinitionChanged;
			Events.ViewDistanceChanged    += ViewDistanceChanged;
			Events.ProjectionChanged      += ProjectionChanged;
			Events.ContextLost      += ContextLost;
			Events.ContextRecreated += ContextRecreated;
			
			SetMeshBuilder(DefaultMeshBuilder());
		}
		
		public void Dispose() {
			ClearChunkCache();
			renderer.chunks = null;
			renderer.unsortedChunks = null;
			Events.TerrainAtlasChanged -= TerrainAtlasChanged;
			Events.OnNewMap            -= OnNewMap;
			Events.OnNewMapLoaded      -= OnNewMapLoaded;
			Events.EnvVariableChanged  -= EnvVariableChanged;
			
			Events.BlockDefinitionChanged -= BlockDefinitionChanged;
			Events.ViewDistanceChanged    -= ViewDistanceChanged;
			Events.ProjectionChanged      -= ProjectionChanged;
			Events.ContextLost      -= ContextLost;
			Events.ContextRecreated -= ContextRecreated;
			builder.Dispose();
		}
		
		public void Refresh() {
			chunkPos = new Vector3I(int.MaxValue);
			if (renderer.chunks != null && game.World.HasBlocks) {
				ClearChunkCache();
				ResetChunkCache();
			}
			
			ResetPartsCounts();
		}
		
		void RefreshBorders(int clipLevel) {
			chunkPos = new Vector3I(int.MaxValue);
			if (renderer.chunks == null || !game.World.HasBlocks) return;
			
			for (int cz = 0; cz < chunksZ; cz++)
				for (int cy = 0; cy < chunksY; cy++)
					for (int cx = 0; cx < chunksX; cx++)
			{
				bool isBorder = cx == 0 || cz == 0 || cx == (chunksX - 1) || cz == (chunksZ - 1);
				if (isBorder && (cy * 16) < clipLevel) {
					renderer.RefreshChunk(cx, cy, cz);
				}
			}
		}
		
		void EnvVariableChanged(EnvVar envVar) {
			if (envVar == EnvVar.SunCol || envVar == EnvVar.ShadowCol) {
				Refresh();
			} else if (envVar == EnvVar.EdgeLevel || envVar == EnvVar.SidesOffset) {
				int oldClip = builder.edgeLevel;
				builder.sidesLevel = Math.Max(0, game.World.Env.SidesHeight);
				builder.edgeLevel = Math.Max(0, game.World.Env.EdgeHeight);
				RefreshBorders(Math.Max(oldClip, builder.edgeLevel));
			}
		}

		void TerrainAtlasChanged() {
			if (renderer._1DUsed != -1) {
				bool refreshRequired = elementsPerBitmap != Atlas1D.TilesPerAtlas;
				if (refreshRequired) Refresh();
			}
			
			renderer._1DUsed = Atlas1D.UsedAtlasesCount();
			elementsPerBitmap = Atlas1D.TilesPerAtlas;
			ResetUsedFlags();
		}
		
		void BlockDefinitionChanged() {
			renderer._1DUsed = Atlas1D.UsedAtlasesCount();
			ResetUsedFlags();
			Refresh();
		}
		
		void ProjectionChanged() {
			lastCamPos = Utils.MaxPos();
		}
		
		void ViewDistanceChanged() {
			lastCamPos = Utils.MaxPos();
		}

		void ResetUsedFlags() {
			for (int i = 0; i < Atlas1D.MaxAtlases; i++) {
				renderer.pendingTranslucent[i] = true;
				renderer.usedTranslucent[i]    = false;
				renderer.pendingNormal[i]      = true;
				renderer.usedNormal[i]         = false;
			}
		}
		
		void ResetPartsCounts() {
			for (int i = 0; i < Atlas1D.MaxAtlases; i++) {
				renderer.normalPartsCount[i]      = 0;
				renderer.translucentPartsCount[i] = 0;
			}
		}
		
		void OnNewMap() {
			game.ChunkUpdates = 0;
			ClearChunkCache();
			ResetPartsCounts();
			
			renderer.chunks = null;
			renderer.unsortedChunks = null;
			chunkPos = new Vector3I(int.MaxValue, int.MaxValue, int.MaxValue);
		}
				
		int chunksX, chunksY, chunksZ;
		void OnNewMapLoaded() {
			chunksX = Utils.CeilDiv(game.World.Width,  16); renderer.chunksX = chunksX;
			chunksY = Utils.CeilDiv(game.World.Height, 16); renderer.chunksY = chunksY;
			chunksZ = Utils.CeilDiv(game.World.Length, 16); renderer.chunksZ = chunksZ;
			
			int count = chunksX * chunksY * chunksZ;
			if (renderer.chunks == null || renderer.chunks.Length != count) {
				renderer.chunks = new ChunkInfo[count];
				renderer.unsortedChunks = new ChunkInfo[count];
				renderer.renderChunks = new ChunkInfo[count];
				distances = new int[count];
			}
			CreateChunkCache();
			builder.OnNewMapLoaded();
			lastCamPos = Utils.MaxPos();
		}
		
		void CreateChunkCache() {
			int index = 0;
			for (int z = 0; z < game.World.Length; z += 16)
				for (int y = 0; y < game.World.Height; y += 16)
					for (int x = 0; x < game.World.Width; x += 16)
			{
				renderer.chunks[index] = new ChunkInfo(x, y, z);
				renderer.unsortedChunks[index] = renderer.chunks[index];
				renderer.renderChunks[index] = renderer.chunks[index];
				distances[index] = 0;
				index++;
			}
		}
		
		void ResetChunkCache() {
			int index = 0;
			for (int z = 0; z < game.World.Length; z += 16)
				for (int y = 0; y < game.World.Height; y += 16)
					for (int x = 0; x < game.World.Width; x += 16)
			{
				renderer.unsortedChunks[index].Reset(x, y, z);
				index++;
			}
		}
		
		void ClearChunkCache() {
			if (renderer.chunks == null) return;
			for (int i = 0; i < renderer.chunks.Length; i++) {
				DeleteChunk(renderer.chunks[i]);
			}
			
			ResetPartsCounts();
		}
		
		void DeleteChunk(ChunkInfo info) {
			info.Empty = false; info.AllAir = false;
			#if OCCLUSION
			info.OcclusionFlags = 0;
			info.OccludedFlags = 0;
			#endif
			#if !GL11
			game.Graphics.DeleteVb(ref info.Vb);
			#endif
			
			if (info.NormalParts != null) {
				ChunkPartInfo[] parts = info.NormalParts;
				for (int i = 0; i < parts.Length; i++) {
					if (parts[i].Offset < 0) continue;
					renderer.normalPartsCount[i]--;
					#if GL11
					game.Graphics.DeleteVb(ref parts[i].Vb);
					#endif
				}
				info.NormalParts = null;
			}
			
			if (info.TranslucentParts != null) {
				ChunkPartInfo[] parts = info.TranslucentParts;
				for (int i = 0; i < parts.Length; i++) {
					if (parts[i].Offset < 0) continue;
					renderer.translucentPartsCount[i]--;
					#if GL11
					game.Graphics.DeleteVb(ref parts[i].Vb);
					#endif
				}
				info.TranslucentParts = null;
			}
		}
		
		void ContextLost() { ClearChunkCache(); }
		void ContextRecreated() { Refresh(); }
		

		int chunksTarget = 12;
		const double targetTime = (1.0 / 30) + 0.01;
		void UpdateChunks(double delta) {
			int chunkUpdates = 0;
			chunksTarget += delta < targetTime ? 1 : -1; // build more chunks if 30 FPS or over, otherwise slowdown.
			Utils.Clamp(ref chunksTarget, 4, game.MaxChunkUpdates);
			
			LocalPlayer p = game.LocalPlayer;
			Vector3 cameraPos = game.CurrentCameraPos;
			bool samePos = cameraPos == lastCamPos && p.HeadY == lastRotY && p.HeadX == lastHeadX;
			renderer.renderCount = samePos ? UpdateChunksStill(ref chunkUpdates) :
				UpdateChunksAndVisibility(ref chunkUpdates);
			
			lastCamPos = cameraPos;
			lastRotY = p.HeadY; lastHeadX = p.HeadX;
			if (!samePos || chunkUpdates != 0)
				ResetUsedFlags();
		}
		Vector3 lastCamPos;
		float lastRotY, lastHeadX;
		
		int UpdateChunksAndVisibility(ref int chunkUpdates) {
			ChunkInfo[] chunks = renderer.chunks, render = renderer.renderChunks;
			int j = 0;
			int viewDistSqr = AdjustViewDist(game.ViewDistance);
			int userDistSqr = AdjustViewDist(game.UserViewDistance);
			
			for (int i = 0; i < chunks.Length; i++) {
				ChunkInfo info = chunks[i];
				if (info.Empty) continue;
				int distSqr = distances[i];
				bool noData = info.NormalParts == null && info.TranslucentParts == null;
				
				// Unload chunks beyond visible range
				if (!noData && distSqr >= userDistSqr + 32 * 16) {
					DeleteChunk(info); continue;
				}
				noData |= info.PendingDelete;
				
				if (noData && distSqr <= viewDistSqr && chunkUpdates < chunksTarget) {
					DeleteChunk(info);
					BuildChunk(info, ref chunkUpdates);
				}
				info.Visible = distSqr <= viewDistSqr &&
					game.Culling.SphereInFrustum(info.CentreX, info.CentreY, info.CentreZ, 14); // 14 ~ sqrt(3 * 8^2)
				if (info.Visible && !info.Empty) { render[j] = info; j++; }
			}
			return j;
		}
		
		int UpdateChunksStill(ref int chunkUpdates) {
			ChunkInfo[] chunks = renderer.chunks, render = renderer.renderChunks;
			int j = 0;
			int viewDistSqr = AdjustViewDist(game.ViewDistance);
			int userDistSqr = AdjustViewDist(game.UserViewDistance);
			
			for (int i = 0; i < chunks.Length; i++) {
				ChunkInfo info = chunks[i];
				if (info.Empty) continue;
				int distSqr = distances[i];
				bool noData = info.NormalParts == null && info.TranslucentParts == null;
				
				if (!noData && distSqr >= userDistSqr + 32 * 16) {
					DeleteChunk(info); continue;
				}
				noData |= info.PendingDelete;
				
				if (noData && distSqr <= userDistSqr && chunkUpdates < chunksTarget) {
					DeleteChunk(info);
					BuildChunk(info, ref chunkUpdates);
					
					// only need to update the visibility of chunks in range.
					info.Visible = distSqr <= viewDistSqr &&
						game.Culling.SphereInFrustum(info.CentreX, info.CentreY, info.CentreZ, 14); // 14 ~ sqrt(3 * 8^2)
					if (info.Visible && !info.Empty) { render[j] = info; j++; }
				} else if (info.Visible) {
					render[j] = info; j++;
				}
			}
			return j;
		}
		
		
		static int AdjustViewDist(int dist) {
			int viewDist = Utils.AdjViewDist(Math.Max(16, dist));
			return (viewDist + 24) * (viewDist + 24);
		}
		
		
		void BuildChunk(ChunkInfo info, ref int chunkUpdates) {
			game.ChunkUpdates++;
			chunkUpdates++;
			info.PendingDelete = false;
			builder.MakeChunk(info);
			
			if (info.NormalParts == null && info.TranslucentParts == null) {
				info.Empty = true;
				return;
			}
			
			if (info.NormalParts != null) {
				ChunkPartInfo[] parts = info.NormalParts;
				for (int i = 0; i < parts.Length; i++) {
					if (parts[i].Offset < 0) continue;
					renderer.normalPartsCount[i]++;
				}
			}
			if (info.TranslucentParts != null) {
				ChunkPartInfo[] parts = info.TranslucentParts;
				for (int i = 0; i < parts.Length; i++) {
					if (parts[i].Offset < 0) continue;
					renderer.translucentPartsCount[i]++;
				}
			}
		}
		
		
		void UpdateSortOrder() {
			Vector3 cameraPos = game.CurrentCameraPos;
			Vector3I newChunkPos = Vector3I.Floor(cameraPos);
			newChunkPos.X = (newChunkPos.X & ~0x0F) + 8;
			newChunkPos.Y = (newChunkPos.Y & ~0x0F) + 8;
			newChunkPos.Z = (newChunkPos.Z & ~0x0F) + 8;
			if (newChunkPos == chunkPos) return;
			
			ChunkInfo[] chunks = game.MapRenderer.chunks;
			int[] distances = this.distances;
			Vector3I pPos = newChunkPos;
			chunkPos = pPos;
			
			for (int i = 0; i < chunks.Length; i++) {
				ChunkInfo info = chunks[i];
				
				// Calculate distance to chunk centre
				int dx = info.CentreX - pPos.X, dy = info.CentreY - pPos.Y, dz = info.CentreZ - pPos.Z;
				distances[i] = dx * dx + dy * dy + dz * dz;
				
				// Can work out distance to chunk faces as offset from distance to chunk centre on each axis.
				int dXMin = dx - 8, dXMax = dx + 8;
				int dYMin = dy - 8, dYMax = dy + 8;
				int dZMin = dz - 8, dZMax = dz + 8;
				
				// Back face culling: make sure that the chunk is definitely entirely back facing.
				info.DrawLeft = !(dXMin <= 0 && dXMax <= 0);
				info.DrawRight = !(dXMin >= 0 && dXMax >= 0);
				info.DrawFront = !(dZMin <= 0 && dZMax <= 0);
				info.DrawBack = !(dZMin >= 0 && dZMax >= 0);
				info.DrawBottom = !(dYMin <= 0 && dYMax <= 0);
				info.DrawTop = !(dYMin >= 0 && dYMax >= 0);
			}

			// NOTE: Over 5x faster compared to normal comparison of IComparer<ChunkInfo>.Compare
			if (distances.Length > 1) {
				QuickSort(distances, chunks, 0, chunks.Length - 1);
			}
			ResetUsedFlags();
			//SimpleOcclusionCulling();
		}
		
		static void QuickSort(int[] keys, ChunkInfo[] values, int left, int right) {
			while (left < right) {
				int i = left, j = right;
				int pivot = keys[(i + j) / 2];
				// partition the list
				while (i <= j) {
					while (pivot > keys[i]) i++;
					while (pivot < keys[j]) j--;
					
					if (i <= j) {
						int key = keys[i]; keys[i] = keys[j]; keys[j] = key;
						ChunkInfo value = values[i]; values[i] = values[j]; values[j] = value;
						i++; j--;
					}
				}
				
				// recurse into the smaller subset
				if (j - left <= right - i) {
					if (left < j)
						QuickSort(keys, values, left, j);
					left = i;
				} else {
					if (i < right)
						QuickSort(keys, values, i, right);
					right = j;
				}
			}
		}
	}
}