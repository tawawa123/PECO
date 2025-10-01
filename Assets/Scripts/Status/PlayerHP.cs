using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHP : MonoBehaviour
{
    [SerializeField] private GameObject Player;
    private Slider slider;
    private PlayerStatus p_status;

    void Awake()
    {
        p_status = Player.GetComponent<PlayerStatus>();
        slider = this.GetComponent<Slider>();

        slider.maxValue = p_status.GetHp;
    }

    // Update is called once per frame
    void Update()
    {
        slider.value = p_status.GetHp;
    }
}
