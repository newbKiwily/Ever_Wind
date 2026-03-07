using UnityEngine;

[CreateAssetMenu]
public class InventoryItem : ScriptableObject
{
    public string ItemName;
    public Sprite ItemImage;
    public bool IsStackable;
    public string ItemInstruction;
    public int IsFromEquipmentUI = 0;

    private void OnEnable()
    {
        ItemName = name;
    }
}