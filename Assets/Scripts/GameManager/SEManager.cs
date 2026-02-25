using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoundEffects
{
    [System.Serializable]
    public class SESoundData
    {
        public SEType type;
        public AudioClip clip;
        public float volume = 1f;
        public float baseRadius = 5f;
    }

    public struct SoundEvent
    {
        public Vector3 position;
        public SEType seType;
        public SEGroup seGroup;
        public float baseRadius;
    }


    public class SEManager : MonoBehaviour
    {
        public static SEManager Instance { get; private set; }

        [Header("SE Database")]
        [SerializeField] private List<SESoundData> soundList;

        [Header("AudioSource Pool")]
        [SerializeField] private int poolSize = 10;

        private Dictionary<SEType, SESoundData> soundMap;
        private Queue<AudioSource> sourcePool;

        public event Action<SoundEvent> OnSoundPlayed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            BuildDatabase();
            CreatePool();
        }

        private void BuildDatabase()
        {
            soundMap = new Dictionary<SEType, SESoundData>();

            foreach (var data in soundList)
            {
                soundMap[data.type] = data;
            }
        }

        private void CreatePool()
        {
            sourcePool = new Queue<AudioSource>();

            for (int i = 0; i < poolSize; i++)
            {
                GameObject go = new GameObject("SE_AudioSource");
                go.transform.parent = transform;

                AudioSource source = go.AddComponent<AudioSource>();
                source.spatialBlend = 1f; // 3D sound

                sourcePool.Enqueue(source);
            }
        }

        private AudioSource GetSource()
        {
            if (sourcePool.Count > 0)
            {
                return sourcePool.Dequeue();
            }

            GameObject go = new GameObject("SE_AudioSource_Extra");
            go.transform.parent = transform;

            return go.AddComponent<AudioSource>();
        }

        private void ReturnSource(AudioSource source)
        {
            source.Stop();
            sourcePool.Enqueue(source);
        }

        public void PlaySE(SEType type, SEGroup group, Vector3 position)
        {
            if (!soundMap.TryGetValue(type, out var data))
            {
                Debug.LogWarning($"SE not found: {type}");
                return;
            }

            

            AudioSource source = GetSource();

            source.transform.position = position;
            source.clip = data.clip;
            source.volume = data.volume;
            source.Play();

            StartCoroutine(ReturnAfterPlay(source));

            // SoundEvent 発行
            SoundEvent soundEvent = new SoundEvent
            {
                position = position,
                seType = type,
                seGroup = group,
                baseRadius = data.baseRadius
            };

            OnSoundPlayed?.Invoke(soundEvent);
        }

        private System.Collections.IEnumerator ReturnAfterPlay(AudioSource source)
        {
            yield return new WaitWhile(() => source.isPlaying);
            ReturnSource(source);
        }
    }
}