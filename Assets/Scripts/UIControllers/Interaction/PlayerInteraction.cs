using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using GameUI;

public class PlayerInteraction : MonoBehaviour
{
    private TextMeshProUGUI interactionText; // TextMeshProUGUIでも可
    private Interactable currentTarget;

    private void Start()
    {
        interactionText = UIManager.Instance.Get(UIType.InteractText).GetComponent<TextMeshProUGUI>();
        InteractManager.OnAboutToBeDestroyed += OnTargetDestroyed;
    }

    private void Update()
    {
        // Fキーでフィールドオブジェクトへのインタラクト起動
        if (currentTarget != null && Input.GetKeyDown(KeyCode.F))
        {
            currentTarget.Interact();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // あたってるオブジェクトが消えたとき
        // var notifier = other.GetComponent<InteractManager>();
        // if (notifier == null)
        //     SetFalseInteractUI();

        // あたってるオブジェクトから離れたとき
        if (other.TryGetComponent(out Interactable interactable))
        {
            currentTarget = interactable;

            interactionText.text = interactable.GetInteractionText();
            UIManager.Instance.SetGroupActive(UIGroup.InGame, true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out Interactable interactable) && interactable == currentTarget)
        {
            SetFalseInteractUI();
        }
    }


    // interactUIを非表示にする
    private void SetFalseInteractUI()
    {
        currentTarget = null;
        UIManager.Instance.SetGroupActive(UIGroup.InGame, false);
    }

    private void OnTargetDestroyed(Interactable obj)
    {
        SetFalseInteractUI();
    }
}
