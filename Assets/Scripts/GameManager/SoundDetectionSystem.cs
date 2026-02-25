using System.Collections.Generic;
using UnityEngine;
using SoundEffects;

public class SoundDetectionSystem : MonoBehaviour
{
    public static SoundDetectionSystem Instance { get; private set; }

    private readonly List<EnemySoundListener> listeners = new();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (SEManager.Instance != null)
            SEManager.Instance.OnSoundPlayed += OnSoundPlayed;
    }

    private void OnDisable()
    {
        if (SEManager.Instance != null)
            SEManager.Instance.OnSoundPlayed -= OnSoundPlayed;
    }

    public void RegisterListener(EnemySoundListener listener)
    {
        if (!listeners.Contains(listener))
            listeners.Add(listener);
    }

    public void UnregisterListener(EnemySoundListener listener)
    {
        listeners.Remove(listener);
    }

    private void OnSoundPlayed(SoundEvent soundEvent)
    {
        // UI音などは無視
        if (soundEvent.seGroup == SEGroup.UI)
            return;

        foreach (var listener in listeners)
        {
            if (!listener.HearingEnabled)
                continue;

            float distance = Vector3.Distance(
                soundEvent.position,
                listener.transform.position
            );

            float hearingRange = soundEvent.baseRadius * listener.HearingMultiplier;

            if (distance <= hearingRange)
            {
                listener.OnSoundHeard(soundEvent);
            }
        }
    }
}
