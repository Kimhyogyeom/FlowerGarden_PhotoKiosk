using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 스티커 스케일 조절 슬라이더
/// - 스티커 드롭 시 옆에 생성됨
/// - 위로 드래그하면 확대, 아래로 드래그하면 축소
/// - 다른 스티커 드롭 시 기존 슬라이더는 사라지고 새 스티커에 생성
/// </summary>
public class StickerScaleSlider : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Scale Settings")]
    [Tooltip("최소 스케일 배율")]
    [SerializeField] private float _minScale = 0.3f;

    [Tooltip("최대 스케일 배율")]
    [SerializeField] private float _maxScale = 4.0f;

    [Header("Visual Settings")]
    [Tooltip("슬라이더 너비")]
    [SerializeField] private float _sliderWidth = 40f;

    [Tooltip("슬라이더 높이")]
    [SerializeField] private float _sliderHeight = 150f;

    [Tooltip("스티커로부터의 오프셋 (오른쪽)")]
    [SerializeField] private float _offsetX = 60f;

    // 대상 스티커
    private RectTransform _targetSticker;
    private Vector3 _initialScale;
    private float _currentScaleMultiplier = 1f;

    // 초기 스티커 크기 (확대 전 기준으로 오프셋 계산용)
    private float _initialStickerWidth;

    // 드래그 상태
    private Canvas _parentCanvas;
    private Camera _uiCamera;
    private float _dragStartLocalY;
    private float _scaleAtDragStart;

    // UI 컴포넌트
    private RectTransform _rectTransform;
    private Image _backgroundImage;
    private Image _handleImage;
    private RectTransform _handleRect;

    // 슬라이더 트랙과 핸들
    private GameObject _track;
    private GameObject _handle;

    /// <summary>
    /// 슬라이더 생성 및 초기화
    /// </summary>
    public static StickerScaleSlider Create(RectTransform targetSticker, Transform parent)
    {
        // 슬라이더 루트 오브젝트 생성
        GameObject sliderObj = new GameObject("StickerScaleSlider");
        sliderObj.transform.SetParent(parent, false);

        var slider = sliderObj.AddComponent<StickerScaleSlider>();
        slider.Initialize(targetSticker);

        return slider;
    }

    private void Initialize(RectTransform targetSticker)
    {
        _targetSticker = targetSticker;
        _initialScale = targetSticker.localScale;
        _currentScaleMultiplier = 1f;

        // 초기 스티커 크기 저장 (확대 전 기준으로 오프셋 계산에 사용)
        _initialStickerWidth = targetSticker.rect.width * targetSticker.lossyScale.x;

        // Canvas 및 카메라 참조 저장
        _parentCanvas = GetComponentInParent<Canvas>();
        if (_parentCanvas != null)
        {
            _uiCamera = _parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _parentCanvas.worldCamera;
        }

        _rectTransform = gameObject.AddComponent<RectTransform>();

        // 슬라이더 크기 설정
        _rectTransform.sizeDelta = new Vector2(_sliderWidth, _sliderHeight);

        // 위치 설정 (스티커 오른쪽)
        UpdatePosition();

        // UI 생성
        CreateSliderUI();
    }

    private void CreateSliderUI()
    {
        // 배경 트랙
        _track = new GameObject("Track");
        _track.transform.SetParent(transform, false);

        var trackRect = _track.AddComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0.5f, 0f);
        trackRect.anchorMax = new Vector2(0.5f, 1f);
        trackRect.pivot = new Vector2(0.5f, 0.5f);
        trackRect.sizeDelta = new Vector2(10f, _sliderHeight - 20f);
        trackRect.anchoredPosition = Vector2.zero;

        var trackImage = _track.AddComponent<Image>();
        trackImage.color = new Color(0f, 0f, 0f, 1f);  // 검은색 알파 1

        // 흰색 테두리 추가
        var trackOutline = _track.AddComponent<Outline>();
        trackOutline.effectColor = Color.white;
        trackOutline.effectDistance = new Vector2(2f, 2f);

        // 핸들
        _handle = new GameObject("Handle");
        _handle.transform.SetParent(transform, false);

        _handleRect = _handle.AddComponent<RectTransform>();
        _handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        _handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        _handleRect.pivot = new Vector2(0.5f, 0.5f);
        _handleRect.sizeDelta = new Vector2(_sliderWidth, 30f);
        _handleRect.anchoredPosition = Vector2.zero;

        _handleImage = _handle.AddComponent<Image>();
        _handleImage.color = new Color(0f, 0f, 0f, 1f);  // 검은색 알파 1

        // 흰색 테두리 추가
        var handleOutline = _handle.AddComponent<Outline>();
        handleOutline.effectColor = Color.white;
        handleOutline.effectDistance = new Vector2(2f, 2f);

        // 위/아래 화살표 아이콘 (텍스트로 대체)
        CreateArrowIcon(true);  // 위 화살표
        CreateArrowIcon(false); // 아래 화살표
    }

    private void CreateArrowIcon(bool isUp)
    {
        GameObject arrow = new GameObject(isUp ? "ArrowUp" : "ArrowDown");
        arrow.transform.SetParent(transform, false);

        var arrowRect = arrow.AddComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(0.5f, isUp ? 1f : 0f);
        arrowRect.anchorMax = new Vector2(0.5f, isUp ? 1f : 0f);
        arrowRect.pivot = new Vector2(0.5f, isUp ? 1f : 0f);
        arrowRect.sizeDelta = new Vector2(30f, 20f);
        arrowRect.anchoredPosition = new Vector2(0f, isUp ? -5f : 5f);

        var text = arrow.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = isUp ? "▲" : "▼";
        text.fontSize = 16f;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.color = Color.white;
    }

    private void Update()
    {
        // 대상 스티커가 삭제되면 슬라이더도 삭제
        if (_targetSticker == null)
        {
            Destroy(gameObject);
            return;
        }

        // 스티커 위치 따라가기
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (_targetSticker == null || _rectTransform == null) return;

        // 스티커의 월드 위치를 기준으로 슬라이더 위치 계산
        Vector3 stickerPos = _targetSticker.position;

        // 초기 스티커 크기 기준으로 고정 오프셋 계산 (스티커 확대해도 슬라이더 위치 고정)
        float offsetWorld = (_initialStickerWidth / 2f) + _offsetX;

        _rectTransform.position = stickerPos + new Vector3(offsetWorld, 0f, 0f);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 터치 시작 시 현재 스케일과 터치 위치 저장
        _scaleAtDragStart = _currentScaleMultiplier;

        // 터치 시작 위치를 로컬 좌표로 저장
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform, eventData.position, _uiCamera, out localPoint))
        {
            _dragStartLocalY = localPoint.y;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_targetSticker == null || _rectTransform == null) return;

        // 현재 터치 위치를 로컬 좌표로 변환
        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform, eventData.position, _uiCamera, out localPoint))
        {
            return;
        }

        // 드래그 시작점으로부터의 이동 거리 계산
        float deltaY = localPoint.y - _dragStartLocalY;

        // 슬라이더 높이 기준으로 스케일 변화량 계산
        // 가운데 = 1.0x 기준, 위로 올리면 확대, 아래로 내리면 축소
        float trackHeight = _sliderHeight - 40f;
        float halfTrack = trackHeight / 2f;

        // 위쪽 절반: 1.0x ~ maxScale, 아래쪽 절반: minScale ~ 1.0x
        // deltaY를 기준으로 스케일 변화량 계산
        float scaleChange;
        if (deltaY >= 0)
        {
            // 위로 드래그: 1.0x에서 maxScale까지
            scaleChange = (deltaY / halfTrack) * (_maxScale - 1f);
        }
        else
        {
            // 아래로 드래그: 1.0x에서 minScale까지
            scaleChange = (deltaY / halfTrack) * (1f - _minScale);
        }

        // 새 스케일 적용 (시작 스케일 + 변화량)
        _currentScaleMultiplier = Mathf.Clamp(_scaleAtDragStart + scaleChange, _minScale, _maxScale);

        // 스티커 스케일 적용
        _targetSticker.localScale = _initialScale * _currentScaleMultiplier;

        // 핸들 위치 업데이트 (시각적 피드백)
        UpdateHandlePosition();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 드래그 종료 시 특별한 처리 없음
    }

    private void UpdateHandlePosition()
    {
        if (_handleRect == null) return;

        // 가운데 = 1.0x 기준으로 핸들 위치 계산
        float halfHeight = (_sliderHeight - 40f) / 2f;
        float handleY;

        if (_currentScaleMultiplier >= 1f)
        {
            // 1.0x ~ maxScale: 가운데(0) ~ 위(+halfHeight)
            float normalized = (_currentScaleMultiplier - 1f) / (_maxScale - 1f);
            handleY = Mathf.Lerp(0f, halfHeight, normalized);
        }
        else
        {
            // minScale ~ 1.0x: 아래(-halfHeight) ~ 가운데(0)
            float normalized = (_currentScaleMultiplier - _minScale) / (1f - _minScale);
            handleY = Mathf.Lerp(-halfHeight, 0f, normalized);
        }

        _handleRect.anchoredPosition = new Vector2(0f, handleY);
    }

    /// <summary>
    /// 현재 스케일 배율 반환
    /// </summary>
    public float GetCurrentScale()
    {
        return _currentScaleMultiplier;
    }

    /// <summary>
    /// 대상 스티커 반환
    /// </summary>
    public RectTransform GetTargetSticker()
    {
        return _targetSticker;
    }

    /// <summary>
    /// 슬라이더 삭제
    /// </summary>
    public void Remove()
    {
        Destroy(gameObject);
    }
}
