using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestContentUI : ButtonSlot
{
    [SerializeField]
    private TMP_Text _questNameText;

    [SerializeField]
    private TMP_Text _questDescriptionText;

    [SerializeField]
    private TMP_Text _conditionText;

    [SerializeField]
    private Color _incompleteColor = Color.red;

    [SerializeField]
    private Color _completeColor = Color.green;

    private QuestManager.QuestProgressData _questProgress;

    protected override void Awake()
    {
        if (GetComponent<Button>() == null)
        {
            gameObject.AddComponent<Button>();
        }

        base.Awake();
    }

    public void Init(QuestManager.QuestProgressData questProgress)
    {
        FindMissingReferences();

        if (questProgress == null || questProgress.Quest == null)
            return;

        _questProgress = questProgress;
        _item = null;

        string conditionText = BuildConditionText(questProgress);
        string rewardText = BuildRewardText(questProgress);
        string statusText = BuildStatusText(questProgress);

        if (_questNameText != null && _questDescriptionText != null && _conditionText != null)
        {
            _questNameText.text = questProgress.Quest.QuestName;
            _questDescriptionText.text = questProgress.Quest.QuestDescription;
            _conditionText.text = $"{conditionText}\n{rewardText}\n{statusText}";
            return;
        }

        TMP_Text combinedText = _questNameText != null ? _questNameText : GetComponentInChildren<TMP_Text>(true);
        if (combinedText == null)
            return;

        combinedText.text = $"{questProgress.Quest.QuestName}\n{questProgress.Quest.QuestDescription}\n{conditionText}\n{rewardText}\n{statusText}";
    }

    protected override void OnClick()
    {
        if (_questProgress == null || _questProgress.Quest == null)
            return;

        if (!_questProgress.IsCompleted || _questProgress.RewardClaimed)
            return;

        QuestManager questManager = SingletonManager.Instance.GetSingleton<QuestManager>();
        if (questManager == null)
            return;

        questManager.ClaimReward(_questProgress.Quest.QuestId);
    }

    private void Reset()
    {
        FindMissingReferences();
    }

    private void OnValidate()
    {
        FindMissingReferences();
    }

    private void FindMissingReferences()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        if (_questNameText == null && texts.Length > 0)
            _questNameText = texts[0];

        if (_questDescriptionText == null && texts.Length > 1)
            _questDescriptionText = texts[1];

        if (_conditionText == null && texts.Length > 2)
            _conditionText = texts[2];
    }

    private string BuildConditionText(QuestManager.QuestProgressData questProgress)
    {
        List<Quest.QuestRequirement> conditions = questProgress.Quest.Conditions;
        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < conditions.Count; i++)
        {
            Quest.QuestRequirement condition = conditions[i];
            int currentCount = i < questProgress.CurrentCounts.Count ? questProgress.CurrentCounts[i] : 0;
            int requiredCount = condition.RequiredCount;
            bool isCompleted = currentCount >= requiredCount;
            Color color = isCompleted ? _completeColor : _incompleteColor;

            if (i > 0)
                builder.Append('\n');

            builder.Append("<color=#");
            builder.Append(ColorUtility.ToHtmlStringRGB(color));
            builder.Append(">");
            builder.Append(currentCount);
            builder.Append("/");
            builder.Append(requiredCount);
            builder.Append("</color>");
        }

        return builder.ToString();
    }

    private string BuildRewardText(QuestManager.QuestProgressData questProgress)
    {
        if (questProgress.Quest.Rewards == null || questProgress.Quest.Rewards.Count == 0)
            return "Reward: None";

        StringBuilder builder = new StringBuilder("Reward: ");

        for (int i = 0; i < questProgress.Quest.Rewards.Count; i++)
        {
            Quest.QuestReward reward = questProgress.Quest.Rewards[i];
            if (i > 0)
                builder.Append(", ");

            builder.Append(reward.ItemKey);
            builder.Append(" x");
            builder.Append(reward.Amount);
        }

        return builder.ToString();
    }

    private string BuildStatusText(QuestManager.QuestProgressData questProgress)
    {
        if (!questProgress.IsCompleted)
            return "Status: In Progress";

        if (questProgress.RewardClaimed)
            return "Status: Claimed";

        return "Status: Complete (Click to Claim)";
    }
}
