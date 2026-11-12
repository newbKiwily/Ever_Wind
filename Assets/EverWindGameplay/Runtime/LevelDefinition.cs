using System;
using UnityEngine;
namespace EverWind.Gameplay
{
[Serializable]
public sealed class LevelDefinition
{
    public int Id;
    public GameObject Visual;
    public Sprite Minimap;
    public Vector3 MinimapPosition, MinimapRotation, Center, Spawn;
    public float MinimapScale = 1, Radius = 100, HeightX, HeightZ, HeightStep = 1;
    public int HeightColumns, HeightRows;
    public float[] Heights = Array.Empty<float>();
    public float Height(float x, float z)
    {
        if (HeightColumns < 2 || HeightRows < 2 || Heights.Length != HeightColumns * HeightRows)
            return Spawn.y;
        float u = Mathf.Clamp((x - HeightX) / HeightStep, 0, HeightColumns - 1),
              v = Mathf.Clamp((z - HeightZ) / HeightStep, 0, HeightRows - 1);
        int ix = Mathf.Min((int)u, HeightColumns - 2), iz = Mathf.Min((int)v, HeightRows - 2);
        u -= ix;
        v -= iz;
        return Mathf.Lerp(
            Mathf.Lerp(Heights[iz * HeightColumns + ix], Heights[iz * HeightColumns + ix + 1], u),
            Mathf.Lerp(Heights[(iz + 1) * HeightColumns + ix], Heights[(iz + 1) * HeightColumns + ix + 1], u),
            v);
    }
}
}
