#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverWind.Gameplay;
public static class GatherGroundingChecks
{
 [MenuItem("Tools/EverWind/Verify gathering foot grounding")]
 public static string Run()
 {
  if(Application.isPlaying)throw new InvalidOperationException("Edit Mode only");
  var scene=EditorSceneManager.NewPreviewScene();
  try
  {
   var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/GamePlayEdit/Characters/Player/PlayerVisual.prefab"));
   UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(model,scene);
   var animator=model.GetComponentInChildren<Animator>();
   animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<ActionVisualProfile>("Assets/Scripts/GamePlayEdit/Characters/Player/PlayerProfile.asset").Controller;
   animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();animator.Update(0);
   var feet=new[]{animator.GetBoneTransform(HumanBodyBones.LeftFoot),animator.GetBoneTransform(HumanBodyBones.RightFoot)};
   var hips=animator.GetBoneTransform(HumanBodyBones.Hips);
   animator.Play("Idle",0,0);animator.Update(0);float standing=hips.position.y;
   float minFoot=float.MaxValue,maxFoot=float.MinValue,minHip=float.MaxValue;
   var root=model.transform.position;
   for(int i=0;i<60;i++)
   {
    animator.Play("Gather",0,i/60f);animator.Update(0);
    float foot=feet.Min(t=>t.position.y);minFoot=Mathf.Min(minFoot,foot);maxFoot=Mathf.Max(maxFoot,foot);minHip=Mathf.Min(minHip,hips.position.y);
    if(model.transform.position!=root)throw new Exception("Gather moved gameplay root");
   }
   if(maxFoot>.15f||minFoot<0||maxFoot-minFoot>.05f)throw new Exception("Gather feet are not grounded: "+minFoot+".."+maxFoot);
   if(minHip>standing*.5f)throw new Exception("Gather did not lower the hips");
   var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/Scripts/GamePlayEdit/Animation/Player/Clips/Gathering.fbx");
   if(!importer.clipAnimations.All(c=>c.lockRootHeightY))throw new Exception("Y motion must be baked into pose");
   return "PASS (source asset only): 60 Gather samples, feet="+minFoot.ToString("F3")+".."+maxFoot.ToString("F3")+"m, hips="+standing.ToString("F3")+" -> "+minHip.ToString("F3")+"m, gameplay root unchanged";
  }
  finally{EditorSceneManager.ClosePreviewScene(scene);}
 }
}
#endif
