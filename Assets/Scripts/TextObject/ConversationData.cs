using UnityEngine;

[System.Serializable]
public class ConversationSequence
{
    public int requiredProgress; // このテキストを表示するための進行度
    [TextArea(2, 5)] public string[] texts; // 複数文を保持
}

[CreateAssetMenu(fileName = "ConversationData", menuName = "Data/Conversation")]
public class ConversationData : ScriptableObject
{
    public string npcName;
    public ConversationSequence[] conversations;

    // 現在の進行度に合う会話シーケンスを取得
    public ConversationSequence GetConversation(int currentProgress)
    {
        for (int i = conversations.Length - 1; i >= 0; i--)
        {
            if (currentProgress >= conversations[i].requiredProgress)
                return conversations[i];
        }
        return null;
    }
}
