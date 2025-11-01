using UnityEngine;
using UnityEditor;
 
public class ChangeAllChildrenTag : MonoBehaviour 
{
 
    [MenuItem("MyTools/Change All Children Tag")]
    static void ChangeTag()
    {
        // ヒエラルキー上で選択した全ての要素
        Transform[] transforms = Selection.transforms;
 
        foreach (Transform transform in transforms) {
            GetChildren(transform);
        }
    }
 
    static void GetChildren(Transform transform)
    {
        // 新しく設定するタグ
        string newTag = "Untagged";
 
        // タグを設定
        transform.tag = newTag;
 
        // 子要素を取得
        Transform children = transform.GetComponentInChildren<Transform>();
        if (children.childCount == 0)
        {
            // 見つからなければ終了
            return;
        }
 
        foreach (Transform child in children)
        {
            // 子要素の子要素も同様に
            GetChildren(child);
        }
    }
}