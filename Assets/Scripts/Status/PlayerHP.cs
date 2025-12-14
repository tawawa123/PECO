using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHP : MonoBehaviour
{
    private Slider slider;
    private PlayerStatus p_status;

    private void Start()
    {
        p_status = GameManager.Instance.CurrentStatus;
        slider = this.GetComponent<Slider>();

        slider.maxValue = p_status.GetHp;
    }

    // Update is called once per frame
    void Update()
    {
        slider.value = p_status.GetHp;
    }
}
