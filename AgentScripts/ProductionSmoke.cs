using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using EverWind.Gameplay;
using Object=UnityEngine.Object;
public static class ProductionSmoke {
 static void Check(bool value,string label){if(!value)throw new InvalidOperationException(label);}
 static async Task Wait(Func<bool> condition,string label,float timeout=12){float until=Time.realtimeSinceStartup+timeout;while(!condition()){if(Time.realtimeSinceStartup>until)throw new TimeoutException(label);await Task.Delay(50);}}
 public static async Task<object> Login(){
  var account=Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText(".utmp/migration/accounts.json"))[0];
  int first=Time.frameCount;await Task.Delay(200);Check(Time.frameCount>first,"Player loop frozen");
  var ui=Object.FindFirstObjectByType<LoginUI>();ui.userIdField.text=(string)account["user"];ui.passwordField.text=(string)account["password"];ui.loginButton.onClick.Invoke();
  await Wait(()=>GameplayIntegration.Active!=null && GameplayIntegration.Active.Player!=null && GameplaySession.Current.Transport.Ready,"original login -> InGame");
  await Task.Delay(300);var integration=GameplayIntegration.Active;
  Check(integration.Popups.Inventory!=null,"Inventory initialization");Check(integration.Quests.ActiveQuests.Count==3,"Original quests");Check(integration.TutorialPhase==0,"Camera tutorial state");
  Check(Object.FindObjectsByType<Player>(FindObjectsSortMode.None).Length==0,"Legacy Player gameplay accidentally active");
  Check(Object.FindObjectsByType<FieldItem>(FindObjectsSortMode.None).Length==0,"Legacy gather script accidentally active");
  Check(Object.FindObjectsByType<global::NetworkClient>(FindObjectsSortMode.None).Length==0,"Legacy network accidentally active");
  integration.Popups.OpenInventory();await Task.Delay(100);Check(integration.Popups.IsPopUpOn,"Original inventory popup");integration.Popups.CloseCurrentUI();
  return new{scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,frames=Time.frameCount-first,map=integration.Registry.CurrentMap,position=integration.Player.transform.position.ToString(),
   resources=integration.Registry.Entities.Count(a=>a.Kind==EntityKind.Resource),monsters=integration.Registry.Entities.Count(a=>a.Kind==EntityKind.Monster),quests=integration.Quests.ActiveQuests.Count,tutorial=integration.TutorialPhase};
 }
 public static async Task<object> Features(){
  var account=Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText(".utmp/migration/accounts.json"))[1];
  var ui=Object.FindFirstObjectByType<LoginUI>();ui.userIdField.text=(string)account["user"];ui.passwordField.text=(string)account["password"];ui.loginButton.onClick.Invoke();
  await Wait(()=>GameplayIntegration.Active?.Player!=null && GameplaySession.Current.Transport.Ready,"second account login");await Task.Delay(250);
  var game=GameplayIntegration.Active;var inv=game.Popups.Inventory;var catalog=game.Catalog;game.Popups.OpenInventory();
  var sword=(EquipmentItem)catalog.Item(11);Check(inv.GetItemCount(sword)==1,"DB inventory load");
  var slot=inv.GetComponentsInChildren<InventorySlot>(true).Single(s=>s.GetInventoryItem()==sword);inv.UseItem(slot);
  await Wait(()=>game.Equipment.TryGetValue(1003,out var item)&&item==11,"server equipment response");
  await Wait(()=>inv.GetItemCount(sword)==0,"equipped item removed from inventory");
  float hp=game.Player.Hp;var potion=(HPPotion)catalog.Item(12);inv.UseItem(inv.GetComponentsInChildren<InventorySlot>(true).Single(s=>s.GetInventoryItem()==potion));
  await Wait(()=>game.Player.Hp>hp,"server potion heal");Check(Mathf.Approximately(game.Player.Hp,Mathf.Min(game.Player.MaxHp,hp+potion.HealAmount)),"potion amount");
  int placements=game.Registry.Entities.Count(a=>a.Kind==EntityKind.Placement);inv.UseItem(inv.GetComponentsInChildren<InventorySlot>(true).Single(s=>s.GetInventoryItem()==catalog.Item(3)));
  await Wait(()=>game.Registry.Entities.Count(a=>a.Kind==EntityKind.Placement)==placements+1,"server placement");
  game.Popups.CloseCurrentUI();game.Popups.OpenCraftUI();
  int branches=inv.GetItemCount(catalog.Item(1));var recipe=catalog.Recipes.Single(r=>r.Id==2).Definition;game.Craft(recipe);
  await Wait(()=>inv.GetItemCount(catalog.Item(4))==1,"original recipe server craft",10);Check(inv.GetItemCount(catalog.Item(1))==branches-2,"craft input consumed once");
  game.Popups.CloseCurrentUI();game.Unequip(EquipmentItem.EquipmentType.Weapon);
  await Wait(()=>!game.Equipment.ContainsKey(1003)&&inv.GetItemCount(sword)==1,"server unequip");
  // Original text pool and effect pool receive exactly one presentation event per server impact.
  game.ShowDamage(game.Player,new ServerMessage{Epoch=game.Registry.Transport.Epoch,Entity=game.Player.EntityId,ActionId=999999,Value=12});
  await Task.Delay(100);
  var result=new{items=game.Registry.Inventory.Count,hp=game.Player.Hp,craftOutput=inv.GetItemCount(catalog.Item(4)),placements=game.Registry.Entities.Count(a=>a.Kind==EntityKind.Placement),quests=game.Quests.ActiveQuests.Count};
  GameplaySession.Current.Logout();await Wait(()=>UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="LogIn","saved logout");
  return result;
 }
 public static async Task<object> World(){
  var accounts=Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText(".utmp/migration/accounts.json"));var account=accounts[1];
  var ui=Object.FindFirstObjectByType<LoginUI>();ui.userIdField.text=(string)account["user"];ui.passwordField.text=(string)account["password"];ui.loginButton.onClick.Invoke();
  await Wait(()=>GameplayIntegration.Active?.Player!=null&&GameplaySession.Current.Transport.Ready,"relogin");await Task.Delay(200);
  var game=GameplayIntegration.Active;var registry=game.Registry;var control=game.Player.GetComponent<PlayerActionController>();
  await Wait(()=>game.Popups.Inventory.GetItemCount(game.Catalog.Item(4))==1,"crafted output persisted");Check(game.Player.Hp>0,"live test account");
  var resource=registry.Entities.Where(a=>a.Kind==EntityKind.Resource&&a.Stock>0).OrderBy(a=>(a.transform.position-game.Player.transform.position).sqrMagnitude).First();
  int item=resource.name.StartsWith("Resource")?2:0;int before=registry.Inventory.TryGetValue(item,out int count)?count:0;
  // Drive the same controller that the original Space input now calls.
  var starting=game.Player.transform.position;control.RequestGather();
  await Wait(()=>resource.Stock==0,"approach + gather",15);Check((game.Player.transform.position-starting).sqrMagnitude>.01f,"automatic approach moved");
  await Wait(()=>resource.Stock>0,"client timer validated refill",13);
  control.RequestGather();await Wait(()=>resource.Stock==0,"second gather",8);
  await Wait(()=>game.Quests.ActiveQuests.Any(q=>q.Quest.QuestId==101&&q.IsCompleted),"server gather quest");
  game.Quests.ClaimReward(101);await Wait(()=>game.Quests.ActiveQuests.Any(q=>q.Quest.QuestId==101&&q.RewardClaimed),"quest reward response");
  var beforeMap=registry.CurrentMap;game.Send(new Request{Kind=RequestKind.ChangeMap,Definition=1});
  await Wait(()=>registry.CurrentMap==1&&registry.LocalActor!=null&&GameplaySession.Current.Transport.Ready,"original second map");
  Check(registry.Entities.Count(a=>a.Kind==EntityKind.Resource)==23,"second map resources");
  var result=new{approached=true,refilled=true,questClaimed=true,map=registry.CurrentMap,resources=registry.Entities.Count(a=>a.Kind==EntityKind.Resource),position=registry.LocalActor.transform.position.ToString()};
  GameplaySession.Current.Logout();await Wait(()=>UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="LogIn","map saved logout");return result;
 }
}
