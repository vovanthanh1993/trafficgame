using UnityEngine;

[System.Serializable]
public class QuestObjective
{
    [Tooltip("Loại item cần collect")]
    public ItemType itemType;
    
    [Tooltip("Số lượng item cần collect")]
    public int requiredAmount;
}
