using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using EverWind.Gameplay;
using Object=UnityEngine.Object;
public static class FinalProductionSmoke {
 static void Check(bool value,string name){if(!value)throw new InvalidOperationException(name);}
 static async Task Wait(Func<bool> condition,string name){float until=Time.realtimeSinceStartup+12;while(!condition()){if(Time.realtimeSinceStartup>until)throw new TimeoutException(name);await Task.Delay(50);}}
 public static async Task<object> Run(){
  var account=Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText(".utmp/migration/accounts.json"))[0];
  async Task Login(){var ui=Object.FindFirstObjectByType<LoginUI>();ui.userIdField.text=(string)account["user"];ui.passwordField.text=(string)account["password"];ui.loginButton.onClick.Invoke();
   await Wait(()=>GameplayIntegration.Active?.Player!=null && GameplaySession.Current.Transport.Ready && GameplayIntegration.Active.Popups.Inventory!=null,"login + UI initialized");}
  await Login();var game=GameplayIntegration.Active;Check(game.TutorialPhase==0||game.TutorialPhase==1,"original tutorial resumed");
  if(game.TutorialPhase==0){await Task.Delay(5200);game.Tutorial.TransitionStep(TutorialStep.Move);}
  await Wait(()=>game.TutorialPhase==1,"server approved tutorial transition");
  game.Popups.OpenInventory();await Task.Delay(100);Check(game.Popups.IsPopUpOn,"original popup works");game.Popups.CloseCurrentUI();
  // Simulate connection loss rather than asking the UI to leave: the session must return to login safely.
  GameplaySession.Current.Transport.CloseConnection();await Wait(()=>UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="LogIn","disconnect recovery");
  await Login();game=GameplayIntegration.Active;await Wait(()=>game.TutorialPhase==1,"tutorial stage persisted");
  Check(Object.FindObjectsByType<InputManager>(FindObjectsSortMode.None).Length==1,"input manager duplicated after reconnect");
  Check(Object.FindObjectsByType<GameplaySession>(FindObjectsSortMode.None).Length==1,"session duplicated after reconnect");
  GameplaySession.Current.Logout();await Wait(()=>UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="LogIn","saved logout");
  return new{tutorialPersisted=true,disconnectRecovery=true,uiReinitialized=true,scene="LogIn"};
 }
}
