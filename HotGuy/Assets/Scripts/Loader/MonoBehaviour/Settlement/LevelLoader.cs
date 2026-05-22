using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class LevelLoader : MonoBehaviour
{
    [Header("Scene")]
    public string sceneName;

    [Header("Fade Settings")]
    public Image fadeImage;
    public float fadeDuration = 1f;

    [Header("Audio - Click")]
    public AudioSource audioSource;
    public AudioClip clickSound;

    [Header("Audio - Music Fade")]
    public AudioSource musicSource;     // 背景音乐
    public float musicFadeDuration = 1f;

    private bool isLoading = false;

    public void LoadLevel()
    {
        if (isLoading) return;
        isLoading = true;

        if (audioSource && clickSound)
        {
            audioSource.PlayOneShot(clickSound);
        }

        StartCoroutine(FadeAndLoad());
    }

    IEnumerator FadeAndLoad()
    {
        float t = 0f;

        Color color = fadeImage.color;
        float musicStartVolume = musicSource ? musicSource.volume : 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float progress = t / fadeDuration;

            // 屏幕渐黑
            color.a = Mathf.Lerp(0f, 1f, progress);
            fadeImage.color = color;

            // 音乐渐弱
            if (musicSource)
            {
                musicSource.volume = Mathf.Lerp(musicStartVolume, 0f, progress);
            }

            yield return null;
        }

        SceneManager.LoadScene(sceneName);
    }
}