namespace SoundEffects
{
    public enum SEType
    {
        None = 0,
        
        // プレイヤーSE
        PlayerAttack1,  // 攻撃1
        PlayerAttack2,  // 攻撃2
        PlayerAttack3,  // 攻撃3
        Walk,           // 歩き
        Run,            // 走り
        ClouchWalk,     // しゃがみ
        Sliding,        // スライディング
        Rolling,        // 回避
        Parry,          // パリィ
        Guard,          // ガード

        // 槍クマSE
    }

    public enum SEGroup
    {
        Player,     // プレイヤー
        Enemy,      // 敵
        UI,         // オブジェクト
        Field,
    }
}