using System.Collections;
using UnityEngine;

/// <summary>
/// 挂在开头场景的根 GameObject 上。
/// 由 TutorialManager 在试玩结束后（或跳过试玩后）调用 StartIntro() 触发。
/// </summary>
public class IntroManager : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator introAnimator;
    [SerializeField] private string introStateName = "Intro";

    [Header("Audio - BGM")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip bgmClip;
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 1f;

    [Header("Audio - SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private SfxEntry[] sfxEntries;

    [Header("Intro GameObject")]
    [Tooltip("开头结束后要隐藏的根 GameObject")]
    [SerializeField] private GameObject introRoot;

    [System.Serializable]
    public class SfxEntry
    {
        public float triggerTime;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1f;
    }

    /// <summary>由 TutorialManager 调用（无论教程开关开启还是关闭）。</summary>
    public void StartIntro()
    {
        StartCoroutine(RunIntro());
    }

    private IEnumerator RunIntro()
    {
        yield return null;

        if (GameManager.Instance != null)
            GameManager.Instance.ShowPromptForCurrentLevel();

        if (bgmSource != null && bgmClip != null)
        {
            bgmSource.clip   = bgmClip;
            bgmSource.volume = bgmVolume;
            bgmSource.loop   = false;
            bgmSource.Play();
        }

        float introDuration = 0f;

        if (introAnimator != null)
        {
            introAnimator.Play(introStateName, 0, 0f);
            yield return null;
            introDuration = introAnimator.GetCurrentAnimatorStateInfo(0).length;
        }

        if (sfxEntries != null && sfxEntries.Length > 0 && sfxSource != null)
            StartCoroutine(PlaySfxSequence(sfxEntries));

        if (introDuration > 0f)
            yield return new WaitForSeconds(introDuration);

        if (introRoot != null)
            introRoot.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.StartGame();
    }

    private IEnumerator PlaySfxSequence(SfxEntry[] entries)
    {
        float elapsed = 0f;
        int index = 0;

        System.Array.Sort(entries, (a, b) => a.triggerTime.CompareTo(b.triggerTime));

        while (index < entries.Length)
        {
            SfxEntry entry = entries[index];

            if (elapsed >= entry.triggerTime)
            {
                if (entry.clip != null)
                    sfxSource.PlayOneShot(entry.clip, entry.volume);
                index++;
            }
            else
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
        }
    }
}