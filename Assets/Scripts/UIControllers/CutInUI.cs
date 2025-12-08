using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using Cysharp.Threading.Tasks;

public class CutInUI : MonoBehaviour
{
    [SerializeField] private GameObject cutInUI;
    [SerializeField] private GameObject stealthAttackEffect;

    private void Start()
    {
        cutInUI.SetActive(false);
    }

    private async UniTask<bool> PlayCutInAsync(
        CancellationToken token,
        Vector3 targetPos,
        float displayDuration = 1f)
    {
        Time.timeScale = 0f;
        cutInUI.SetActive(true);

        // カットイン時間待機
        await UniTask.Delay(System.TimeSpan.FromSeconds(displayDuration), ignoreTimeScale: true);

        cutInUI.SetActive(false);
        Time.timeScale = 1f;

        // エフェクトの生成
        Instantiate(stealthAttackEffect, targetPos, Quaternion.identity);
        // エフェクト再生時間待機
        await UniTask.Delay(System.TimeSpan.FromSeconds(2.5f), ignoreTimeScale: true);

        return true;
    }
}
