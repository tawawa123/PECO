using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHP : MonoBehaviour
{
    private Slider slider;
    private EnemyStatus e_status;

    void Awake()
    {
        e_status = GetComponentInParent<EnemyStatus>();
        slider = this.GetComponent<Slider>();

        slider.maxValue = e_status.GetHp;
    }

    // Update is called once per frame
    void Update()
    {
        slider.value = e_status.GetHp;
        this.gameObject.transform.LookAt(GameObject.Find("Free Camera").transform.position);
    }
}
