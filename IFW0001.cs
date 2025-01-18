using UnityEngine;
using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;
//using System;
//using System.IO;
//using System.Diagnostics;
//using Math;

[BepInPlugin("s649_immersive_fairy_weakness", "IFW", "1.1.0.0")]
public class IFWMain : BaseUnityPlugin {
	internal const int EL_FairyWeak = 1204;
	internal const int EL_WeightLifting = 207;
	private static ConfigEntry<float> CE_WeightLimitDivide;
	public static float configWeightLimitDivide {
		get {return Mathf.Clamp(CE_WeightLimitDivide.Value,1f,10f);}
	}
    private void Start() {
		CE_WeightLimitDivide = Config.Bind("#1_WeightLimit", "WeightLimitDivide", 10.0f, "Fairy PC's Weight Limit will be divided by this value");

        var harmony = new Harmony("IFWMain");
        harmony.PatchAll();
    }

	public static int WeightCanKeepHandle(Chara c){
		return (c.HasElement(EL_WeightLifting))? 5000 + c.elements.Value(EL_WeightLifting) * 10: 5000;
	}
	public static int WeightCanKeepLift(Chara c){
		return (c.HasElement(EL_WeightLifting))? 10000 + c.elements.Value(EL_WeightLifting) * 20: 10000;
	}
	public static bool IsOnGlobalMap(){
            return (EClass.pc.currentZone.id == "ntyris") ? true : false;
        }
}

namespace Fairy_weight{
	
	[HarmonyPatch]
	public class WeightPatch
	{
		
		[HarmonyPostfix]
		[HarmonyPatch(typeof(Chara), "WeightLimit", MethodType.Getter)]
		public static void WeightLimit_PostPatch(Chara __instance, ref int __result)
		{
            Chara c = __instance;
			if (c.IsPC && c.HasElement(IFWMain.EL_FairyWeak))
			{
				float rs = (float)(__result) / IFWMain.configWeightLimitDivide;
                __result = (int)rs;
            	//__result = __instance.STR * 50 + __instance.END * 25 + __instance.Evalue(207) * 500 + 4500;
			}
		}
		
	}
	[HarmonyPatch]
	public class TickPatch{
		private static AIAct actBefore;

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Chara),"TickConditions")]
		public static void TickConditionsPatch(Chara __instance){
			Chara c = __instance;
			
			if(c.IsPC && c.HasElement(IFWMain.EL_FairyWeak)){
				//if(!IFWMain.IsOnGlobalMap()){Debug.Log("[IFW}]C:" + c.ai.ToString());}
				if(actBefore != null){
					if(actBefore.ToString() != c.ai.ToString())
					Debug.Log("[IFW}]C:" + c.ai.ToString() + ":before->" + actBefore.ToString());
				} else {
					Debug.Log("[IFW}]C:" + c.ai.ToString());
				}

				
				if(c.ai is GoalManualMove){
					if(c.held != null){
						//Debug.Log("[IFW] " + c.held.ToString());
						if(c.held.ChildrenAndSelfWeight > IFWMain.WeightCanKeepLift(c)){
							Msg.Say("tooHeavyToEquip", c.held);
							c.DropHeld();
							
						}
					}
				}
				if(c.ai is TaskHarvest){
					if(c.held != null){
						//Debug.Log("[IFW] " + c.held.ToString());
						if(c.held.ChildrenAndSelfWeight > IFWMain.WeightCanKeepHandle(c)){
							Msg.Say("tooHeavyToEquip", c.held);
							c.ai.Current.TryCancel(c.held);
							
						}
					}
				}
				//ai is AI_PlayMusic
				if(c.ai is AI_PlayMusic){
					AI_PlayMusic aiplay = (AI_PlayMusic)c.ai;
					if(c.held == aiplay.tool && c.held.ChildrenAndSelfWeight > IFWMain.WeightCanKeepHandle(c)){
						Msg.Say("tooHeavyToEquip", c.held);
						c.ai.Current.TryCancel(c.held);
					}
				}

				actBefore = c.ai;
			}
		}
		
	}

	[HarmonyPatch]
	public class ThrowPatch
	{
		[HarmonyPrefix]
		[HarmonyPatch(typeof(ActThrow), "CanThrow")]
		public static bool CanThrowTest(Chara c,Thing t,Card target,Point p)
		{
			
			if(t.SelfWeight >  IFWMain.WeightCanKeepHandle(c)){
				//if(t != null){Debug.Log("[IFW]throw : "+ t.ToString() + "->" + t.SelfWeight.ToString());}
				//Msg.SayRaw("TooHeavy");
				return false;
			}
			
			return true;
			
		}	
	}
	[HarmonyPatch]
	public class RangedPatch
	{
		[HarmonyPrefix]
		[HarmonyPatch(typeof(ActRanged), "CanPerform")]
		public static bool CanActRangedTest(ActRanged __instance)
		{
			ActRanged ar = __instance;
			if(ar != null){
				Chara cc = Act.CC;
				Thing tool = Act.TOOL;
				if(cc != null && tool != null){Debug.Log("[IFW]Ranged : "+ cc.ToString() + "->" + tool.ToString());}
				
				//Msg.SayRaw("TooHeavy");
				//return false;
			}
			
			return true;
			
		}	
	}
}

//namespace TooHeavyToHandle {
////[HarmonyPatch]
		
		
//}
/*
		public WeightPatch()
		{
		}
        
		
		[HarmonyPrefix]
		[HarmonyPatch(typeof(Chara), "CanPick", MethodType.Getter)]
		public static void CanPick_PrePatch(Chara __instance, ref Card c)
		{
            //Player pc = __instance;
            //Chara c = pc.chara;
            Debug.Log("これ呼ばれてんの？");
           // Chara c = __instance;

			
		}
		*/


//Debug.Log("Fairy PC Found!");
                //int k = c.karma;
                //if(k > 100){k = 100;}
                //if(k < 1){k = 1;}
                //Debug.Log("height is "+ c.bio.height);
                //Debug.Log("karma is "+ pc.karma);
                //pc.chara.bio.height = pc.chara.race.height * k / 100; //height mod