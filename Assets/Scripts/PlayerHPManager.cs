using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHPManager : MonoBehaviour
{
    private PlayerStatus pStatus;
    public int maxHP;
    [SerializeField] private Slider slider;

    void Awake()
    {
        pStatus = this.GetComponent<PlayerStatus>();
        slider.value = 1;
        maxHP = this.pStatus.Hp;
    }

    public void Update()
    {
        slider.value = (float)pStatus.Hp / (float)maxHP;
    }
}
