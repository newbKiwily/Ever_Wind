#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using EverWind.Gameplay;
public static class InGameUICharacterPreviewSetup
{
 const string TexturePath="Assets/Scripts/UI/InGame/Prefabs/EquipmentPreview.renderTexture";
 public static void ApplyUI(Transform canvas)
 {
  var frame=canvas.Find("HUD/Vitals/ProfileFrame");if(frame.GetComponent<ProfileFrameRotation>()==null)frame.gameObject.AddComponent<ProfileFrameRotation>();
  var texture=AssetDatabase.LoadAssetAtPath<RenderTexture>(TexturePath);
  if(texture==null){texture=new RenderTexture(1024,1024,24,RenderTextureFormat.ARGB32){name="EquipmentPreview"};AssetDatabase.CreateAsset(texture,TexturePath);}
  var equipment=canvas.Find("Inventory/Equipment");var rect=equipment.Find("EquipmentRawImage") as RectTransform;
  if(rect==null){rect=(RectTransform)new GameObject("EquipmentRawImage",typeof(RectTransform)).transform;rect.SetParent(equipment,false);rect.gameObject.layer=5;}
  rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(650,650);rect.anchoredPosition=new Vector2(-100,0);
  var image=rect.GetComponent<RawImage>()??rect.gameObject.AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;image.color=Color.white;rect.SetAsFirstSibling();
 }
 [MenuItem("Tools/EverWind/Connect inventory character preview")]
 public static string Install()
 {
  if(Application.isPlaying)throw new InvalidOperationException("Edit Mode only");
  var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");if(!scene.IsValid()||!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
  if(scene.isDirty)throw new InvalidOperationException("Save existing changes first");
  var root=scene.GetRootGameObjects().Single(g=>g.name=="InGame UI");
  if(PrefabUtility.IsPartOfPrefabInstance(root))PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);
  ApplyUI(root.transform.Find("Canvas"));
  PrefabUtility.SaveAsPrefabAssetAndConnect(root,"Assets/Scripts/UI/InGame/Prefabs/InGameUI.prefab",InteractionMode.AutomatedAction);
  var main=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GameplayCameraController>(true)).Single();
  var cameraObject=main.PreviewCamera;
  if(cameraObject==null){cameraObject=new GameObject("Equipment Preview Camera");cameraObject.transform.SetParent(main.transform,false);main.PreviewCamera=cameraObject;}
  var camera=cameraObject.GetComponent<Camera>();if(camera==null)camera=cameraObject.AddComponent<Camera>();
  camera.targetTexture=AssetDatabase.LoadAssetAtPath<RenderTexture>(TexturePath);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<6;camera.fieldOfView=38;camera.nearClipPlane=.1f;camera.farClipPlane=30;camera.allowHDR=false;
  cameraObject.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();return "Connected rotating frame, inventory RawImage, transparent RenderTexture and front preview camera";
 }
}
#endif
