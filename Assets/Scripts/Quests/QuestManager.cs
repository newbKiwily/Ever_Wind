using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : SingletonBase<QuestManager>
{
    public System.Func<int, bool> RequestClaim;
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
        SyncQuestTable();
        LoadQuestProgress();
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

        
    }

    public List<QuestLoadData> GetSaveData()
    {
        List<QuestLoadData> saveData = new List<QuestLoadData>(_activeQuests.Count);

        for (int i = 0; i < _activeQuests.Count; i++)
        {
            QuestProgressData questProgress = _activeQuests[i];
            if (questProgress.Quest == null)
                continue;

            QuestLoadData data = new QuestLoadData
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
        QuestProgressData progress = FindQuestProgress(questId);

        if (progress == null || !progress.IsCompleted || progress.RewardClaimed)
        {
            return false;
        }

        return RequestClaim?.Invoke(questId) ?? false;
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

        foreach (Quest quest in _questTable.Values)
        {
            AcceptQuest(quest);
        }

        _isInitializing = false;
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
    public void ApplyServerProgress(int questId,int condition,int count,bool completed,bool claimed)
    {
        var progress=FindQuestProgress(questId);if(progress==null)return;
        if(condition>=0 && condition<progress.CurrentCounts.Count) progress.CurrentCounts[condition]=count;
        progress.IsCompleted=completed;progress.RewardClaimed=claimed;
    }
}
