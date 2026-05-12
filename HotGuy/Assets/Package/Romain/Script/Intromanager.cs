using System.Collections;
using UnityEngine;

/// <summary>
/// 挂在开头场景的根 GameObject 上。
/// 由 TutorialManager 在试玩结束后（或跳过试玩后）调用 StartIntro() 触发。
/// </summary>
public class IntroManager : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 内部数据结构
    // ──────────────────────────────────────────────

    [System.Serializable]
    public class AnimatorEntry
    {
        [Tooltip("是否启用该动画机")]
        public bool enabled = true;

        public Animator animator;

        [Tooltip("要触发的 Trigger 参数名")]
        public string triggerName = "PlayIntro";

        [Tooltip("从 Intro 开始后，等待多少秒再触发该 Trigger")]
        [Min(0f)]
        public float delayBeforeTrigger = 0f;

        [Tooltip("触发后，等待多少秒视为该动画机播完（用于计算总时长）")]
        [Min(0f)]
        public float estimatedDuration = 1f;
    }

    [System.Serializable]
    public class SfxEntry
    {
        public float triggerTime;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1f;
    }

    // ──────────────────────────────────────────────
    // Inspector 字段
    // ──────────────────────────────────────────────

    [Header("Animators")]
    [Tooltip("可配置多个动画机，每个可单独开关、单独设置延迟与时长")]
    [SerializeField] private AnimatorEntry[] animatorEntries;

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

    // ──────────────────────────────────────────────
    // 公开接口
    // ──────────────────────────────────────────────

    /// <summary>由 TutorialManager 调用（无论教程开关开启还是关闭）。</summary>
    public void StartIntro()
    {
        StartCoroutine(RunIntro());
    }

    // ──────────────────────────────────────────────
    // 核心流程
    // ──────────────────────────────────────────────

    private IEnumerator RunIntro()
    {
        yield return null;

        if (GameManager.Instance != null)
            GameManager.Instance.ShowPromptForCurrentLevel();

        // ── BGM ──────────────────────────────────
        if (bgmSource != null && bgmClip != null)
        {
            bgmSource.clip   = bgmClip;
            bgmSource.volume = bgmVolume;
            bgmSource.loop   = false;
            bgmSource.Play();
        }

        // ── 动画机：各自独立延迟触发 Trigger ─────
        float maxFinishTime = 0f;

        if (animatorEntries != null)
        {
            foreach (AnimatorEntry entry in animatorEntries)
            {
                if (!entry.enabled || entry.animator == null)
                    continue;

                // 每个动画机启动独立协程，到时间后触发 Trigger
                StartCoroutine(TriggerAfterDelay(entry));

                // 该动画机预计完成的绝对时间 = 延迟 + 估算时长
                float finishTime = entry.delayBeforeTrigger + entry.estimatedDuration;
                if (finishTime > maxFinishTime)
                    maxFinishTime = finishTime;
            }
        }

        // ── SFX 序列 ─────────────────────────────
        if (sfxEntries != null && sfxEntries.Length > 0 && sfxSource != null)
            StartCoroutine(PlaySfxSequence(sfxEntries));

        // ── 等待所有动画机均播完 ──────────────────
        if (maxFinishTime > 0f)
            yield return new WaitForSeconds(maxFinishTime);

        // ── 收尾 ─────────────────────────────────
        if (introRoot != null)
            introRoot.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.StartGame();
    }

    // ──────────────────────────────────────────────
    // 动画机延迟触发协程
    // ──────────────────────────────────────────────

    private IEnumerator TriggerAfterDelay(AnimatorEntry entry)
    {
        if (entry.delayBeforeTrigger > 0f)
            yield return new WaitForSeconds(entry.delayBeforeTrigger);

        if (entry.animator != null && !string.IsNullOrEmpty(entry.triggerName))
            entry.animator.SetTrigger(entry.triggerName);
    }

    // ──────────────────────────────────────────────
    // SFX 辅助协程
    // ──────────────────────────────────────────────

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