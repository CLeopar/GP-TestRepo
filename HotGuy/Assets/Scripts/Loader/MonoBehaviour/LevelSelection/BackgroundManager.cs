using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundManager : MonoBehaviour
{
    [SerializeField] private RawImage _frontLayer;
    [SerializeField] private RawImage _backLayer;
    [SerializeField] private List<Texture> _backgrounds;
    [SerializeField] private float _scrollX, _scrollY;
    [SerializeField] private float _transitionDuration = 1.5f;

    [Header("CardSelector")] [SerializeField]
    private CardSelector _cardSelector; // 拖拽到此处

    private int _currentIndex = 0;
    private bool _isTransitioning = false;

    void Start()
    {
        if (_backgrounds.Count == 0) return;
        _frontLayer.texture = _backgrounds[0];
        SetAlpha(_backLayer, 0f);
    }

    void Update()
    {
        if (_backgrounds.Count == 0) return;

        // 背景滚动
        Vector2 scroll = new Vector2(_scrollX, _scrollY) * Time.deltaTime;
        _frontLayer.uvRect = new Rect(_frontLayer.uvRect.position + scroll, _frontLayer.uvRect.size);
        _backLayer.uvRect = new Rect(_backLayer.uvRect.position + scroll, _backLayer.uvRect.size);

        // 监听卡片索引变化
        if (_cardSelector != null)
        {
            int cardIndex = _cardSelector.GetCurrentIndex();
            if (cardIndex != _currentIndex && !_isTransitioning)
            {
                // 确保索引在背景列表范围内
                int bgIndex = Mathf.Clamp(cardIndex, 0, _backgrounds.Count - 1);
                StartCoroutine(TransitionTo(bgIndex));
            }
        }
    }

    private IEnumerator TransitionTo(int nextIndex)
    {
        _isTransitioning = true;
        _currentIndex = nextIndex; // 提前更新，防止 Update 重复触发

        _backLayer.texture = _backgrounds[nextIndex];
        _backLayer.uvRect = new Rect(_frontLayer.uvRect.position, _frontLayer.uvRect.size);
        SetAlpha(_backLayer, 0f);

        float elapsed = 0f;
        while (elapsed < _transitionDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(_backLayer, Mathf.Clamp01(elapsed / _transitionDuration));
            yield return null;
        }

        _frontLayer.texture = _backgrounds[nextIndex];
        _frontLayer.uvRect = _backLayer.uvRect;
        SetAlpha(_frontLayer, 1f);
        SetAlpha(_backLayer, 0f);

        _isTransitioning = false;
    }

    private void SetAlpha(RawImage img, float alpha)
    {
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }
}