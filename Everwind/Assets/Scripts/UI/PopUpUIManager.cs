using UnityEngine;

public class PopUpUIManager : SingletonBase<PopUpUIManager>
{
    public override bool IsPersistent => false;

    public Inventory Inventory { get; private set; }
    public CraftUI CraftUI { get; private set; }
    public ItemTooltip ItemTooltip { get; private set; }
    public DeadUI DeadUI { get; private set; }

    public EquipmentUI EquipmentUI { get; private set; }
    public QuestUI QuestUI { get; private set; }

    private GameObject _currentUI;

    public bool IsPopUpOn;
    [SerializeField]
    private CameraMoving _cameraMoving;

    protected override void Awake()
    {
        Priority = 40;
        base.Awake();
    }

    private void OnEnable()
    {
        UIEvents.OnDeadUiOpenRequested += PopUpDeadUI;
        UIEvents.OnDeadUiCloseRequested += CloseDeadUI;
    }

    private void OnDisable()
    {
        UIEvents.OnDeadUiOpenRequested -= PopUpDeadUI;
        UIEvents.OnDeadUiCloseRequested -= CloseDeadUI;
    }

    public override void Init()
    {
        IsPopUpOn = false;
        UIEvents.SetPopupOpenState(IsPopUpOn);

        _cameraMoving.OffPreviCam();

        Inventory = GetComponentInChildren<Inventory>();
        Inventory.Init();

        CraftUI = GetComponentInChildren<CraftUI>();
        CraftUI.Init(this);

        ItemTooltip = this.GetComponent<ItemTooltip>();
        ItemTooltip.Init();

        DeadUI = GetComponentInChildren<DeadUI>();
        DeadUI.Init();

        EquipmentUI = GetComponentInChildren<EquipmentUI>();
        EquipmentUI.Init();

        QuestUI = GetComponentInChildren<QuestUI>();
        QuestUI.Init();

        Inventory.gameObject.SetActive(false);
        CraftUI.gameObject.SetActive(false);
        DeadUI.gameObject.SetActive(false);
        EquipmentUI.gameObject.SetActive(false);
        QuestUI.gameObject.SetActive(true);
    }

    public void PopUpDeadUI()
    {
        DeadUI.ShowDeadUI();
        IsPopUpOn = true;
        UIEvents.SetPopupOpenState(IsPopUpOn);   
        _currentUI = DeadUI.gameObject;
        return;
    }

    public void CloseDeadUI()
    {
        DeadUI.gameObject.SetActive(false);
        IsPopUpOn = false;
        UIEvents.SetPopupOpenState(IsPopUpOn);
        _currentUI = null;
        return;
    }

    public void OpenInventory()
    {
        if (IsPopUpOn) return;

        IsPopUpOn = true;
        UIEvents.SetPopupOpenState(IsPopUpOn);
        Inventory.gameObject.SetActive(true);
        _currentUI = Inventory.gameObject;
        Inventory.UpdateInventory();
        _cameraMoving.OnPreviewCam();
        ItemTooltip.gameObject.SetActive(true);
    }

    public void OpenCraftUI()
    {
        if (IsPopUpOn) return;

        IsPopUpOn = true;
        UIEvents.SetPopupOpenState(IsPopUpOn);
        CraftUI.gameObject.SetActive(true);
        _currentUI = CraftUI.gameObject;
        CraftUI.FlushCraftZone();
        ItemTooltip.gameObject.SetActive(true);
    }

    public void OnOffQuestUI()
    {
        if (QuestUI.gameObject.activeSelf)
        {
            QuestUI.gameObject.SetActive(false);
            return;
        }
        QuestUI.gameObject.SetActive(true);
        QuestUI.RefreshQuestContents();
    }

    public void CloseCurrentUI()
    {
        if (_currentUI == null) return;

        _currentUI.gameObject.SetActive(false);
        _currentUI = null;
        IsPopUpOn = false;
        UIEvents.SetPopupOpenState(IsPopUpOn);
        ItemTooltip.tooltipWindow.SetActive(false);
        _cameraMoving.OffPreviCam();
    }

    private void Update()
    {
        if (InputManager.Instance.GetInventoryKeyDown())
            OpenInventory();
        if (InputManager.Instance.GetCraftUIKeyDown())
            OpenCraftUI();
        if (InputManager.Instance.GetUICloseKeyDown())
            CloseCurrentUI();
        if (InputManager.Instance.GetQuestUIKeyDown())
            OnOffQuestUI();
    }
}

