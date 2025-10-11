using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destination : MonoBehaviour
{
    //初期位置
    private Vector3 startPosition;
    //目的地
    [SerializeField] private Vector3 destination;
    [SerializeField] private Transform[] targets;
    [SerializeField] private int order = 0;

    public enum Route {inOrder, random}
    public Route route;


    void Start()
    {
        //　初期位置を設定
        startPosition = transform.position;
        SetDestination(transform.position);
    }

    public void CreateDestination()
    {
        if(route == Route.inOrder)
        {
            CreateInOrderDestination();
        }else if(route == Route.random)
        {
            CreateRandomDestination();
        }
    }

    //targetsに設定した順番に作成
    private void CreateInOrderDestination()
    {
        if(order < targets.Length-1)
        {
            order++;
            SetDestination(new Vector3(targets[order].transform.position.x, targets[order].transform.position.y, targets[order].transform.position.z));
        }
        else
        {
            order = 0;
            SetDestination(new Vector3(targets[order].transform.position.x, targets[order].transform.position.y, targets[order].transform.position.z));
        }
    }

    //　targetsからランダムに作成
    private void CreateRandomDestination()
    {
        int num = Random.Range(0, targets.Length);
        SetDestination(new Vector3(targets[num].transform.position.x, targets[order].transform.position.y, targets[num].transform.position.z));
    }

    //　目的地の設定
    public void SetDestination(Vector3 position)
    {
        destination = position;
    }

    //　目的地の取得
    public Vector3 GetDestination()
    {
        return destination;
    }
}
