﻿// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using ClassicalSharp.Entities;
using ClassicalSharp.Physics;
using OpenTK;

namespace ClassicalSharp.Model {
	
	public class HumanoidModel : IModel {
		
		public ModelSet Set, SetSlim, Set64;
		public HumanoidModel(Game game) : base(game) {
			CalcHumanAnims = true;
			UsesHumanSkin = true;
		}
		
		protected BoxDesc head, torso, lLeg, rLeg, lArm, rArm;
		protected float offset = 0.5f;
		public override void CreateParts() {
			vertices = new ModelVertex[boxVertices * (7 + 7 + 4)];
			Set = new ModelSet();
			MakeDescriptions();
			
			Set.Head = BuildBox(head.TexOrigin(0, 0));
			Set.Torso = BuildBox(torso.TexOrigin(16, 16));
			Set.LeftLeg = BuildBox(lLeg.TexOrigin(0, 16));
			Set.RightLeg = BuildBox(rLeg.TexOrigin(0, 16));
			Set.Hat = BuildBox(head.TexOrigin(32, 0).Expand(offset));
			Set.LeftArm = BuildBox(lArm.TexOrigin(40, 16));
			Set.RightArm = BuildBox(rArm.TexOrigin(40, 16));
			lArm = lArm.MirrorX(); lLeg = lLeg.MirrorX();
			
			Set64 = new ModelSet();
			Set64.Head = Set.Head;
			Set64.Torso = Set.Torso;
			Set64.LeftLeg = BuildBox(lLeg.TexOrigin(16, 48));
			Set64.RightLeg = Set.RightLeg;
			Set64.Hat = Set.Hat;
			Set64.LeftArm = BuildBox(lArm.TexOrigin(32, 48));
			Set64.RightArm = Set.RightArm;
			
			Set64.TorsoLayer = BuildBox(torso.TexOrigin(16, 32).Expand(offset));
			Set64.LeftLegLayer = BuildBox(lLeg.TexOrigin(0, 48).Expand(offset));
			Set64.RightLegLayer = BuildBox(rLeg.TexOrigin(0, 32).Expand(offset));
			Set64.LeftArmLayer = BuildBox(lArm.TexOrigin(48, 48).Expand(offset));
			Set64.RightArmLayer = BuildBox(rArm.TexOrigin(40, 32).Expand(offset));
			
			SetSlim = new ModelSet();
			SetSlim.Head = Set64.Head;
			SetSlim.Torso = Set64.Torso;
			SetSlim.LeftLeg = Set64.LeftLeg;
			SetSlim.RightLeg = Set64.RightLeg;
			SetSlim.Hat = Set64.Hat;
			lArm.SizeX -= 1; lArm.X1 += (offset * 2)/16f;
			SetSlim.LeftArm = BuildBox(lArm.TexOrigin(32, 48));
			rArm.SizeX -= 1; rArm.X2 -= (offset * 2)/16f;
			SetSlim.RightArm = BuildBox(rArm.TexOrigin(40, 16));
			
			SetSlim.TorsoLayer = Set64.TorsoLayer;
			SetSlim.LeftLegLayer = Set64.LeftLegLayer;
			SetSlim.RightLegLayer = Set64.RightLegLayer;
			SetSlim.LeftArmLayer = BuildBox(lArm.TexOrigin(48, 48).Expand(offset));
			SetSlim.RightArmLayer = BuildBox(rArm.TexOrigin(40, 32).Expand(offset));
		}
		
		protected virtual void MakeDescriptions() {
			head = MakeBoxBounds(-4, 24, -4, 4, 32, 4).RotOrigin(0, 24, 0);
			torso = MakeBoxBounds(-4, 12, -2, 4, 24, 2);
			lLeg = MakeBoxBounds(0, 0, -2, -4, 12, 2).RotOrigin(0, 12, 0);
			rLeg = MakeBoxBounds(0, 0, -2, 4, 12, 2).RotOrigin(0, 12, 0);
			lArm = MakeBoxBounds(-4, 12, -2, -8, 24, 2).RotOrigin(-5, 22, 0);
			rArm = MakeBoxBounds(4, 12, -2, 8, 24, 2).RotOrigin(5, 22, 0);
		}
		
		public override float NameYOffset { get { return 32.5f/16f; } }		
		public override float GetEyeY(Entity entity) { return 26/16f; }
		
		public override Vector3 CollisionSize {
			get { return new Vector3(8.6f/16f, 28.1f/16f, 8.6f/16f); }
		}
		
		public override AABB PickingBounds {
			get { return new AABB(-8/16f, 0, -4/16f, 8/16f, 32/16f, 4/16f); }
		}
		
		public override void DrawModel(Entity p) {
			ApplyTexture(p);
			// players should not be able to use invisible skins
			game.Graphics.AlphaTest = false;
			
			SkinType skinType = IModel.skinType;
			ModelSet model = skinType == SkinType.Type64x64Slim ? SetSlim :
				(skinType == SkinType.Type64x64 ? Set64 : Set);
			
			DrawRotate(-p.HeadXRadians, 0, 0, model.Head, true);
			DrawPart(model.Torso);
			
			DrawRotate(p.anim.leftLegX, 0, p.anim.leftLegZ, model.LeftLeg, false);
			DrawRotate(p.anim.rightLegX, 0, p.anim.rightLegZ, model.RightLeg, false);
			Rotate = RotateOrder.XZY;
			DrawRotate(p.anim.leftArmX, 0, p.anim.leftArmZ, model.LeftArm, false);
			DrawRotate(p.anim.rightArmX, 0, p.anim.rightArmZ, model.RightArm, false);
			Rotate = RotateOrder.ZYX;
			UpdateVB();
			
			game.Graphics.AlphaTest = true;
			index = 0;
			if (skinType != SkinType.Type64x32) {
				DrawPart(model.TorsoLayer);
				DrawRotate(p.anim.leftLegX, 0, p.anim.leftLegZ, model.LeftLegLayer, false);
				DrawRotate(p.anim.rightLegX, 0, p.anim.rightLegZ, model.RightLegLayer, false);
				Rotate = RotateOrder.XZY;
				DrawRotate(p.anim.leftArmX, 0, p.anim.leftArmZ, model.LeftArmLayer, false);
				DrawRotate(p.anim.rightArmX, 0, p.anim.rightArmZ, model.RightArmLayer, false);
				Rotate = RotateOrder.ZYX;
			}
			DrawRotate(-p.HeadXRadians, 0, 0, model.Hat, true);
			UpdateVB();
		}
		
		public override void DrawArm(Entity p) {
			SkinType skin = IModel.skinType;
			ModelSet model = skin == SkinType.Type64x64Slim ? SetSlim : 
				(skin == SkinType.Type64x64 ? Set64 : Set);
						
			DrawArmPart(model.RightArm);
			if (skin != SkinType.Type64x32) {
				DrawArmPart(model.RightArmLayer);
			}
			UpdateVB();
		}
		
		public class ModelSet {
			public ModelPart Head, Torso, LeftLeg, RightLeg, LeftArm, RightArm, Hat,
			TorsoLayer, LeftLegLayer, RightLegLayer, LeftArmLayer, RightArmLayer;
		}
	}
}