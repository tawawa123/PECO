public interface IPlayerControlStrategy
{
    void OnEnter();     // 変身時
    void OnExit();      // 変身解除時
    void Tick();        // Update

    // 外部入力からステートの強制変更
    void ChangeParry();
    void ChangeStun();
    void AddDamage(int damage);
}
