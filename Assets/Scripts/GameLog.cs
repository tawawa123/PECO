using UnityEngine;

/// <summary>
/// 開発用トレースログ。
/// GAME_LOG シンボルが定義されているときだけ実行される。
/// 未定義（通常ビルド）では [Conditional] により呼び出しごと除去されるため、
/// 引数の文字列生成やボックス化も含めて一切のGC負荷が発生しない。
///
/// エディタでトレースを有効化する場合は、Player Settings の
/// Scripting Define Symbols に「GAME_LOG」を追加する。
/// </summary>
public static class GameLog
{
    [System.Diagnostics.Conditional("GAME_LOG")]
    public static void Trace(object message) => Debug.Log(message);
}
