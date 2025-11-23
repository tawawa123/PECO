using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private GameObject interactionUI;
    [SerializeField] private TextMeshProUGUI interactionText; // TextMeshProUGUIでも可

    private Interactable currentTarget;

    private void Start()
    {
        InteractManager.OnAboutToBeDestroyed += OnTargetDestroyed;

        if (interactionUI != null)
            interactionUI.SetActive(false);
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
            if (interactionUI != null)
            {
                interactionText.text = interactable.GetInteractionText();
                interactionUI.SetActive(true);
            }
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
        if (interactionUI != null)
            interactionUI.SetActive(false);
    }

    private void OnTargetDestroyed(Interactable obj)
    {
        SetFalseInteractUI();
    }
}
