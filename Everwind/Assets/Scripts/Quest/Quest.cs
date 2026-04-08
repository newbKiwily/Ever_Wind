using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class Quest : ScriptableObject
{
    [Serializable]
    public class QuestRequirement
    {
        public QuestConditionType ConditionType;
        public int TargetId;
        public int RequiredCount;
    }

    public enum QuestConditionType
    {
        Combat,
        Gather,
        Craft
    }

    public int QuestId;
    public string QuestName;
    public string QuestDescription;
    public List<QuestRequirement> Conditions = new();
}

