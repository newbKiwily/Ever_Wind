using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class InspectGround {
 public static object Run(){var root=PrefabUtility.LoadPrefabContents("Assets/Scenes/Map1 1.prefab");try {
  Physics.SyncTransforms();var ground=root.GetComponentsInChildren<Collider>(true).Where(c=>c.gameObject.layer==8||c.CompareTag("Ground")).ToArray();
  return new {ground=ground.Select(c=>new{name=c.name,layer=c.gameObject.layer,tag=c.tag,bounds=c.bounds.ToString(),mesh=c is MeshCollider mc?mc.sharedMesh?.name:""}).ToArray(),
   hits=ground.Select(c=>c.Raycast(new Ray(new Vector3(327,400,-23.05f),Vector3.down),out var h,500)?h.point.y:-999).Where(y=>y>0).ToArray(),
   all=root.GetComponentsInChildren<Collider>(true).GroupBy(c=>c.gameObject.layer).Select(g=>new{layer=g.Key,count=g.Count(),examples=g.Take(5).Select(c=>c.name).ToArray()}).ToArray()};
 }finally{PrefabUtility.UnloadPrefabContents(root);}}
}
