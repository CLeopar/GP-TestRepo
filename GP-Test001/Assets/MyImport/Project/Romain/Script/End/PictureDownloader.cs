using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PictureDownloader : MonoBehaviour
{
    [Header("下载设置")]
    [SerializeField] private string folderName = "GameCaptures";

    [Header("下载按钮")]
    [SerializeField] private Button downloadButton;

    [Header("弹窗设置")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private float popupDuration = 2f;

    private HashSet<int> _selectedIndices = new HashSet<int>();
    private Tween _popupTween;

    private void Start()
    {
        if (downloadButton != null)
            downloadButton.interactable = false;

        if (popupPanel != null)
            popupPanel.SetActive(false);
    }

    public void SetSelectedIndices(HashSet<int> indices)
    {
        _selectedIndices = new HashSet<int>(indices);

        if (downloadButton != null)
            downloadButton.interactable = _selectedIndices.Count > 0;
    }

    public void OnDownloadClicked()
    {
        var sprites = GameManager.Instance.CapturedSprites;

        if (_selectedIndices == null || _selectedIndices.Count == 0)
        {
            ShowPopup();
            return;
        }

        if (sprites == null || sprites.Count == 0)
        {
            ShowPopup();
            return;
        }

        foreach (int index in _selectedIndices)
        {
            if (index < 0 || index >= sprites.Count || sprites[index] == null)
                continue;
            SaveTexture(sprites[index].texture, index);
        }

        ShowPopup();
    }

    private string SaveTexture(Texture2D tex, int index)
    {
        string folderPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "..", folderName)
        );

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filePath = Path.Combine(folderPath, $"GameCapture_{index}_{timestamp}.png");

        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Debug.Log($"图片已保存：{filePath}");
        return filePath;
    }

    private void ShowPopup()
    {
        if (popupPanel == null) return;

        _popupTween?.Kill(true);
        DOTween.Kill(popupPanel.transform);

        CanvasGroup cg = popupPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = popupPanel.AddComponent<CanvasGroup>();

        popupPanel.SetActive(true);
        cg.alpha = 0f;
        popupPanel.transform.localScale = Vector3.one * 0.8f;

        Sequence seq = DOTween.Sequence();
        seq.Append(cg.DOFade(1f, 0.2f));
        seq.Join(popupPanel.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack));
        seq.AppendInterval(popupDuration);
        seq.Append(cg.DOFade(0f, 0.3f));
        seq.OnComplete(() =>
        {
            popupPanel.SetActive(false);
            cg.alpha = 0f;
        });

        _popupTween = seq;
    }

    private void OnDestroy()
    {
        _popupTween?.Kill();
    }
}