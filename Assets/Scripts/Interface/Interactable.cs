public interface Interactable
{
    void Interact(); // Fキーで実行される共通の入口
    string GetInteractionText(); // UI表示などに利用できる
}
