using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SpriteSequencePlayer : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fadeDuration = 0.2f;

    [Header("随机切换时间范围（秒）")]
    [SerializeField] private float minInterval = 3f;
    [SerializeField] private float maxInterval = 5f;

    private Image image;
    private int currentFrame = 0;
    private float timer = 0f;
    private float currentInterval;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    private void Start()
    {
        if (frames != null && frames.Length > 0)
            image.sprite = frames[0];

        currentInterval = RandomInterval();
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0) return;

        timer += Time.unscaledDeltaTime;

        if (timer >= currentInterval)
        {
            timer = 0f;
            currentInterval = RandomInterval(); // 切换后重新随机下一次时间
            SwitchFrame();
        }
    }

    private void SwitchFrame()
    {
        int nextFrame;
        do
        {
            nextFrame = Random.Range(0, frames.Length);
        } while (frames.Length > 1 && nextFrame == currentFrame);

        image.DOFade(0f, fadeDuration)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                currentFrame = nextFrame;
                image.sprite = frames[currentFrame];
                image.DOFade(1f, fadeDuration).SetUpdate(true);
            });
    }

    private float RandomInterval()
    {
        return Random.Range(minInterval, maxInterval);
    }
}