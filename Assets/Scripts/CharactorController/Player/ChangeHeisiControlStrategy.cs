using UnityEngine;

namespace StateManager
{
    /// <summary>
    /// 「兵士」へ変身した状態のプレイヤー操作Strategy。
    /// 基底クラスとの違いは、Tabキーで一時的にUI操作ステートへ移行できる点のみ。
    /// </summary>
    public class ChangeHeisiControllerStrategy : PlayerControlStrategyBase
    {
        public ChangeHeisiControllerStrategy(PlayerController context) : base(context) { }

        // 変身状態固有の入力: TabキーでUI操作モードへ
        protected override void HandleTransformInput()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
                ChangeUIControlleState();
        }
    }
}
