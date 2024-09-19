﻿// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using ClassicalSharp.GraphicsAPI;
using ClassicalSharp.Map;
using OpenTK;

namespace ClassicalSharp.Renderers {

	public sealed class SkyboxRenderer : IGameComponent {
		
		int tex, vb;
		Game game;
		const int count = 6 * 4;	
		public bool ShouldRender { get { return tex != 0 && !game.EnvRenderer.minimal; } }
		 
		void IGameComponent.Init(Game game) {
			this.game = game;
			Events.TextureChanged     += TextureChanged;
			Events.TexturePackChanged += TexturePackChanged;
			Events.EnvVariableChanged += EnvVariableChanged;
			Events.ContextLost += ContextLost;
			Events.ContextRecreated += ContextRecreated;
			ContextRecreated();
		}
		
		void IGameComponent.Reset(Game game) { }
		void IGameComponent.Ready(Game game) { }
		void IGameComponent.OnNewMap(Game game) { MakeVb(); }
		void IGameComponent.OnNewMapLoaded(Game game) { }
		
		void IDisposable.Dispose() {
			game.Graphics.DeleteTexture(ref tex);
			ContextLost();
			
			Events.TextureChanged     -= TextureChanged;
			Events.TexturePackChanged -= TexturePackChanged;
			Events.EnvVariableChanged -= EnvVariableChanged;
			Events.ContextLost -= ContextLost;
			Events.ContextRecreated -= ContextRecreated;			
		}
		
		void EnvVariableChanged(EnvVar envVar) {
			if (envVar != EnvVar.CloudsCol) return;
			MakeVb();
		}
		
		void TexturePackChanged() {
			game.Graphics.DeleteTexture(ref tex);
		}
		
		void TextureChanged(string name, byte[] data) {
			if (Utils.CaselessEq(name, "skybox.png")) {
				game.LoadTexture(ref tex, name, data);
			}
		}
		
		public void Render(double deltaTime) {
			if (vb == 0) return;
			game.Graphics.DepthWrite = false;
			game.Graphics.Texturing = true;
			game.Graphics.BindTexture(tex);
			game.Graphics.SetBatchFormat(VertexFormat.P3fT2fC4b);
			
			Matrix4 m = Matrix4.Identity, rotY, rotX, view;
			
			// Base skybox rotation
			float rotTime = (float)(game.accumulator * 2 * Math.PI); // So speed of 1 rotates whole skybox every second
			WorldEnv env = game.World.Env;
			Matrix4.RotateY(out rotY, env.SkyboxHorSpeed * rotTime);
			Matrix4.Mult(out m, ref m, ref rotY);
			Matrix4.RotateX(out rotX, env.SkyboxVerSpeed * rotTime);
			Matrix4.Mult(out m, ref m, ref rotX);
			
			// Rotate around camera
			Vector3 pos = game.CurrentCameraPos;
			game.CurrentCameraPos = Vector3.Zero;
			game.Camera.GetView(out view);
			Matrix4.Mult(out m, ref m, ref view);
			game.CurrentCameraPos = pos;
			
			game.Graphics.LoadMatrix(ref m);
			game.Graphics.BindVb(vb);
			game.Graphics.DrawVb_IndexedTris(count);
			
			game.Graphics.Texturing = false;
			game.Graphics.LoadMatrix(ref game.Graphics.View);
			game.Graphics.DepthWrite = true;
		}
		
		void ContextLost() { game.Graphics.DeleteVb(ref vb); }	
		void ContextRecreated() { MakeVb(); }
		
		
		unsafe void MakeVb() {
			if (game.Graphics.LostContext) return;
			game.Graphics.DeleteVb(ref vb);
			VertexP3fT2fC4b* vertices = stackalloc VertexP3fT2fC4b[count];
			IntPtr start = (IntPtr)vertices;
			
			const float pos = 1.0f;
			VertexP3fT2fC4b v; v.Col = game.World.Env.CloudsCol;
			
			// Render the front quad			                        
			v.X = -pos; v.Y = -pos; v.Z = -pos; v.U = 0.25f; v.V = 1.00f; *vertices = v; vertices++;
			v.X =  pos;                         v.U = 0.50f;              *vertices = v; vertices++;
			            v.Y =  pos;                          v.V = 0.50f; *vertices = v; vertices++;
			v.X = -pos;                         v.U = 0.25f;              *vertices = v; vertices++;
			
			// Render the left quad
			v.X = -pos; v.Y = -pos; v.Z =  pos; v.U = 0.00f; v.V = 1.00f; *vertices = v; vertices++;
			                        v.Z = -pos; v.U = 0.25f;              *vertices = v; vertices++;
			            v.Y =  pos;                          v.V = 0.50f; *vertices = v; vertices++;
			                        v.Z =  pos; v.U = 0.00f;              *vertices = v; vertices++;
			
			// Render the back quad			                       
			v.X =  pos; v.Y = -pos; v.Z =  pos; v.U = 0.75f; v.V = 1.00f; *vertices = v; vertices++;
			v.X = -pos;                         v.U = 1.00f;              *vertices = v; vertices++;
			            v.Y =  pos;                          v.V = 0.50f; *vertices = v; vertices++;
			v.X =  pos;                         v.U = 0.75f;              *vertices = v; vertices++;
			
			// Render the right quad
			v.X =  pos; v.Y = -pos; v.Z = -pos; v.U = 0.50f; v.V = 1.00f; *vertices = v; vertices++;
			                        v.Z =  pos; v.U = 0.75f;              *vertices = v; vertices++;
			            v.Y =  pos;                          v.V = 0.50f; *vertices = v; vertices++;
			                        v.Z = -pos; v.U = 0.50f;              *vertices = v; vertices++;
			
			// Render the top quad
			v.X =  pos; v.Y =  pos; v.Z = -pos;                           *vertices = v; vertices++;
			                        v.Z =  pos;              v.V = 0.00f; *vertices = v; vertices++;
			v.X = -pos;                         v.U = 0.25f;              *vertices = v; vertices++;
			                        v.Z = -pos;              v.V = 0.50f; *vertices = v; vertices++;
			
			// Render the bottom quad
			v.X =  pos; v.Y = -pos; v.Z = -pos; v.U = 0.75f;              *vertices = v; vertices++;
			                        v.Z =  pos;              v.V = 0.00f; *vertices = v; vertices++;
			v.X = -pos;                         v.U = 0.50f;              *vertices = v; vertices++;
			                        v.Z = -pos;              v.V = 0.50f; *vertices = v; vertices++;
			
			vb = game.Graphics.CreateVb(start, VertexFormat.P3fT2fC4b, count);
		}
	}
}