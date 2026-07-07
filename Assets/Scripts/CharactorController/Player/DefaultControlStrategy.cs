namespace StateManager
{
    /// <summary>
    /// 変身していない通常状態のプレイヤー操作Strategy。
    /// 挙動はすべて基底クラス PlayerControlStrategyBase が提供するため、
    /// このクラスは型を分けるためのエントリポイントのみを持つ。
    /// </summary>
    public class DefaultControllerStrategy : PlayerControlStrategyBase
    {
        public DefaultControllerStrategy(PlayerController context) : base(context) { }
    }
}
