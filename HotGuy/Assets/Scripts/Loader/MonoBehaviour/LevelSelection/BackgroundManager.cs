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

    [Header("Tile Settings")]
    [Tooltip("屏幕竖直方向显示多少个tile（瓦片），比如4表示一屏高显示4个正方形tile")]
    [SerializeField] private float _tilesPerScreenHeight = 4f;
    [Tooltip("Tile 大小的额外缩放系数，1为默认值，大于1则tile更小更密，小于1则tile更大更疏")]
    [SerializeField] private float _tileScale = 1f;

    [Header("CardSelector")] 
    [SerializeField] private CardSelector _cardSelector;

    private int _currentIndex = 0;
    private bool _isTransitioning = false;
    private float _screenAspect;
    private Vector2 _uvSize;

    void Start()
    {
        if (_backgrounds.Count == 0) return;
        
        // 关键：确保所有背景图的 Wrap Mode 为 Repeat，否则平铺会拉伸
        foreach (var tex in _backgrounds)
        {
            if (tex != null) tex.wrapMode = TextureWrapMode.Repeat;
        }
        
        _frontLayer.texture = _backgrounds[0];
        SetAlpha(_backLayer, 0f);
        
        UpdateScreenAspect();
        RecalculateUvSize();
    }

    void Update()
    {
        if (_backgrounds.Count == 0) return;

        // 编辑器或运行时窗口大小变化时，重新计算 UV 尺寸
        float currentAspect = (float)Screen.width / Screen.height;
        if (Mathf.Abs(_screenAspect - currentAspect) > 0.001f)
        {
            UpdateScreenAspect();
            RecalculateUvSize();
        }

        // 背景无限滚动
        Vector2 scroll = new Vector2(_scrollX, _scrollY) * Time.deltaTime;
        _frontLayer.uvRect = new Rect(_frontLayer.uvRect.position + scroll, _frontLayer.uvRect.size);
        _backLayer.uvRect = new Rect(_backLayer.uvRect.position + scroll, _backLayer.uvRect.size);

        // 监听卡片索引变化
        if (_cardSelector != null)
        {
            int cardIndex = _cardSelector.GetCurrentIndex();
            if (cardIndex != _currentIndex && !_isTransitioning)
            {
                int bgIndex = Mathf.Clamp(cardIndex, 0, _backgrounds.Count - 1);
                StartCoroutine(TransitionTo(bgIndex));
            }
        }
    }

    private void UpdateScreenAspect()
    {
        _screenAspect = (float)Screen.width / Screen.height;
    }

    private void RecalculateUvSize()
    {
        if (_frontLayer.texture == null) return;
        
        float texAspect = (float)_frontLayer.texture.width / _frontLayer.texture.height;
        
        // UV 高度 = 一屏竖直方向要显示多少个 tile
        float uvHeight = _tilesPerScreenHeight * _tileScale;
        
        // UV 宽度根据屏幕比例和图片比例计算，保证 tile 显示比例正确
        // 公式推导：
        // (Screen.width / uvWidth) / (Screen.height / uvHeight) = texture.width / texture.height
        // 即每个 tile 的屏幕显示宽高比 = 图片原始宽高比
        float uvWidth = uvHeight * _screenAspect / texAspect;
        
        _uvSize = new Vector2(uvWidth, uvHeight);
        _frontLayer.uvRect = new Rect(_frontLayer.uvRect.position, _uvSize);
        
        if (_backLayer.texture != null)
        {
            _backLayer.uvRect = new Rect(_backLayer.uvRect.position, _uvSize);
        }
    }

    private IEnumerator TransitionTo(int nextIndex)
    {
        _isTransitioning = true;
        _currentIndex = nextIndex;

        _backLayer.texture = _backgrounds[nextIndex];
        if (_backgrounds[nextIndex] != null)
            _backgrounds[nextIndex].wrapMode = TextureWrapMode.Repeat;
        
        // 切换时同步 UV 位置和大小，保证两张图完全重合
        _backLayer.uvRect = new Rect(_frontLayer.uvRect.position, _uvSize);
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

    // 运行时动态调整 tile 密度
    public void SetTileDensity(float tilesPerScreenHeight)
    {
        _tilesPerScreenHeight = Mathf.Max(0.1f, tilesPerScreenHeight);
        RecalculateUvSize();
    }

    // 运行时动态调整 tile 缩放
    public void SetTileScale(float scale)
    {
        _tileScale = Mathf.Max(0.1f, scale);
        RecalculateUvSize();
    }
}