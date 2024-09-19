﻿// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using ClassicalSharp.GraphicsAPI;
using ClassicalSharp.Map;
using ClassicalSharp.Physics;
using OpenTK;
using BlockID = System.UInt16;

namespace ClassicalSharp.Renderers {

	public unsafe sealed class EnvRenderer : IGameComponent {
		
		int cloudsVb, cloudVertices, skyVb, skyVertices, cloudsTex;
		World map;
		Game game;
		internal bool legacy, minimal;
		
		double BlendFactor(float x) {
			//return -0.05 + 0.22 * (0.25 * Math.Log(x));
			double blend = -0.13 + 0.28 * (0.25 * Math.Log(x));
			if (blend < 0) blend = 0;
			if (blend > 1) blend = 1;
			return blend;
		}
		
		void CalcFog(out float density, out PackedCol col) {
			Vector3 pos = game.CurrentCameraPos;
			Vector3I coords = Vector3I.Floor(pos);
			
			BlockID block = game.World.SafeGetBlock(coords);
			AABB blockBB = new AABB(
				(Vector3)coords + BlockInfo.MinBB[block],
				(Vector3)coords + BlockInfo.MaxBB[block]);
			
			if (blockBB.Contains(pos) && BlockInfo.FogDensity[block] != 0) {
				density = BlockInfo.FogDensity[block];
				col = BlockInfo.FogCol[block];
			} else {
				density = 0;
				// Blend fog and sky together
				float blend = (float)BlendFactor(game.ViewDistance);
				col = PackedCol.Lerp(map.Env.FogCol, map.Env.SkyCol, blend);
			}
		}
		
		/// <summary> Sets mode to minimal environment rendering.
		/// - only sets the background/clear colour to blended fog colour.
		/// (no smooth fog, clouds, or proper overhead sky) </summary>
		public void UseLegacyMode(bool legacy) {
			this.legacy = legacy;
			ContextRecreated();
		}
		
		public void UseMinimalMode(bool minimal) {
			this.minimal = minimal;
			ContextRecreated();
		}
		
		public void Render(double deltaTime) {
			UpdateFog();
			if (minimal || skyVb == 0 || cloudsVb == 0) return;
			
			RenderSky(deltaTime);
			RenderClouds(deltaTime);
		}
		
		void EnvVariableChanged(EnvVar envVar) {
			if (minimal) return;
			
			if (envVar == EnvVar.SkyCol) {
				ResetSky();
			} else if (envVar == EnvVar.FogCol) {
				UpdateFog();
			} else if (envVar == EnvVar.CloudsCol) {
				ResetClouds();
			} else if (envVar == EnvVar.CloudsLevel) {
				ResetSky();
				ResetClouds();
			}
		}
		
		void IGameComponent.Init(Game game) {
			this.game = game;
			map = game.World;
			ResetAllEnv();
			
			Events.TextureChanged += TextureChanged;
			Events.EnvVariableChanged += EnvVariableChanged;
			Events.ViewDistanceChanged += ResetAllEnv;
			Events.ContextLost += ContextLost;
			Events.ContextRecreated += ContextRecreated;
			game.SetViewDistance(game.UserViewDistance);
		}
		
		void IGameComponent.Ready(Game game) { }
		void IGameComponent.Reset(Game game) { OnNewMap(game); }
		public void OnNewMap(Game game) {
			game.Graphics.Fog = false;
			ContextLost();
		}
		
		void IGameComponent.OnNewMapLoaded(Game game) {
			game.Graphics.Fog = !minimal;
			ResetAllEnv();
		}
		
		void TextureChanged(string name, byte[] data) {
			if (Utils.CaselessEq(name, "clouds.png")) {
				game.LoadTexture(ref cloudsTex, name, data);
			}
		}
		
		void ResetAllEnv() { ContextRecreated(); }
		
		void IDisposable.Dispose() {
			game.Graphics.DeleteTexture(ref cloudsTex);
			ContextLost();
			
			Events.TextureChanged -= TextureChanged;
			Events.EnvVariableChanged -= EnvVariableChanged;
			Events.ViewDistanceChanged -= ResetAllEnv;
			Events.ContextLost -= ContextLost;
			Events.ContextRecreated -= ContextRecreated;
		}
		
		void ContextLost() {
			game.Graphics.DeleteVb(ref skyVb);
			game.Graphics.DeleteVb(ref cloudsVb);
		}
		
		void ContextRecreated() {
			ContextLost();
			game.Graphics.Fog = !minimal;
			UpdateFog();
			
			if (minimal) return;
			ResetClouds();
			ResetSky();
		}
		
		void RenderSky(double delta) {
			if (game.SkyboxRenderer.ShouldRender) return;
			Vector3 pos = game.CurrentCameraPos;
			float normalY = map.Height + 8;
			float skyY = Math.Max(pos.Y + 8, normalY);
			IGraphicsApi gfx = game.Graphics;
			
			gfx.SetBatchFormat(VertexFormat.P3fC4b);
			gfx.BindVb(skyVb);
			if (skyY == normalY) {
				gfx.DrawVb_IndexedTris(skyVertices);
			} else {
				Matrix4 m = game.Graphics.View;
				float dy = skyY - normalY; // inlined Y translation matrix multiply
				m.Row3.X += dy * m.Row1.X; m.Row3.Y += dy * m.Row1.Y;
				m.Row3.Z += dy * m.Row1.Z; m.Row3.W += dy * m.Row1.W;
				
				gfx.LoadMatrix(ref m);
				gfx.DrawVb_IndexedTris(skyVertices);
				gfx.LoadMatrix(ref game.Graphics.View);
			}
		}
		
		void RenderClouds(double delta) {
			if (game.World.Env.CloudHeight < -2000) return;
			double time = game.accumulator;
			float offset = (float)(time / 2048f * 0.6f * map.Env.CloudsSpeed);
			IGraphicsApi gfx = game.Graphics;
			
			gfx.SetMatrixMode(MatrixType.Texture);
			Matrix4 matrix = Matrix4.Identity; matrix.Row3.X = offset; // translate X axis
			gfx.LoadMatrix(ref matrix);
			gfx.SetMatrixMode(MatrixType.Modelview);
			
			gfx.AlphaTest = true;
			gfx.Texturing = true;
			gfx.BindTexture(cloudsTex);
			gfx.SetBatchFormat(VertexFormat.P3fT2fC4b);
			gfx.BindVb(cloudsVb);
			gfx.DrawVb_IndexedTris(cloudVertices);
			gfx.AlphaTest = false;
			gfx.Texturing = false;
			
			gfx.SetMatrixMode(MatrixType.Texture);
			gfx.LoadIdentityMatrix();
			gfx.SetMatrixMode(MatrixType.Modelview);
		}
		
		void UpdateFogMinimal(float fogDensity) {
			// TODO: rewrite this to avoid raising the event? want to avoid recreating vbos too many times often
			if (fogDensity != 0) {
				// Exp fog mode: f = e^(-density*coord)
				// Solve coord for f = 0.05 (good approx for fog end)
				//   i.e. log(0.05) = -density * coord
				
				const double log005 = -2.99573227355399;
				double dist = log005 / -fogDensity;
				game.SetViewDistance((int)dist);
			} else {
				game.SetViewDistance(game.UserViewDistance);
			}
		}
		
		void UpdateFogNormal(float fogDensity, PackedCol fogCol) {
			IGraphicsApi gfx = game.Graphics;
			if (fogDensity != 0) {
				gfx.SetFogMode(Fog.Exp);
				gfx.SetFogDensity(fogDensity);
			} else if (game.World.Env.ExpFog) {
				gfx.SetFogMode(Fog.Exp);
				// f = 1-z/end   f = e^(-dz)
				//   solve for f = 0.01 gives:
				// e^(-dz)=0.01 --> -dz=ln(0.01)
				// 0.99=z/end   --> z=end*0.99
				//   therefore
				// d = -ln(0.01)/(end*0.99)
				
				const double log001 = -4.60517018598809;
				double density = -log001 / (game.ViewDistance * 0.99);
				gfx.SetFogDensity((float)density);
			} else {
				gfx.SetFogMode(Fog.Linear);
				gfx.SetFogEnd(game.ViewDistance);
			}
			gfx.SetFogCol(fogCol);
		}
		
		void UpdateFog() {
			float fogDensity; PackedCol fogCol;
			CalcFog(out fogDensity, out fogCol);
			game.Graphics.ClearCol(fogCol);
			
			if (!map.HasBlocks) return;
			if (minimal) {
				UpdateFogMinimal(fogDensity);
			} else {
				UpdateFogNormal(fogDensity, fogCol);
			}
		}
		
		void ResetClouds() {
			if (!map.HasBlocks || game.Graphics.LostContext) return;
			game.Graphics.DeleteVb(ref cloudsVb);
			RebuildClouds(game.ViewDistance, legacy ? 128 : 65536);
		}
		
		void ResetSky() {
			if (!map.HasBlocks || game.Graphics.LostContext) return;
			game.Graphics.DeleteVb(ref skyVb);
			RebuildSky(game.ViewDistance, legacy ? 128 : 65536);
		}
		
		void RebuildClouds(int extent, int axisSize) {
			extent = Utils.AdjViewDist(extent);
			int x1 = -extent, x2 = map.Width + extent;
			int z1 = -extent, z2 = map.Length + extent;
			cloudVertices = Utils.CountVertices(x2 - x1, z2 - z1, axisSize);
			
			VertexP3fT2fC4b[] vertices = new VertexP3fT2fC4b[cloudVertices];
			DrawCloudsY(x1, z1, x2, z2, map.Env.CloudHeight, axisSize, map.Env.CloudsCol, vertices);
			fixed (VertexP3fT2fC4b* ptr = vertices) {
				cloudsVb = game.Graphics.CreateVb((IntPtr)ptr, VertexFormat.P3fT2fC4b, cloudVertices);
			}
		}
		
		void RebuildSky(int extent, int axisSize) {
			extent = Utils.AdjViewDist(extent);
			int x1 = -extent, x2 = map.Width + extent;
			int z1 = -extent, z2 = map.Length + extent;
			skyVertices = Utils.CountVertices(x2 - x1, z2 - z1, axisSize);
			
			VertexP3fC4b[] vertices = new VertexP3fC4b[skyVertices];
			int height = Math.Max(map.Height + 2 + 6, map.Env.CloudHeight + 6);
			
			DrawSkyY(x1, z1, x2, z2, height, axisSize, map.Env.SkyCol, vertices);
			fixed (VertexP3fC4b* ptr = vertices) {
				skyVb = game.Graphics.CreateVb((IntPtr)ptr, VertexFormat.P3fC4b, skyVertices);
			}
		}
		
		void DrawSkyY(int x1, int z1, int x2, int z2, int y, int axisSize,
		              PackedCol col, VertexP3fC4b[] vertices) {
			int endX = x2, endZ = z2, startZ = z1;
			int i = 0;
			VertexP3fC4b v;
			v.Y = y; v.Col = col;
			
			for (; x1 < endX; x1 += axisSize) {
				x2 = x1 + axisSize;
				if (x2 > endX) x2 = endX;
				z1 = startZ;
				for (; z1 < endZ; z1 += axisSize) {
					z2 = z1 + axisSize;
					if (z2 > endZ) z2 = endZ;
					
					v.X = x1; v.Z = z1; vertices[i++] = v;
					v.Z = z2; vertices[i++] = v;
					v.X = x2;           vertices[i++] = v;
					v.Z = z1; vertices[i++] = v;
				}
			}
		}
		
		void DrawCloudsY(int x1, int z1, int x2, int z2, int y, int axisSize,
		                 PackedCol col, VertexP3fT2fC4b[] vertices) {
			int endX = x2, endZ = z2, startZ = z1;
			// adjust range so that largest negative uv coordinate is shifted to 0 or above.
			float offset = Utils.CeilDiv(-x1, 2048);
			int i = 0;
			VertexP3fT2fC4b v;
			v.Y = y + 0.1f; v.Col = col;
			
			for (; x1 < endX; x1 += axisSize) {
				x2 = x1 + axisSize;
				if (x2 > endX) x2 = endX;
				z1 = startZ;
				for (; z1 < endZ; z1 += axisSize) {
					z2 = z1 + axisSize;
					if (z2 > endZ) z2 = endZ;
					
					float u1 = x1 / 2048f + offset, u2 = x2 / 2048f + offset;
					float v1 = z1 / 2048f + offset, v2 = z2 / 2048f + offset;
					v.X = x1; v.Z = z1; v.U = u1; v.V = v1; vertices[i++] = v;
					v.Z = z2;           v.V = v2; vertices[i++] = v;
					v.X = x2;           v.U = u2;           vertices[i++] = v;
					v.Z = z1;           v.V = v1; vertices[i++] = v;
				}
			}
		}
	}
}