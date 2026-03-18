using AYellowpaper.SerializedCollections;
using System;
using UnityEngine;

public class EquipmentUI : MonoBehaviour
{
    [SerializeField]
    private SerializedDictionary<EquipmentItem.EquipmentType, EquipmentSlot> _equipmentSlots = new SerializedDictionary<EquipmentItem.EquipmentType, EquipmentSlot>();
    [SerializeField]
    private PlayerStatManager _statManager;

    public static event Action EquipTClear;

    public void Init()
    {
        var equipItems = SingletonManager.Instance.GetSingleton<DataCenter>().LoadEquipItems;
        var itemMediator = SingletonManager.Instance.GetSingleton<ItemMediator>();

        _statManager = SingletonManager.Instance.GetSingleton<WorldLoader>().InstancedPlayer.GetComponent<Player>().GetPlayerStatManager();

        while (equipItems.Count > 0)
        {
            var key = equipItems.Dequeue();
            var item = itemMediator.GetItemInfo(key);
            if (item is EquipmentItem equipItem)
            {
                this.Equip(equipItem);
            }
        }
    }

    public void Equip(EquipmentItem item)
    {
        if (_equipmentSlots.TryGetValue(item.Type, out var equipmentSlot))
        {
            equipmentSlot.Initialize(item, this);

            switch (item.Type)
            {
                case EquipmentItem.EquipmentType.Boots:
                case EquipmentItem.EquipmentType.Gloves:
                case EquipmentItem.EquipmentType.FullBody:
                case EquipmentItem.EquipmentType.Helmet:
                    _statManager.SetDefencePower(_statManager.GetDefencePower() + item.Power);
                    break;
                case EquipmentItem.EquipmentType.Weapon:
                    _statManager.SetAttackPower(_statManager.GetAttackPower() + item.Power);
                    break;
            }

            EquipTClear?.Invoke();
        }
    }

    public void ClearUI(EquipmentSlot slot)
    {   
   
        slot.Clear();
    }

    public void Unequip(EquipmentSlot slot)
    {
        if (slot == null || slot.GetInventoryItem() == null) return;

        EquipmentItem equippedItem = slot.GetInventoryItem() as EquipmentItem;
        if (equippedItem == null) return;

        switch (equippedItem.Type)
        {
            case EquipmentItem.EquipmentType.Helmet:
            case EquipmentItem.EquipmentType.Boots:
            case EquipmentItem.EquipmentType.Gloves:
            case EquipmentItem.EquipmentType.FullBody:
                _statManager.SetDefencePower(_statManager.GetDefencePower() - equippedItem.Power);
                break;
            case EquipmentItem.EquipmentType.Weapon:
                _statManager.SetAttackPower(_statManager.GetAttackPower() - equippedItem.Power);
                break;
        }

        ClearUI(slot);
        SingletonManager.Instance.GetSingleton<PopUpUIManager>().Inventory.PutItem(equippedItem);
    }

    public void UnequipAll()
    {
        foreach (var slot in _equipmentSlots.Values)
        {
            if (slot != null && slot.GetInventoryItem() != null)
            {
                slot.GetInventoryItem().IsFromEquipmentUI = 1;
                Unequip(slot);
            }
        }
    }
}