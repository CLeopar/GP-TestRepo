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
    private class Level
    {
        public GameObject levelObj;
        [HideInInspector] public List<PoseEditorController> poseEditorControllerList;
        [HideInInspector] public Image pictureImage;
        [HideInInspector] public Text similarityText;
        [HideInInspector] public float similarity = 0;
        public string promptString;

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
            Debug.LogError("[GameManager] pictureImage δ���á�");
            yield break;
        }

        GameObject[] hideObjects = GameObject.FindGameObjectsWithTag("Hide");
        for (int i = 0; i < hideObjects.Length; i++)
        {
            GameObject go = hideObjects[i];
            if (go != null && go.activeSelf)
            {
                go.SetActive(false);
            }
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
        float tmpCount1 = 0, tmpCount2 = 0;
        foreach (var tmpController in levelList[currentLevel].poseEditorControllerList)
        {
            tmpCount2 += tmpController.Joints.Length;
            foreach (var tmpJoint in tmpController.Joints)
            {
                if (tmpJoint.isTranslationJoint)
                {
                    // 平移关节：判断 bodyRoot 的 Pos X / Pos Y 是否在指定区间内
                    if (tmpController.BodyRoot != null)
                    {
                        Vector2 pos = tmpController.BodyRoot.anchoredPosition;
                        bool xOk = pos.x >= tmpJoint.standardPositionXRange.x && pos.x < tmpJoint.standardPositionXRange.y;
                        bool yOk = pos.y >= tmpJoint.standardPositionYRange.x && pos.y < tmpJoint.standardPositionYRange.y;
                        if (xOk && yOk)
                        {
                            tmpCount1++;
                        }
                        else
                        {
                            Debug.Log(tmpJoint.rect.name);
                            Debug.Log(pos);
                            Debug.Log("X range: " + tmpJoint.standardPositionXRange + " Y range: " + tmpJoint.standardPositionYRange);
                        }
                    }
                }
                else
                {
                    // 旋转关节：判断 skeleton 的 Z 角是否在指定区间内
                    if (tmpJoint.skeleton.localRotation.eulerAngles.z >= tmpJoint.standardRotationZRange.x
                        && tmpJoint.skeleton.localRotation.eulerAngles.z < tmpJoint.standardRotationZRange.y)
                    {
                        tmpCount1++;
                    }
                    else
                    {
                        Debug.Log(tmpJoint.skeleton.name);
                        Debug.Log(tmpJoint.skeleton.localRotation.eulerAngles.z);
                        Debug.Log(tmpJoint.standardRotationZRange);
                    }
                }
            }
        }
        Debug.Log(tmpCount1);
        Debug.Log(tmpCount2);
        levelList[currentLevel].similarity = tmpCount1 / tmpCount2;
        levelList[currentLevel].similarityText.text = "完成度：" + Mathf.Round(levelList[currentLevel].similarity * 10000f) / 100f + "%";
    }
}