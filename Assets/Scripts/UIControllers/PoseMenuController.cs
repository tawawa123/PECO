using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameUI;

public class PoseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject poseMenuPanel;
    [SerializeField] private GameObject keyConImage;
    [SerializeField] private GameObject tutorialPanel;

    private GameObject expTutorialWindow;

    public void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            AllSetActiveFalse();
            poseMenuPanel.SetActive(!poseMenuPanel.activeSelf);
        }
    }

    private void AllSetActiveFalse()
    {
        // PoseMenuに配置してあるアクティブウィンドウをすべて非表示にする
        keyConImage.SetActive(false);
        tutorialPanel.SetActive(false);

        if(expTutorialWindow != null)
            expTutorialWindow.SetActive(false);
    }

    // キーコンフィグメニューを表示
    public void KeyCon()
    {
        AllSetActiveFalse();
        keyConImage.SetActive(!keyConImage.activeSelf);
    }

    // チュートリアルウィンドウを表示
    public void Tutorial()
    {
        AllSetActiveFalse();
        tutorialPanel.SetActive(!tutorialPanel.activeSelf);
    }

    // チュートリアルの各説明項目を表示
    public void ExplainTutorialMenu(GameObject tutorialWindow)
    {
        if(expTutorialWindow != null)
            expTutorialWindow.SetActive(false);

        this.expTutorialWindow = tutorialWindow;
        tutorialWindow.SetActive(!tutorialWindow.activeSelf);
    }
}
