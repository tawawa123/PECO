using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SoundEffects;
using StateManager;

// enemyのための音を聞く機構
public class EnemySoundListener : MonoBehaviour
{
    [Header("Hearing Settings")]
    [SerializeField] private float hearingMultiplier = 1f;
    [SerializeField] private bool hearingEnabled = true;

    public float HearingMultiplier => hearingMultiplier;
    public bool HearingEnabled => hearingEnabled;

    private void Start()
    {
        SoundDetectionSystem.Instance.RegisterListener(this);
    }

    private void OnDisable()
    {
        if (SoundDetectionSystem.Instance != null)
            SoundDetectionSystem.Instance.UnregisterListener(this);
    }

    public void OnSoundHeard(SoundEvent soundEvent)
    {
        Debug.Log($"{name} heard sound: {soundEvent.seType}");

        // ここで EnemyAI に渡す
        var ai = GetComponent<YarikumaControllerStrategy>();
        if (ai != null)
        {
            ai.OnSoundHeard(soundEvent);
        }
    }
}
