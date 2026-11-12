using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using EverWind.Gameplay;
using Object=UnityEngine.Object;

// Run through Unity CLI. Scene/prefab edits always use Editor APIs.
public static class MigrateProductionScenes {
 const string Root="Assets/EverWindGameplay/Generated";
 static readonly CultureInfo Culture=CultureInfo.InvariantCulture;
 static string N(float n)=>n.ToString("R",Culture);
 static bool Ground(Collider collider){var b=collider.bounds;float width=Mathf.Min(b.size.x,b.size.z);
  return collider.gameObject.layer==8 || (collider is MeshCollider && width>3 && b.size.y<Mathf.Max(.6f,width*.12f));}
 static T[] All<T>(UnityEngine.SceneManagement.Scene s) where T:Component =>s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
 static void Strip(GameObject root,bool keepGround=false) {
  foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true).Reverse())if(component!=null)Object.DestroyImmediate(component);
  foreach(var collider in root.GetComponentsInChildren<Collider>(true))if(!keepGround || collider.gameObject.layer!=8)Object.DestroyImmediate(collider);
  foreach(var body in root.GetComponentsInChildren<Rigidbody>(true))Object.DestroyImmediate(body);
  foreach(var animator in root.GetComponentsInChildren<Animator>(true)){animator.runtimeAnimatorController=null;animator.applyRootMotion=false;animator.fireEvents=false;}
 }
 static GameObject Visual(GameObject source,string name,bool place=false) {
  var go=Object.Instantiate(source);go.name=name;Strip(go);
  go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
  if(place){var renderers=go.GetComponentsInChildren<Renderer>();if(renderers.Length>0){var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);go.transform.position-=Vector3.up*bounds.min.y;}}
  var wrapper=new GameObject(name);go.transform.SetParent(wrapper.transform,true);
  var result=PrefabUtility.SaveAsPrefabAsset(wrapper,Root+"/"+name+".prefab");Object.DestroyImmediate(wrapper);return result;
 }
 static void PutAsset(Object value,string path){var old=AssetDatabase.LoadMainAssetAtPath(path);if(old==null)AssetDatabase.CreateAsset(value,path);else {EditorUtility.CopySerialized(value,old);Object.DestroyImmediate(value);}}
 public static object Run() {
  if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode first");
  for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save open scene edits first");
  var catalog=AssetDatabase.LoadAssetAtPath<GameplayCatalog>(Root+"/Catalog.asset");
  if(catalog==null)throw new InvalidOperationException("Existing gameplay catalog required");
  var originalRules=File.ReadAllLines("Shared/Gameplay.rules");
  var rules=new List<string>{"# Production world exported from the original EverWind maps and item/quest assets.","policy 15 10"};
  rules.AddRange(originalRules.Where(l=>l.StartsWith("item ")||l.StartsWith("recipe ")||l.StartsWith("skill ")));
  if(!rules.Any(l=>l.StartsWith("skill 3 ")))rules.Add("skill 3 1.2 0.55 2.5 15 2 0");
  rules.AddRange(new[]{"skillmeta 1 10 0","skillmeta 2 15 0","skillmeta 3 15 3"});
  catalog.Skills=new[]{new GameplayCatalog.SkillEntry{Id=1,Reach=1.2f,Cooldown=.8f},new GameplayCatalog.SkillEntry{Id=2,Reach=1.8f,Cooldown=2},new GameplayCatalog.SkillEntry{Id=3,Reach=2.5f,Cooldown=2}};
  catalog.Quests=AssetDatabase.FindAssets("t:Quest",new[]{"Assets/Scripts/Quest"}).Select(AssetDatabase.GUIDToAssetPath).Select(p=>AssetDatabase.LoadAssetAtPath<Quest>(p)).Where(q=>q!=null).OrderBy(q=>q.QuestId).ToArray();
  var sourceScene=EditorSceneManager.OpenPreviewScene("Assets/Scenes/InGame.unity");
  var mediator=All<ItemMediator>(sourceScene).Single();
  var guide=All<TutorialGuide>(sourceScene).Single();
  rules.Add($"tutorialmove {N(guide.TargetBoxPosition.x)} {N(guide.TargetBoxPosition.y)} {N(guide.TargetBoxPosition.z)} 2");
  var gatherKeys=new Dictionary<Type,int>();foreach(var pair in mediator.FieldItemTable)if(pair.Value!=null && pair.Value.GetComponent<FieldItem>() is FieldItem f)gatherKeys[f.GetType()]=pair.Key;
  var visuals=new Dictionary<int,ItemVisualEntry>();
  foreach(var entry in catalog.Items) {
   int kind=0,slot=0,power=0;float heal=0;
   if(entry.Definition is EquipmentItem equipment){kind=1;slot=(int)equipment.Type;power=equipment.Power;}
   else if(entry.Definition is HPPotion potion){kind=2;heal=potion.HealAmount;}
   else if(entry.Definition is PlaceItem place){kind=3;visuals[entry.Id]=new ItemVisualEntry{Item=entry.Id,Placement=Visual(place.Placement,"Placement"+entry.Id,true)};}
   Type fieldType=entry.Definition.ItemName=="Branch"?typeof(Branch):entry.Definition.ItemName=="Grass"?typeof(Grass):entry.Definition.ItemName=="Rock"?typeof(Rock):null;
   int gatherKey=fieldType!=null && gatherKeys.TryGetValue(fieldType,out var key)?key:0;
   rules.Add($"itemdef {entry.Id} {kind} {slot} {power} {N(heal)} {gatherKey} {Animator.StringToHash(entry.Definition.ItemName)}");
  }
  foreach(var quest in catalog.Quests){foreach(var c in quest.Conditions)rules.Add($"questcondition {quest.QuestId} {(int)c.ConditionType} {(c.TargetId==0?-1:c.TargetId)} {c.RequiredCount}");
   foreach(var reward in quest.Rewards){var item=catalog.Items.FirstOrDefault(e=>e.Definition.ItemName==reward.ItemKey);if(item.Id>0)rules.Add($"questreward {quest.QuestId} {item.Id} {reward.Amount}");}}
  EditorSceneManager.ClosePreviewScene(sourceScene);
  var loginPreview=EditorSceneManager.OpenPreviewScene("Assets/Scenes/LogIn.unity");
  var maps=All<DataCenter>(loginPreview).Single().MapTable.OrderBy(p=>p.Key).ToArray();
  var world=AssetDatabase.LoadAssetAtPath<GameplayWorldCatalog>(Root+"/WorldCatalog.asset");if(world==null){world=ScriptableObject.CreateInstance<GameplayWorldCatalog>();AssetDatabase.CreateAsset(world,Root+"/WorldCatalog.asset");}
  var levels=new List<LevelDefinition>();var summary=new List<object>();
  foreach(var map in maps) {
   int id=map.Key;var data=map.Value;
   var root=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(data.MapPrefab));
   try {
    var level=new LevelDefinition{Id=id,Minimap=data.MinimapImage,MinimapPosition=data.Position,MinimapRotation=data.Rotation,MinimapScale=data.Scale,
     Center=new Vector3(300,0,-80),Radius=110,Spawn=id==0?new Vector3(327,211,-23.05f):new Vector3(298,211,-80),HeightX=190,HeightZ=-190,HeightStep=.5f,HeightColumns=441,HeightRows=441};
    var fields=root.GetComponentsInChildren<FieldItem>(true).OrderBy(f=>f.GetType().Name).ThenBy(f=>f.transform.position.x).ThenBy(f=>f.transform.position.z).ToArray();
    rules.Add($"map {id} {N(level.Radius)}");rules.Add($"spawn {id} {N(level.Spawn.x)} {N(level.Spawn.y)} {N(level.Spawn.z)} {N(level.Center.x)} {N(level.Center.z)}");
    int resourceIndex=0;
    foreach(var field in fields) {
     int itemId=catalog.Items.Single(e=>e.Definition.ItemName==field.GetType().Name).Id;
     int entity=20000+id*1000+resourceIndex++;var p=field.transform.position;
     if(!visuals.TryGetValue(itemId,out var visual))visual=new ItemVisualEntry{Item=itemId};
     if(visual.Resource==null)visual.Resource=Visual(field.gameObject,"Resource"+itemId);
     visuals[itemId]=visual;
     rules.Add($"resource {id} {entity} {N(p.x)} {N(p.z)} 0.25 1 {itemId}");
     rules.Add($"resourcecfg {id} {entity} 5 {(field is Branch?5:10)}");
    }
    foreach(var field in fields)Object.DestroyImmediate(field.gameObject);
    Physics.SyncTransforms();var ground=root.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled && Ground(c)).ToArray();
    foreach(var collider in ground)collider.gameObject.layer=8;
    level.Heights=new float[level.HeightColumns*level.HeightRows];
    for(int z=0;z<level.HeightRows;z++)for(int x=0;x<level.HeightColumns;x++){
     float px=level.HeightX+x*level.HeightStep,pz=level.HeightZ+z*level.HeightStep,height=float.NegativeInfinity;
     var ray=new Ray(new Vector3(px,500,pz),Vector3.down);
     foreach(var collider in ground){var b=collider.bounds;if(px<b.min.x||px>b.max.x||pz<b.min.z||pz>b.max.z)continue;if(collider.Raycast(ray,out var hit,1000))height=Mathf.Max(height,hit.point.y);}
     level.Heights[z*level.HeightColumns+x]=float.IsNegativeInfinity(height)?209.4f:height;
    }
    level.Spawn.y=level.Height(level.Spawn.x,level.Spawn.z);
    string heightName="Map"+id+".heights";using(var writer=new BinaryWriter(File.Create("Shared/"+heightName)))foreach(float height in level.Heights)writer.Write(height);
    rules.Add($"surface {id} {N(level.HeightX)} {N(level.HeightZ)} {N(level.HeightStep)} {level.HeightColumns} {level.HeightRows} {heightName}");
    int boxes=0;
    foreach(var collider in root.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled && !c.isTrigger && c.gameObject.layer!=8)){
     var b=collider.bounds;if(b.size.y<.6f||b.size.x<.08f||b.size.z<.08f)continue;
     // The enclosing sky mesh is hollow, and its bounds must never become a solid wall.
     if(b.size.x>level.Radius*2 && b.size.z>level.Radius*2)continue;
     if(b.max.y<level.Height(b.center.x,b.center.z)+.05f)continue;
     if(new Vector2(b.center.x-level.Center.x,b.center.z-level.Center.z).magnitude>level.Radius+Mathf.Max(b.extents.x,b.extents.z))continue;
     rules.Add($"box {id} {30000+id*1000+boxes++} {N(b.center.x)} {N(b.center.z)} {N(b.extents.x)} {N(b.extents.z)}");
    }
    // Original monster table controls visual type, while the server owns wandering/HP/refill.
    var center=id==0?new Vector3(295.75f,0,-82):new Vector3(275.18f,0,-120.35f);float radius=id==0?15:10;
    for(int n=0;n<5;n++){float angle=n*Mathf.PI*2/5;var p=center+new Vector3(Mathf.Cos(angle)*radius*.6f,0,Mathf.Sin(angle)*radius*.6f);
     rules.Add($"monster {id} {10000+id*1000+n} {n%2} {N(p.x)} {N(p.z)} {(n%2==0?"0.7 2.5":"0.6 2")} 100");}
    Strip(root,true);root.name="Map"+id+"Visual";level.Visual=PrefabUtility.SaveAsPrefabAsset(root,Root+"/Map"+id+"Visual.prefab");levels.Add(level);
    summary.Add(new{id,resources=resourceIndex,boxes,spawn=level.Spawn.ToString(),heightSamples=level.Heights.Length});
   }finally{PrefabUtility.UnloadPrefabContents(root);}
  }
  EditorSceneManager.ClosePreviewScene(loginPreview);
  world.Levels=levels.ToArray();catalog.Visuals=visuals.Values.OrderBy(v=>v.Item).ToArray();
  EditorUtility.SetDirty(world);EditorUtility.SetDirty(catalog);
  File.WriteAllLines("Shared/Gameplay.rules",rules,new UTF8Encoding(false));
  AddThirdAttack();
  var ingame=EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity",OpenSceneMode.Single);
  foreach(var root in ingame.GetRootGameObjects())if(PrefabUtility.IsPartOfPrefabInstance(root))PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
  foreach(var prior in All<GameplayIntegration>(ingame))Object.DestroyImmediate(prior.gameObject);
  var gameplay=new GameObject("EverWind Gameplay");var registry=gameplay.AddComponent<ServerTransceiverManager>();registry.Catalog=catalog;registry.WorldCatalog=world;
  registry.PlayerVisual=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/PlayerVisual.prefab");registry.PlayerProfile=AssetDatabase.LoadAssetAtPath<ActionVisualProfile>(Root+"/PlayerProfile.asset");
  registry.MonsterVisuals=new[]{AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/SpiderVisual.prefab"),AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/SkeletonVisual.prefab")};
  registry.MonsterProfiles=new[]{AssetDatabase.LoadAssetAtPath<ActionVisualProfile>(Root+"/SpiderProfile.asset"),AssetDatabase.LoadAssetAtPath<ActionVisualProfile>(Root+"/SkeletonProfile.asset")};
  var camera=All<Camera>(ingame).Single(c=>c.CompareTag("MainCamera"));
  if(camera.GetComponent<CameraMoving>()!=null){EverWind.Gameplay.Editor.LegacyControlsSetup.Configure(registry,camera);Object.DestroyImmediate(camera.GetComponent<CameraMoving>());}
  else {camera.GetComponent<GameplayCameraController>().Registry=registry;registry.TargetIndicatorVisual=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/TargetIndicatorVisual.prefab");}
  camera.GetComponent<GameplayCameraController>().PreviewCamera=All<Camera>(ingame).FirstOrDefault(c=>c.name=="PreviewCamera")?.gameObject;
  var integration=gameplay.AddComponent<GameplayIntegration>();integration.Registry=registry;
  integration.Input=All<InputManager>(ingame).Single();integration.Popups=All<PopUpUIManager>(ingame).Single();integration.Quests=All<QuestManager>(ingame).Single();integration.Tutorial=All<TutorialGuide>(ingame).Single();
  integration.Display=All<DisplayUIManager>(ingame).Single();integration.EnemyUI=All<EnemyHpUIManager>(ingame).Single();integration.Effects=All<EffectManager>(ingame).Single();
  SanitizeEffects(integration.Effects);
  foreach(var old in All<MonoBehaviour>(ingame).Where(c=>c is EnemySpawner || c is OtherPlayerManager || c is WorldLoader || c is SceneLoader || c is global::NetworkClient || c is Player || c is Enemy || c is FieldItem).ToArray())if(old!=null)Object.DestroyImmediate(old);
  var popups=new SerializedObject(integration.Popups);popups.FindProperty("_cameraMoving").objectReferenceValue=null;popups.ApplyModifiedPropertiesWithoutUndo();
  EditorSceneManager.SaveScene(ingame);
  var login=EditorSceneManager.OpenScene("Assets/Scenes/LogIn.unity",OpenSceneMode.Single);
  foreach(var old in All<MonoBehaviour>(login).Where(c=>c is global::NetworkClient || c is SceneLoader || c is WorldLoader || c is UnityMainThreadDispatcher).ToArray())if(old!=null)Object.DestroyImmediate(old);
  var session=All<GameplaySession>(login).FirstOrDefault();if(session==null)session=new GameObject("EverWind Session").AddComponent<GameplaySession>();
  session.Transport=session.GetComponent<EverWind.Gameplay.NetworkClient>()??session.gameObject.AddComponent<EverWind.Gameplay.NetworkClient>();session.Catalog=catalog;session.WorldCatalog=world;
  EditorSceneManager.SaveScene(login);
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/LogIn.unity",true),new EditorBuildSettingsScene("Assets/Scenes/InGame.unity",true)};
  AssetDatabase.SaveAssets();AssetDatabase.Refresh();
  // Delete the superseded verification scene only after both production scenes have been saved.
  AssetDatabase.DeleteAsset(Root+"/GameplayVerification.unity");
  File.WriteAllText(".utmp/migration/production-export.json",Newtonsoft.Json.JsonConvert.SerializeObject(summary,Newtonsoft.Json.Formatting.Indented));
  return new{scenes=new[]{"LogIn","InGame"},levels=summary,items=catalog.Items.Length,recipes=catalog.Recipes.Length,quests=catalog.Quests.Length};
 }
 static void AddThirdAttack(){
  var profile=AssetDatabase.LoadAssetAtPath<ActionVisualProfile>(Root+"/PlayerProfile.asset");var controller=(AnimatorController)profile.Controller;
  var source=AssetDatabase.LoadAssetAtPath<AnimationSet>("Assets/Scripts/States/PlayerAniset.asset");var clean=Object.Instantiate(source.Attack3);clean.name="Player_Attack3";AnimationUtility.SetAnimationEvents(clean,Array.Empty<AnimationEvent>());
  PutAsset(clean,Root+"/Clips/Player_Attack3.anim");var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/Clips/Player_Attack3.anim");
  var state=controller.layers[0].stateMachine.states.FirstOrDefault(s=>s.state.name=="Attack3").state??controller.layers[0].stateMachine.AddState("Attack3");state.motion=clip;
  profile.Attacks=new[]{"Attack1","Attack2","Attack3"};profile.Clips=profile.Clips.Where(c=>c.State!="Attack3").Concat(new[]{new ActionVisualProfile.ClipEntry{State="Attack3",Clip=clip}}).ToArray();EditorUtility.SetDirty(profile);EditorUtility.SetDirty(controller);
 }
 static void SanitizeEffects(EffectManager effects){var so=new SerializedObject(effects);var table=so.FindProperty("_particleTable");
  // SerializedDictionary stores key/value entries; follow each ParticleSystem reference without coupling to its storage layout.
  var prop=table.Copy();var end=table.GetEndProperty();bool enter=true;
  while(prop.Next(enter)&&!SerializedProperty.EqualContents(prop,end)){enter=true;if(prop.propertyType!=SerializedPropertyType.ObjectReference||!(prop.objectReferenceValue is ParticleSystem particle))continue;
   var prefab=Visual(particle.gameObject,"Effect_"+particle.name);prop.objectReferenceValue=prefab.GetComponentInChildren<ParticleSystem>();}
  so.ApplyModifiedPropertiesWithoutUndo();
 }
}

