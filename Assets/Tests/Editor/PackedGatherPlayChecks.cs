#if UNITY_EDITOR
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using EverWind.Gameplay;
public static class PackedGatherPlayChecks
{
 [MenuItem("Tools/EverWind/Verify packed gathering (Play Mode)")]
 static async void Menu(){try{Debug.Log(await Run());}catch(Exception e){Debug.LogException(e);}}
 public static async Task<string> Run()
 {
  if(!Application.isPlaying)throw new InvalidOperationException("Run in an isolated Play Mode scene");
  var profileHandle=Addressables.LoadAssetAsync<ActionVisualProfile>(AssetDatabase.AssetPathToGUID("Assets/Scripts/GamePlayEdit/Characters/Player/PlayerProfile.asset"));
  var visualHandle=Addressables.LoadAssetAsync<GameObject>(AssetDatabase.AssetPathToGUID("Assets/Scripts/GamePlayEdit/Characters/Player/PlayerVisual.prefab"));
  GameObject root=null;
  try
  {
   await profileHandle.Task;await visualHandle.Task;
   if(profileHandle.Status!=AsyncOperationStatus.Succeeded||visualHandle.Status!=AsyncOperationStatus.Succeeded)throw new Exception("Addressables load failed");
   if(!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(profileHandle.Result)))throw new Exception("This check must load the built bundle, not AssetDatabase");
   root=new GameObject("Packed Gather Test Actor");
   var actor=root.AddComponent<PlayerServerTransmitter>();var model=UnityEngine.Object.Instantiate(visualHandle.Result,root.transform);
   var animator=model.GetComponentInChildren<Animator>();actor.ConfigureVisual(profileHandle.Result,animator);animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
   actor.Receive(new ServerMessage{Kind=EventKind.Spawn,Entity=9,Hp=100,MaxHp=100,Mode=PlayerMode.NonCombat});
   animator.Rebind();animator.Update(0);
   float standing=animator.GetBoneTransform(HumanBodyBones.Hips).position.y;
   actor.Receive(new ServerMessage{Kind=EventKind.ActionStarted,ActionId=100,Code=(ushort)ActionKind.Gather,Definition=1,Mode=PlayerMode.Looting,Value=(float)Time.unscaledTimeAsDouble,Duration=3});
   float minFoot=float.MaxValue,maxFoot=float.MinValue,minHip=float.MaxValue;bool sawGather=false;
   var feet=new[]{animator.GetBoneTransform(HumanBodyBones.LeftFoot),animator.GetBoneTransform(HumanBodyBones.RightFoot)};
   var origin=root.transform.position;
   for(int i=0;i<60;i++)
   {
    await Task.Delay(50);
    sawGather|=animator.GetCurrentAnimatorClipInfo(0).Any(c=>c.clip.name=="Gathering");
    float foot=feet.Min(f=>f.position.y);minFoot=Mathf.Min(minFoot,foot);maxFoot=Mathf.Max(maxFoot,foot);minHip=Mathf.Min(minHip,animator.GetBoneTransform(HumanBodyBones.Hips).position.y);
    if(root.transform.position!=origin)throw new Exception("Gameplay root moved");
   }
   if(!sawGather||maxFoot>.16f||minFoot<0||minHip>standing*.5f)throw new Exception("Packed gather failed: feet="+minFoot+".."+maxFoot+", hips="+standing+" -> "+minHip+", clip="+sawGather);
   actor.Receive(new ServerMessage{Kind=EventKind.ActionEnded,ActionId=100,Mode=PlayerMode.NonCombat});for(int i=0;i<30&&!animator.GetCurrentAnimatorClipInfo(0).Any(c=>c.clip.name=="Success");i++)await Task.Delay(50);
   if(!animator.GetCurrentAnimatorClipInfo(0).Any(c=>c.clip.name=="Success"))throw new Exception("Packed Success clip did not play: clips="+string.Join(",",animator.GetCurrentAnimatorClipInfo(0).Select(c=>c.clip.name))+", success="+animator.GetBool("Success")+", mode="+actor.Mode+", resolved="+profileHandle.Result.ResolveClip("Success").name);
   return "PASS: real Addressables bundle, ActionStarted -> 60 runtime Gather samples -> ActionEnded/Success; feet="+minFoot.ToString("F3")+".."+maxFoot.ToString("F3")+"m, hips="+standing.ToString("F3")+" -> "+minHip.ToString("F3")+"m, root unchanged";
  }
  finally{if(root!=null)UnityEngine.Object.DestroyImmediate(root);if(profileHandle.IsValid())Addressables.Release(profileHandle);if(visualHandle.IsValid())Addressables.Release(visualHandle);}
 }
}
#endif
