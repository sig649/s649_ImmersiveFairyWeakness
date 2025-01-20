using UnityEngine;
using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;
//using Fairy_weight;
//using Fairy_weight.IFWMain;
//using System;
//using System.IO;
//using System.Diagnostics;


namespace Fairy_weight{

[BepInPlugin("s649_immersive_fairy_weakness", "IFW", "1.1.0.0")]
public class IFWMain : BaseUnityPlugin {
	internal const int EL_FairyWeak = 1204;
	internal const int EL_WeightLifting = 207;
	//------configEntry-------------------------------------------------------------------------------------------------------
	private static ConfigEntry<bool> CE_FlagWeightLimitPenalty;//重量限界にペナルティがかかるかどうか
	private static ConfigEntry<bool> CE_FlagKeepHandleMod;//手持ちのアイテムを扱えるかどうか//このMODのほとんどの動作で使用
	//private static ConfigEntry<bool> CE_FlagKeepLiftMod;//アイテムを持ち上げられるか　↑とまとめる
	private static ConfigEntry<bool> CE_FlagTutorialRescue;//チュートリアルで救済をするかどうか

	private static ConfigEntry<float> CE_WeightLimitMulti;//重量限界乗算値
	private static ConfigEntry<int> CE_BaseWeightCanKeepHandle;//扱えるアイテムの重さの基本値
	private static ConfigEntry<int> CE_ModWeightCanKeepHandle;//扱えるアイテムの重さの成長値
	private static ConfigEntry<int> CE_BaseWeightCanKeepLift;//持ち上げられる重さの基本値
	private static ConfigEntry<int> CE_ModWeightCanKeepLift;//持ち上げられる重さの成長値
	//---------------config props -------------------------------------------------------------------------------------
	public static bool configFlagWeightLimitPenalty {
		get {return CE_FlagWeightLimitPenalty.Value;}
	}
	public static bool configFlagKeepHandleMod {
		get {return CE_FlagKeepHandleMod.Value;}
	}
	/*
	public static bool configFlagKeepLiftMod {
		get {return CE_FlagKeepLiftMod.Value;}
	}
	*/
	public static bool configFlagTutorialRescue {
		get {return CE_FlagTutorialRescue.Value;}
	}
	public static float configWeightLimitMulti {
		get {return Mathf.Clamp(CE_WeightLimitMulti.Value,0.05f,1f);}
	}
	public static int configBaseWeightCanKeepHandle {
		get {return Mathf.Clamp(CE_BaseWeightCanKeepHandle.Value,1000,50000);}
	}
	public static int configModWeightCanKeepHandle {
		get {return Mathf.Clamp(CE_ModWeightCanKeepHandle.Value,1,100);}
	}
	public static int configBaseWeightCanKeepLift {
		get {return Mathf.Clamp(CE_BaseWeightCanKeepLift.Value,1000,100000);}
	}
	public static int configModWeightCanKeepLift {
		get {return Mathf.Clamp(CE_ModWeightCanKeepLift.Value,1,200);}
	}
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////
	
    private void Start() {
		CE_FlagWeightLimitPenalty = Config.Bind("#0_Flag", "FlagWeightLimitPenalty", true, "Penalize weight limits. PC Only");
		CE_FlagKeepHandleMod = Config.Bind("#0_Flag", "FlagKeepHandleMod", true, "Limit the weight of items you can handle. PC Only");
		//CE_FlagKeepLiftMod = Config.Bind("#0_Flag", "FlagKeepLiftMod", true, "Limit the weight of items you can lift. PC Only");
		CE_FlagTutorialRescue = Config.Bind("#0_Flag", "FlagTutorialRescue", true, "Whether to rescue in the tutorial quest.If you dare not proceed with the main quest, please turn it false.");
		
		CE_WeightLimitMulti = Config.Bind("#1_ValueFloat", "WeightLimitMulti", 0.1f, "PC's Weight Limit will be multiplied by this value");

		CE_BaseWeightCanKeepHandle = Config.Bind("#1_ValueInt", "BaseWeightCanKeepHandle", 2000, "Base value of the weight of items that can be handled");
		CE_ModWeightCanKeepHandle = Config.Bind("#1_ValueInt", "ModWeightCanKeepHandle", 10, "Mod value of the weight of items that can be handled");
		CE_BaseWeightCanKeepLift = Config.Bind("#1_ValueInt", "BaseWeightCanKeepLift", 10000, "Base value of the weight of items that can be lifted");
		CE_ModWeightCanKeepLift = Config.Bind("#1_ValueInt", "ModWeightCanKeepLift", 50, "Mod value of the weight of items that can be lifted");

        var harmony = new Harmony("IFWMain");
        harmony.PatchAll();
    }
	
	public static int WeightCanKeepHandle(Chara c){
		int baseV = configBaseWeightCanKeepHandle;
		int modV = configModWeightCanKeepHandle;
		return (c.HasElement(EL_WeightLifting))? baseV + c.elements.Value(EL_WeightLifting) * modV: baseV;
	}
	public static int WeightCanKeepLift(Chara c){
		int baseV = configBaseWeightCanKeepLift;
		int modV = configModWeightCanKeepLift;
		return (c.HasElement(EL_WeightLifting))? baseV + c.elements.Value(EL_WeightLifting) * modV: baseV;
	}
	public static bool IsOnGlobalMap(){
        return (EClass.pc.currentZone.id == "ntyris") ? true : false;
    }
}


	
	[HarmonyPatch]
	public class WeightPatch
	{
		
		[HarmonyPostfix]
		[HarmonyPatch(typeof(Chara), "WeightLimit", MethodType.Getter)]
		public static void WeightLimit_PostPatch(Chara __instance, ref int __result)
		{
            Chara c = __instance;
			if (c.IsPC && c.HasElement(IFWMain.EL_FairyWeak) && IFWMain.configFlagWeightLimitPenalty)
			{
				float rs = (float)(__result) * IFWMain.configWeightLimitMulti;
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
				//debug
				if(actBefore != null){
					//if(actBefore.ToString() != c.ai.ToString())
					//Debug.Log("[IFW}]C:" + c.ai.ToString() + ":before->" + actBefore.ToString());
				} else {
					//Debug.Log("[IFW}]C:" + c.ai.ToString());
				}
				
				
				if(c.ai is GoalManualMove && IFWMain.configFlagKeepHandleMod){
					if(c.held != null){
						//Debug.Log("[IFW] " + c.held.ToString());
						if(c.held.ChildrenAndSelfWeight > IFWMain.WeightCanKeepLift(c)){
							Msg.Say("tooHeavyToEquip", c.held);
							c.DropHeld();
							
						}
					}
				}
				if(c.ai is TaskHarvest  && IFWMain.configFlagKeepHandleMod){
					if(c.held != null){
						//Debug.Log("[IFW] " + c.held.ToString());
						if(c.held.ChildrenAndSelfWeight > IFWMain.WeightCanKeepHandle(c)){
							Msg.Say("tooHeavyToEquip", c.held);
							c.ai.Current.TryCancel(c.held);
							
						}
					}
				}
				//ai is AI_PlayMusic
				if(c.ai is AI_PlayMusic && IFWMain.configFlagKeepHandleMod){
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
			
			if(t.SelfWeight > IFWMain.WeightCanKeepHandle(c) && IFWMain.configFlagKeepHandleMod){
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
			if(IFWMain.configFlagKeepHandleMod && ar != null){
				Chara cc = Act.CC;
				if(!cc.IsPC){return true;}
				Chara tc = Act.CC;
				Thing tool = Act.TOOL;
				if(tc != null && tool != null){
					Debug.Log("[IFW]Ranged : "+ cc.ToString() + "->" + tool.ToString());
					if(tool.SelfWeight > IFWMain.WeightCanKeepHandle(cc) && IFWMain.configFlagKeepHandleMod){
						Debug("[IFW]you lose :" + tool.SelfWeight.ToString() + "vs" + IFWMain.WeightCanKeepHandle(cc).ToString());
						return false;
					}
				}//debug
				
				//Msg.SayRaw("TooHeavy");
				//return false;
			}
			
			return true;
			
		}	
	}
	[HarmonyPatch]
	public class AshPatch
	{
		[HarmonyPostfix]
		[HarmonyPatch(typeof(ThingGen), "_Create")]
		public static void AshExe(string id, int idMat, int lv, Thing __result)
		{
			if(IFWMain.configFlagTutorialRescue && QuestMain.Phase <= 200){
				if(id == "axe"){
					__result.ChangeMaterial(78);//plastic
				}
			}
			
		}
		
	}
	/*
	[HarmonyPatch]
	public class MochiageTest
	{
		[HarmonyPostfix]
		[HarmonyPatch(typeof(Chara), "CanLift")]
		public static void MochiagePatch(bool __result)
		{
			
			if(__instance.IsPC && IFWMain.configFlagKeepHandleMod){
				Debug.Log("[IFW]CanLift:" + c.ChildrenAndSelfWeight.ToString() + "vs" + IFWMain.WeightCanKeepLift(__instance).ToString());

				if(c.ChildrenAndSelfWeight > IFWMain.WeightCanKeepLift(__instance)){
					Debug.Log("[IFW]lose...");
					
				}
				
			}
			
			Debug.Log("[IFW]lose...");
			__result = false;
		}

	}
	*/
}
//----template-----------------------------------------

//Debug.Log("");
/*
	[HarmonyPatch]
	public class NanikaPatch
	{
		[HarmonyPrefix]
		[HarmonyPatch(typeof(Nanika), "Doreka")]
		public static bool NanikaPatch(Doreka __instance)
		{
			
			
		}	
	}

	[HarmonyPatch]
	public class NanikaPatch
	{
		[HarmonyPostfix]
		[HarmonyPatch(typeof(Nanika), "Doreka")]
		public static void NanikaPatch(Doreka __instance)
		{
			
			
		}	
	}
*/
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