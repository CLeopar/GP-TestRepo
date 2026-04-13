using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [System.Serializable]
    public class Level
    {
        public GameObject levelObj;
        [HideInInspector] public List<PoseEditorController> poseEditorControllerList;
        [HideInInspector] public Image pictureImage;
        [HideInInspector] public Text similarityText;
        [HideInInspector] public float similarity = 0;
        public string promptString;

        [Tooltip("所有关节都需要移动到这个区域内才能得到 100% 完成度")]
        public PolygonZone zone = new PolygonZone();

        public void Init()
        {
            foreach (var tmp in levelObj.GetComponentsInChildren<PoseEditorController>())
                poseEditorControllerList.Add(tmp);
            pictureImage = levelObj.transform.Find("Picture").GetComponent<Image>();
            similarityText = levelObj.transform.Find("Similarity").GetComponent<Text>();
        }
    }

    [Header("Timer")]
    [SerializeField] private Text timerText;
    [SerializeField] private RectTransform timerImage;
    [SerializeField] private float time = 30f;

    [Header("Capture")]
    [SerializeField] private Image flashImage;

    [Header("Level")]
    [SerializeField] private List<Level> levelList;

    [Header("Others")]
    [SerializeField] private Text promptText;

    private float timer = 0f;
    private float timerImageInitialWidth;

    private Texture2D lastTexture;
    private Sprite lastSprite;

    private int currentLevel = 0;

    // 供 Editor 访问
    public List<Level> LevelList => levelList;
    public int CurrentLevel => currentLevel;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        foreach (var tmp in levelList)
            tmp.Init();

        promptText.text = levelList[currentLevel].promptString;
        timerImageInitialWidth = timerImage.sizeDelta.x;
        StartCoroutine(TimingCoroutine());
    }

    private void NewLevel()
    {
        if (currentLevel + 1 > levelList.Count - 1)
        {
            //TODO
            return;
        }
        levelList[currentLevel++].levelObj.SetActive(false);
        levelList[currentLevel].levelObj.SetActive(true);
        foreach (var tmp in levelList[currentLevel].poseEditorControllerList)
            tmp.Enable();
        promptText.text = levelList[currentLevel].promptString;
        timerImage.sizeDelta = new Vector2(timerImageInitialWidth, timerImage.sizeDelta.y);
        StartCoroutine(TimingCoroutine());
    }

    private void SetTimerValue(float value)
    {
        timer = Mathf.Clamp(value, 0f, time);
        timerImage.sizeDelta = new Vector2(timer / time * timerImageInitialWidth, timerImage.sizeDelta.y);
    }

    private IEnumerator TimingCoroutine()
    {
        int min = (int)(time / 60f), sec = (int)(time % 60f);
        SetTimerValue(time);

        while (min > 0 || sec > 0)
        {
            SetTimerValue(timer - 1f);

            if (sec > 0) sec--;
            else
            {
                sec = 59;
                min--;
            }

            timerText.text = (min > 9 ? min.ToString() : "0" + min) + ":" + (sec > 9 ? sec.ToString() : "0" + sec);
            yield return new WaitForSeconds(1f);
        }

        Sequence flashSeq = DOTween.Sequence();
        flashSeq.Append(flashImage.DOFade(1f, 0.05f));
        flashSeq.AppendCallback(() =>
        {
            levelList[currentLevel].pictureImage.gameObject.SetActive(true);
            levelList[currentLevel].similarityText.gameObject.SetActive(true);
            CheckPose();
            StartCoroutine(CaptureAndSetPicture());
            foreach (var tmp in levelList[currentLevel].poseEditorControllerList)
                tmp.Disable();
            Invoke("NewLevel", 5f);
        });
        flashSeq.AppendInterval(0.2f);
        flashSeq.Append(flashImage.DOFade(0f, 2f));
    }

    private IEnumerator CaptureAndSetPicture()
    {
        if (levelList[currentLevel].pictureImage == null)
        {
            Debug.LogError("[GameManager] pictureImage 未设置。");
            yield break;
        }

        GameObject[] hideObjects = GameObject.FindGameObjectsWithTag("Hide");
        for (int i = 0; i < hideObjects.Length; i++)
        {
            GameObject go = hideObjects[i];
            if (go != null && go.activeSelf)
                go.SetActive(false);
        }

        yield return new WaitForEndOfFrame();

        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();

        for (int i = 0; i < hideObjects.Length; i++)
        {
            if (hideObjects[i] != null)
                hideObjects[i].SetActive(true);
        }

        if (lastSprite != null) Destroy(lastSprite);
        if (lastTexture != null) Destroy(lastTexture);

        lastTexture = tex;
        lastSprite = Sprite.Create(
            lastTexture,
            new Rect(0, 0, lastTexture.width, lastTexture.height),
            new Vector2(0.5f, 0.5f),
            100f
        );

        levelList[currentLevel].pictureImage.sprite = lastSprite;
        levelList[currentLevel].pictureImage.preserveAspect = true;
    }

    private void OnDestroy()
    {
        if (lastSprite != null) Destroy(lastSprite);
        if (lastTexture != null) Destroy(lastTexture);
    }

    private void CheckPose()
    {
        var level = levelList[currentLevel];
        var zone = level.zone;

        if (zone == null || zone.vertices == null || zone.vertices.Count < 3)
        {
            Debug.LogWarning("[GameManager] 当前关卡的 zone 顶点不足 3 个，无法判定。");
            level.similarityText.text = "完成度：--%";
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;

        float passed = 0f;
        float total = 0f;

        foreach (var controller in level.poseEditorControllerList)
        {
            foreach (var joint in controller.Joints)
            {
                if (joint == null || joint.rect == null) continue;
                total++;

                // 把关节 rect 的世界坐标转为 Canvas anchoredPosition，再判断是否在多边形内
                Vector2 anchoredPos = WorldToAnchored(joint.rect.position, canvasRect);
                bool inside = zone.Contains(anchoredPos);

                if (inside)
                    passed++;
                else
                    Debug.Log($"[CheckPose] 关节 {joint.rect.name}  pos={anchoredPos}  不在区域内");
            }
        }

        level.similarity = total > 0f ? passed / total : 0f;
        level.similarityText.text = "完成度：" + Mathf.Round(level.similarity * 10000f) / 100f + "%";
        Debug.Log($"[GameManager] 完成度：{level.similarity * 100f:F2}%  ({passed}/{total})");
    }

    /// <summary>世界坐标 → Canvas anchoredPosition</summary>
    private Vector2 WorldToAnchored(Vector3 worldPos, RectTransform canvasRect)
    {
        if (canvasRect == null) return Vector2.zero;
        Vector3 local = canvasRect.InverseTransformPoint(worldPos);
        return new Vector2(local.x, local.y);
    }
}