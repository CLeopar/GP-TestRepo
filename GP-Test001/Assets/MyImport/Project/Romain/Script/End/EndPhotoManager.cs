using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class EndPhotoManager : MonoBehaviour
{
    [System.Serializable]
    public class ProjectCard
    {
        public RectTransform card;
        public GameObject outlineImage;
    }

    [Header("项目卡片配置")]
    [SerializeField] private ProjectCard[] projectCards;

    [Header("Camera 模式必填")]
    [SerializeField] private Camera uiCamera;

    [Header("抖动设置")]
    [SerializeField] private float shakeStrength = 8f;
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeInterval = 0.5f;

    [Header("下载功能")]
    [SerializeField] private PictureDownloader pictureDownloader;

    private ProjectCard _currentHovered;
    private Tween _shakeTween;
    private Vector2 _originAnchorPos;
    private bool _isShaking = false;
    private bool _justEnteredCard = false;

    private HashSet<int> _selectedIndices = new HashSet<int>();

    private void Start()
    {
        foreach (var p in projectCards)
        {
            if (p.outlineImage != null)
                p.outlineImage.SetActive(false);
        }
    }

    private void Update()
    {
        ProjectCard hovered = GetHoveredCard();

        if (hovered != _currentHovered)
        {
            if (_currentHovered != null)
                StopShake(_currentHovered);

            _currentHovered = hovered;
            _justEnteredCard = true;

            if (_currentHovered != null)
            {
                _originAnchorPos = _currentHovered.card.anchoredPosition;
                _isShaking = true;
                ScheduleShake();
            }
        }
        else
        {
            _justEnteredCard = false;
        }

        if (Input.GetMouseButtonDown(0) && hovered != null && !_justEnteredCard)
            HandleClick(hovered);
    }

    private ProjectCard GetHoveredCard()
    {
        foreach (var p in projectCards)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(p.card, Input.mousePosition, uiCamera))
                return p;
        }
        return null;
    }

    private void HandleClick(ProjectCard card)
    {
        int index = -1;
        for (int i = 0; i < projectCards.Length; i++)
        {
            if (projectCards[i] == card)
            {
                index = i;
                break;
            }
        }

        if (index < 0) return;

        if (_selectedIndices.Contains(index))
        {
            // 取消选中
            _selectedIndices.Remove(index);
            card.outlineImage?.SetActive(false);
        }
        else
        {
            // 选中
            _selectedIndices.Add(index);
            card.outlineImage?.SetActive(true);
        }

        pictureDownloader?.SetSelectedIndices(_selectedIndices);
    }

    private void ScheduleShake()
    {
        if (!_isShaking || _currentHovered == null) return;
        _shakeTween?.Kill();

        _shakeTween = _currentHovered.card
            .DOShakeAnchorPos(shakeDuration, shakeStrength, 10, 90)
            .OnComplete(() =>
            {
                if (_currentHovered != null)
                    _currentHovered.card.anchoredPosition = _originAnchorPos;

                DOVirtual.DelayedCall(shakeInterval, () =>
                {
                    if (_isShaking) ScheduleShake();
                });
            });
    }

    private void StopShake(ProjectCard card)
    {
        _isShaking = false;
        _shakeTween?.Kill();
        card.card.DOAnchorPos(_originAnchorPos, 0.1f).SetEase(Ease.OutQuad);
    }

    private void OnDestroy()
    {
        _shakeTween?.Kill();
    }
}