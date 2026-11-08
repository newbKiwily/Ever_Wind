using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : SingletonBase<QuestManager>
{
    [System.Serializable]
    public class QuestProgressData
    {
        public Quest Quest;
        public List<int> CurrentCounts = new();
        public bool IsCompleted;
        public bool RewardClaimed;
    }

    [SerializeField]
    private SerializedDictionary<int, Quest> _questTable = new SerializedDictionary<int, Quest>();

    [SerializeField]
    private List<QuestProgressData> _activeQuests = new();

    public override bool IsPersistent => false;
    public IReadOnlyList<QuestProgressData> ActiveQuests => _activeQuests;
    private bool _isInitializing;

    protected override void Awake()
    {
        Priority = 40;
        base.Awake();
    }

    public override void Init()
    {
        SubscribeEvents();
        SyncQuestTable();
        LoadQuestProgress();

        UIEvents.EvQuestProgressChanged();
    }

    protected override void OnDestroy()
    {
        UnsubscribeEvents();
        base.OnDestroy();
    }

    private void SubscribeEvents()
    {
        UnsubscribeEvents();
        PlayEvents.OnCombatEnemyKilled += HandleCombatEnemyKilled;
        PlayEvents.OnGatherCompleted += HandleGatherCompleted;
        PlayEvents.OnCraftCompleted += HandleCraftCompleted;
    }

    private void UnsubscribeEvents()
    {
        PlayEvents.OnCombatEnemyKilled -= HandleCombatEnemyKilled;
        PlayEvents.OnGatherCompleted -= HandleGatherCompleted;
        PlayEvents.OnCraftCompleted -= HandleCraftCompleted;
    }

    public Quest GetQuest(int questId)
    {
        if (_questTable.TryGetValue(questId, out Quest quest))
            return quest;

        return null;
    }

    public void AcceptQuest(Quest quest)
    {
        if (quest == null || FindQuestProgress(quest.QuestId) != null)
            return;

        QuestProgressData progressData = new QuestProgressData
        {
            Quest = quest
        };

        for (int i = 0; i < quest.Conditions.Count; i++)
        {
            progressData.CurrentCounts.Add(0);
        }

        _activeQuests.Add(progressData);

        if (!_isInitializing)
            UIEvents.EvQuestProgressChanged();
    }

    public List<DataCenter.QuestLoadData> GetSaveData()
    {
        List<DataCenter.QuestLoadData> saveData = new List<DataCenter.QuestLoadData>(_activeQuests.Count);

        for (int i = 0; i < _activeQuests.Count; i++)
        {
            QuestProgressData questProgress = _activeQuests[i];
            if (questProgress.Quest == null)
                continue;

            DataCenter.QuestLoadData data = new DataCenter.QuestLoadData
            {
                QuestId = questProgress.Quest.QuestId,
                IsCompleted = questProgress.IsCompleted,
                RewardClaimed = questProgress.RewardClaimed,
                CurrentCounts = new List<int>(questProgress.CurrentCounts)
            };

            saveData.Add(data);
        }

        return saveData;
    }

    public int GetConditionProgress(int questId, int conditionIndex)
    {
        QuestProgressData questProgress = FindQuestProgress(questId);
        if (questProgress == null)
            return 0;

        if (conditionIndex < 0 || conditionIndex >= questProgress.CurrentCounts.Count)
            return 0;

        return questProgress.CurrentCounts[conditionIndex];
    }

    public bool ClaimReward(int questId)
    {
        QuestProgressData questProgress = FindQuestProgress(questId);
        if (questProgress == null || questProgress.Quest == null)
            return false;

        if (!questProgress.IsCompleted || questProgress.RewardClaimed)
            return false;

        ItemMediator itemMediator = SingletonManager.Instance.GetSingleton<ItemMediator>();
        if (itemMediator == null)
            return false;

        for (int i = 0; i < questProgress.Quest.Rewards.Count; i++)
        {
            Quest.QuestReward reward = questProgress.Quest.Rewards[i];
            if (string.IsNullOrWhiteSpace(reward.ItemKey) || reward.Amount <= 0)
                continue;

            InventoryItem rewardItem = itemMediator.GetItemInfo(reward.ItemKey);
            if (rewardItem == null)
            {
                Debug.LogWarning($"Quest reward item not found: {reward.ItemKey}");
                continue;
            }

            for (int j = 0; j < reward.Amount; j++)
            {
                itemMediator.Mediation(reward.ItemKey);
            }
        }

        questProgress.RewardClaimed = true;
        UIEvents.EvQuestProgressChanged();
        return true;
    }

    private void SyncQuestTable()
    {
        List<Quest> quests = new List<Quest>(_questTable.Values);
        _questTable.Clear();

        for (int i = 0; i < quests.Count; i++)
        {
            Quest quest = quests[i];
            if (quest == null)
                continue;

            if (_questTable.ContainsKey(quest.QuestId))
            {
                Debug.LogWarning($"Duplicate QuestId detected while syncing quest table: {quest.QuestId}. The latest quest asset will overwrite the previous entry.");
            }

            _questTable[quest.QuestId] = quest;
        }
    }

    private void LoadQuestProgress()
    {
        _activeQuests.Clear();
        _isInitializing = true;

        DataCenter dataCenter = SingletonManager.Instance.GetSingleton<DataCenter>();
        if (dataCenter != null && dataCenter.LoadQuests.Count > 0)
        {
            for (int i = 0; i < dataCenter.LoadQuests.Count; i++)
            {
                DataCenter.QuestLoadData loadData = dataCenter.LoadQuests[i];
                if (!_questTable.TryGetValue(loadData.QuestId, out Quest quest) || quest == null)
                    continue;

                QuestProgressData progressData = new QuestProgressData
                {
                    Quest = quest,
                    IsCompleted = loadData.IsCompleted,
                    RewardClaimed = loadData.RewardClaimed,
                    CurrentCounts = new List<int>()
                };

                int conditionCount = quest.Conditions.Count;
                for (int j = 0; j < conditionCount; j++)
                {
                    int currentCount = j < loadData.CurrentCounts.Count ? loadData.CurrentCounts[j] : 0;
                    progressData.CurrentCounts.Add(currentCount);
                }

                _activeQuests.Add(progressData);
            }

            dataCenter.LoadQuests.Clear();
        }
        else
        {
            foreach (Quest quest in _questTable.Values)
            {
                AcceptQuest(quest);
            }
        }

        _isInitializing = false;
    }

    private void HandleCombatEnemyKilled(int targetId, int amount)
    {
        if (targetId < 0)
        {
            Debug.LogWarning("Quest progress skipped: combat target ID could not be resolved.");
            return;
        }

        ApplyProgress(Quest.QuestConditionType.Combat, targetId, amount);
    }

    private void HandleGatherCompleted(int targetId, int amount)
    {
        if (targetId < 0)
        {
            Debug.LogWarning("Quest progress skipped: gather target ID could not be resolved.");
            return;
        }

        ApplyProgress(Quest.QuestConditionType.Gather, targetId, amount);
    }

    private void HandleCraftCompleted(int targetId, int amount)
    {
        if (targetId < 0)
        {
            Debug.LogWarning("Quest progress skipped: craft target ID could not be resolved.");
            return;
        }

        ApplyProgress(Quest.QuestConditionType.Craft, targetId, amount);
    }

    private void ApplyProgress(Quest.QuestConditionType conditionType, int targetId, int amount)
    {
        for (int i = 0; i < _activeQuests.Count; i++)
        {
            QuestProgressData questProgress = _activeQuests[i];
            if (questProgress.Quest == null)
                continue;

            bool wasCompleted = questProgress.IsCompleted;
            bool hasMatchedCondition = false;
            bool questCompleted = true;

            for (int j = 0; j < questProgress.Quest.Conditions.Count; j++)
            {
                Quest.QuestRequirement condition = questProgress.Quest.Conditions[j];

                if (condition.ConditionType == conditionType &&
                    (condition.TargetId == 0 || condition.TargetId == targetId))
                {
                    hasMatchedCondition = true;
                    int newCount = questProgress.CurrentCounts[j] + amount;
                    questProgress.CurrentCounts[j] = Mathf.Min(newCount, condition.RequiredCount);
                }

                if (questProgress.CurrentCounts[j] < condition.RequiredCount)
                {
                    questCompleted = false;
                }
            }

            questProgress.IsCompleted = questCompleted;

            if (!hasMatchedCondition)
                continue;

            if (questProgress.IsCompleted && !wasCompleted)
            {
                Debug.Log($"Quest completed: {questProgress.Quest.QuestName} (ID: {questProgress.Quest.QuestId})");
            }
            else
            {
                Debug.Log($"Quest in progress: {questProgress.Quest.QuestName} (ID: {questProgress.Quest.QuestId})");
            }
        }

        UIEvents.EvQuestProgressChanged();
    }

    private QuestProgressData FindQuestProgress(int questId)
    {
        for (int i = 0; i < _activeQuests.Count; i++)
        {
            QuestProgressData questProgress = _activeQuests[i];
            if (questProgress.Quest != null && questProgress.Quest.QuestId == questId)
                return questProgress;
        }

        return null;
    }
}

    