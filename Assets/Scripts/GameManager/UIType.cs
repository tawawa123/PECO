namespace GameUI
{
    public enum UIType
    {
        None = 0,
        
        PauseMenu,      // ポーズメニュー
        Inventory,      // バッグメニュー
        GameOverMenu,   // 死亡時メニュー
        GameCrearMenu,

        HPBar,          // プレイヤーの体力
        StaminaBar,     // プレイヤーのスタミナ
        BossHP,         // ボスバー
        LockonCursor,   // ロックオンカーソル
        StealthAttackMarker, //ステルスアタック可能であることを示すマーカー
        CutIn,

        Minimap,        // ミニマップ
        TalkWindow,     // 会話ウィンドウ
        InteractMenu,
        InteractText,
    }

    public enum UIGroup
    {
        Always,     // 常時表示
        InGame,     // プレイ中
        Menu,       // メニュー系
        Battle,     // 戦闘時
        Dialogue,   // 会話中
    }
}