using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class AuditLegacyLevels {
    public static string Run() {
        var list=new System.Collections.Generic.List<object>();
        foreach(var path in new[]{"Assets/Scenes/Map1 1.prefab","Assets/Scenes/Map1 2.prefab"}) {
            var root=PrefabUtility.LoadPrefabContents(path);
            try {list.Add(new {path,components=root.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c!=null).GroupBy(c=>c.GetType().FullName).Select(g=>new{type=g.Key,count=g.Count()}).ToArray(),
                resources=root.GetComponentsInChildren<FieldItem>(true).Select(f=>new{type=f.GetType().Name,name=f.name,p=f.transform.position.ToString("F2")}).ToArray(),
                colliders=root.GetComponentsInChildren<Collider>(true).GroupBy(c=>c.GetType().Name).Select(g=>new{type=g.Key,count=g.Count()}).ToArray(),
                terrains=root.GetComponentsInChildren<Terrain>(true).Select(t=>new{position=t.transform.position.ToString(),size=t.terrainData.size.ToString()}).ToArray()
            });}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        File.WriteAllText(".utmp/migration/legacy-levels.json",Newtonsoft.Json.JsonConvert.SerializeObject(list,Newtonsoft.Json.Formatting.Indented));
        return "Saved original level component and resource inventory";
    }
}
