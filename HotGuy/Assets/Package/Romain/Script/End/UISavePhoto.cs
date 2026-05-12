using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class UISavePhoto : MonoBehaviour
{
    [Header("References")]
    public UIShakeManager hoverShake;

    [Header("Save Settings")]
    public string folderName = "UnityL2Photo";

    [Header("Popup")]
    public GameObject successPopup;   // 拖入弹窗的 GameObject
    public float popupDuration = 2f;  // 弹窗显示时间（秒）

    private Coroutine _popupCoroutine;

    public void SaveSelectedPhotos()
    {
        if (hoverShake == null)
        {
            Debug.LogError("UISavePhoto: hoverShake 未赋值!");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("UISavePhoto: GameManager.Instance 为空!");
            return;
        }

        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string savePath = Path.Combine(desktopPath, folderName);

        if (!Directory.Exists(savePath))
            Directory.CreateDirectory(savePath);

        List<int> selectedIndices = hoverShake.GetSelectedIndices();

        if (selectedIndices.Count == 0)
        {
            Debug.Log("UISavePhoto: 没有选中任何照片");
            return;
        }

        var textures = GameManager.Instance.CapturedTextures;
        int savedCount = 0;

        foreach (int index in selectedIndices)
        {
            if (index >= textures.Count)
            {
                Debug.LogWarning($"UISavePhoto: index {index} 超出截图列表范围（共 {textures.Count} 张）");
                continue;
            }

            Texture2D tex = textures[index];
            if (tex == null) continue;

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string fileName  = $"photo_{timestamp}.png";
            string fullPath  = Path.Combine(savePath, fileName);

            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(fullPath, bytes);
            savedCount++;

            Debug.Log($"UISavePhoto: 保存成功 -> {fullPath}");
        }

        if (savedCount > 0)
            ShowPopup();

        Debug.Log($"UISavePhoto: 共保存 {savedCount} 张照片到 {savePath}");
    }

    private void ShowPopup()
    {
        if (successPopup == null) return;

        if (_popupCoroutine != null)
            StopCoroutine(_popupCoroutine);

        _popupCoroutine = StartCoroutine(PopupCoroutine());
    }

    private System.Collections.IEnumerator PopupCoroutine()
    {
        successPopup.SetActive(true);
        yield return new WaitForSeconds(popupDuration);
        successPopup.SetActive(false);
        _popupCoroutine = null;
    }
}