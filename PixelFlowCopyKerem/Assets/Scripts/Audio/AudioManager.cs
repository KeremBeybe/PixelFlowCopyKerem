using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Ses Kütüphanesi")]
    public AudioClip[] gameSounds;

    [Header("Havuz Ayarlarý")]
    public int poolSize = 20;
    public bool canGrow = true;

    private List<AudioSource> audioPool;
    private GameObject poolContainer;

    [Header("Play Series Ayarlarý (ASMR)")]
    public float pitchStep = 0.1f;
    public float resetTime = 0.5f;
    public float maxPitch = 2.0f;

    private int comboCount = 0;
    private float lastPlayTime = 0;

    void Awake ()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        CreatePool();
    }

    void CreatePool ()
    {
        audioPool = new List<AudioSource>();
        poolContainer = new GameObject("Audio_Pool_Container");
        poolContainer.transform.SetParent(this.transform);

        for (int i = 0; i < poolSize; i++)
        {
            AddSourceToPool();
        }
    }

    AudioSource AddSourceToPool ()
    {
        GameObject obj = new GameObject("Pooled_AudioSource_" + audioPool.Count);
        obj.transform.SetParent(poolContainer.transform);

        AudioSource source = obj.AddComponent<AudioSource>();
        source.playOnAwake = false;

        audioPool.Add(source);
        return source;
    }

    // --- SENÝN KLASÝK ÝNDEKS SÝSTEMÝN ---
    public void Play2D (int index, float volume = 1f)
    {
        PlayInternal(index, Vector3.zero, volume, 0f);
    }

    public void Play3D (int index, Vector3 position, float volume = 1f)
    {
        PlayInternal(index, position, volume, 1f);
    }

    private void PlayInternal (int index, Vector3 pos, float vol, float spatialBlend)
    {
        if (index < 0 || index >= gameSounds.Length) return;
        if (gameSounds[index] == null) return;

        AudioSource source = GetFreeSource();
        if (source != null)
        {
            source.transform.position = pos;
            source.clip = gameSounds[index];
            source.volume = vol;
            source.spatialBlend = spatialBlend;
            source.pitch = Random.Range(0.95f, 1.05f); // Robotikliði önler
            source.Play();
        }
    }

    // --- YENÝ: EXTENSION METHOD ÝÇÝN DÝREKT KLÝP ÇALMA DESTEÐÝ ---
    public void PlayClip (AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetFreeSource();
        if (source != null)
        {
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.spatialBlend = 0f;
            source.Play();
        }
    }

    // --- EFSANE KOMBO SÝSTEMÝN (Küpler patlarken bunu çaðýracaðýz) ---
    public void PlaySeries (int index)
    {
        if (Time.time - lastPlayTime > resetTime)
        {
            comboCount = 0;
        }

        float newPitch = 1f + (comboCount * pitchStep);
        if (newPitch > maxPitch) newPitch = maxPitch;

        AudioSource source = GetFreeSource();
        if (source != null && index < gameSounds.Length)
        {
            source.clip = gameSounds[index];
            source.pitch = newPitch;
            source.spatialBlend = 0f;
            source.Play();
        }

        comboCount++;
        lastPlayTime = Time.time;
    }

    AudioSource GetFreeSource ()
    {
        for (int i = 0; i < audioPool.Count; i++)
        {
            if (!audioPool[i].isPlaying)
                return audioPool[i];
        }

        if (canGrow)
            return AddSourceToPool();

        return null;
    }
}