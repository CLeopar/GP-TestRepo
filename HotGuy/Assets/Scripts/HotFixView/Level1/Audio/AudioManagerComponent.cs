// AudioManagerComponent.cs
using System.Collections.Generic;
using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class AudioManagerComponent : Entity
{
    private static readonly Dictionary<SFXType, string> SFXPaths = new()
    {
        { SFXType.Dong,         "SFX_Dong" },
        { SFXType.HitWrongBruh, "SFX_HitWrongBruh" },
        { SFXType.Hurt,         "SFX_hurt" },
        { SFXType.Eating1,      "SFX_Eating1" },
        { SFXType.Complete,     "SFX_Complete" },
        { SFXType.Ding,         "SFX_ding" },
        { SFXType.Heaven,       "SFX_Heaven" },
        { SFXType.Werwerwer,    "SFX_werwerwer" },
        { SFXType.ScoreWin,     "SFX_ScoreWin" },
        { SFXType.ScoreWrong,   "SFX_ScoreWrong" },
        { SFXType.DogWhimperSad,"SFX_DogWhimperSad" },
        { SFXType.EatBadEgg,    "EatBadEgg" },
    };

    private Dictionary<SFXType, AudioClip> _cache = new();
    private HashSet<SFXType> _loading = new();

    private AudioSource _eatingSFXSource;
    private AudioSource _heavenSFXSource;
    private AudioSource _dogWhimperSource;
    private AudioSource _werwerwerSource;
    private AudioSource _eatBadEggSource;

    private AudioSource EatingSource
    {
        get
        {
            if (_eatingSFXSource == null)
            {
                var go = new GameObject("EatingAudioSource");
                Object.DontDestroyOnLoad(go);
                _eatingSFXSource = go.AddComponent<AudioSource>();
                _eatingSFXSource.playOnAwake = false;
                _eatingSFXSource.loop = true;
                _eatingSFXSource.spatialBlend = 0f;
            }
            return _eatingSFXSource;
        }
    }

    private AudioSource HeavenSource
    {
        get
        {
            if (_heavenSFXSource == null)
            {
                var go = new GameObject("HeavenAudioSource");
                Object.DontDestroyOnLoad(go);
                _heavenSFXSource = go.AddComponent<AudioSource>();
                _heavenSFXSource.playOnAwake = false;
                _heavenSFXSource.spatialBlend = 0f;
            }
            return _heavenSFXSource;
        }
    }

    private AudioSource DogWhimperSource
    {
        get
        {
            if (_dogWhimperSource == null)
            {
                var go = new GameObject("DogWhimperAudioSource");
                Object.DontDestroyOnLoad(go);
                _dogWhimperSource = go.AddComponent<AudioSource>();
                _dogWhimperSource.playOnAwake = false;
                _dogWhimperSource.loop = true;
                _dogWhimperSource.spatialBlend = 0f;
            }
            return _dogWhimperSource;
        }
    }

    private AudioSource WerwerwerSource
    {
        get
        {
            if (_werwerwerSource == null)
            {
                var go = new GameObject("WerwerwerAudioSource");
                Object.DontDestroyOnLoad(go);
                _werwerwerSource = go.AddComponent<AudioSource>();
                _werwerwerSource.playOnAwake = false;
                _werwerwerSource.loop = true;
                _werwerwerSource.spatialBlend = 0f;
            }
            return _werwerwerSource;
        }
    }

    private AudioSource EatBadEggSource
    {
        get
        {
            if (_eatBadEggSource == null)
            {
                var go = new GameObject("EatBadEggAudioSource");
                Object.DontDestroyOnLoad(go);
                _eatBadEggSource = go.AddComponent<AudioSource>();
                _eatBadEggSource.playOnAwake = false;
                _eatBadEggSource.loop = true;
                _eatBadEggSource.spatialBlend = 0f;
                _eatBadEggSource.volume = 0.4f;
            }
            return _eatBadEggSource;
        }
    }

    public async FTask Play(SFXType type, Vector3? worldPos = null)
    {
        if (_cache.TryGetValue(type, out var cachedClip))
        {
            PlayClip(type, cachedClip, worldPos);
            return;
        }

        if (_loading.Contains(type))
        {
            Log.Error($"[AudioManager] {type} is loading, skip");
            return;
        }

        if (!SFXPaths.TryGetValue(type, out var path))
        {
            Log.Error($"[AudioManager] No path for {type}");
            return;
        }

        _loading.Add(type);
        Log.Error($"[AudioManager] Loading on-demand: {type}");

        var loader = Scene.GetComponent<ResourceLoaderComponent>();
        var clip = await loader.LoadAssetAsync<AudioClip>(path);

        _loading.Remove(type);

        if (clip == null)
        {
            Log.Error($"[AudioManager] Failed to load: {path}");
            return;
        }

        _cache[type] = clip;
        Log.Error($"[AudioManager] ✅ Loaded & playing: {type}");

        PlayClip(type, clip, worldPos);
    }

    public void StopEating()
    {
        if (_eatingSFXSource != null && _eatingSFXSource.isPlaying)
            _eatingSFXSource.Stop();
    }

    public void StopHeaven(float fadeDuration = 0.5f)
    {
        if (_heavenSFXSource == null || !_heavenSFXSource.isPlaying) return;
        
        if (fadeDuration <= 0f)
        {
            _heavenSFXSource.Stop();
            return;
        }
        
        FadeOutSource(_heavenSFXSource, fadeDuration).Coroutine();
    }

    public void StopDogWhimperSad(float fadeDuration = 0.3f)
    {
        if (_dogWhimperSource == null || !_dogWhimperSource.isPlaying) return;
        
        if (fadeDuration <= 0f)
        {
            _dogWhimperSource.Stop();
            return;
        }
        
        FadeOutSource(_dogWhimperSource, fadeDuration).Coroutine();
    }

    public void StopWerwerwer()
    {
        if (_werwerwerSource != null && _werwerwerSource.isPlaying)
            _werwerwerSource.Stop();
    }

    public void StopEatBadEgg()
    {
        if (_eatBadEggSource != null && _eatBadEggSource.isPlaying)
            _eatBadEggSource.Stop();
    }

    private async FTask FadeOutSource(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            await FTask.Wait(Scene, (long)(Time.deltaTime * 1000));
        }
        
        source.Stop();
        source.volume = startVolume;
    }

    private void PlayClip(SFXType type, AudioClip clip, Vector3? worldPos)
    {
        if (type == SFXType.Eating1)
        {
            if (EatingSource.clip != clip)
                EatingSource.clip = clip;
            EatingSource.Stop();
            EatingSource.Play();
            Log.Error($"[AudioManager] 🔊 PLAYED (persistent loop): {clip.name}");
        }
        else if (type == SFXType.Heaven)
        {
            HeavenSource.clip = clip;
            HeavenSource.Play();
            Log.Error($"[AudioManager] 🔊 PLAYED (persistent): {clip.name}");
        }
        else if (type == SFXType.DogWhimperSad)
        {
            if (DogWhimperSource.clip != clip)
                DogWhimperSource.clip = clip;
            if (!DogWhimperSource.isPlaying)
                DogWhimperSource.Play();
            Log.Error($"[AudioManager] 🔊 PLAYED (persistent loop): {clip.name}");
        }
        else if (type == SFXType.Werwerwer)
        {
            if (WerwerwerSource.clip != clip)
                WerwerwerSource.clip = clip;
            if (!WerwerwerSource.isPlaying)
                WerwerwerSource.Play();
            Log.Error($"[AudioManager] 🔊 PLAYED (persistent loop): {clip.name}");
        }
        else if (type == SFXType.EatBadEgg)
        {
            if (EatBadEggSource.clip != clip)
                EatBadEggSource.clip = clip;
            EatBadEggSource.volume = 0.4f;
            EatBadEggSource.mute = false;
            if (!EatBadEggSource.isPlaying)
            {
                EatBadEggSource.Play();
                Log.Error($"[AudioManager] 🔊 PLAYED BGM: {clip.name}, volume={EatBadEggSource.volume}, isPlaying={EatBadEggSource.isPlaying}");
            }
        }
        else
        {
            var pos = worldPos ?? (Camera.main?.transform.position ?? Vector3.zero);
            AudioSource.PlayClipAtPoint(clip, pos, 1f);
            Log.Error($"[AudioManager] 🔊 PLAYED: {clip.name} at {pos}");
        }
    }
}

public class AudioManagerComponent_Awake : AwakeSystem<AudioManagerComponent>
{
    protected override void Awake(AudioManagerComponent self)
    {
    }
}