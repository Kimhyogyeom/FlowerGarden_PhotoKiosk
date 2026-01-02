using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 드래그 가능한 스티커 오브젝트
/// - 드래그 시작 시 복제본 생성 후 드래그
/// - DropZone에 드롭되면 자식으로 추가
/// - DropZone 밖에 드롭되면 삭제
/// - 드롭된 스티커도 재드래그 가능
/// </summary>
public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Settings")]
    [Tooltip("드래그 시 사용할 Canvas (없으면 자동 탐색)")]
    [SerializeField] private Canvas _canvas;

    // 컴포넌트 캐싱
    private RectTransform _rectTransform;
    private Image _image;
    private CanvasGroup _canvasGroup;

    // 상태 플래그
    private bool _isSpawned = false;        // 복제된 인스턴스인지 여부
    private bool _isDragging = false;       // 현재 드래그 중인지
    private bool _isDroppedToZone = false;  // DropZone에 드롭되었는지
    private bool _isDeleted = false;        // DeleteZone에서 삭제 예정인지

    // 드롭된 DropZone 참조
    private DropZone _currentDropZone;

    // 드래그 중인 복제본 (원본에서 관리)
    private GameObject _dragClone;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _image = GetComponent<Image>();

        // Canvas 자동 탐색
        if (_canvas == null)
        {
            _canvas = GetComponentInParent<Canvas>();
        }

        // CanvasGroup 추가 (raycast 제어용)
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    /// <summary>
    /// "(Clone)" 제거
    /// </summary>
    private string BaseName(string n)
    {
        return n.Replace("(Clone)", "").Trim();
    }

    /// <summary>
    /// 복제본으로 설정 (드래그 가능 상태)
    /// </summary>
    public void SetAsSpawned()
    {
        _isSpawned = true;
        _isDroppedToZone = false;
    }

    /// <summary>
    /// 드래그 시작
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 원본인 경우: 복제본 생성
        if (!_isSpawned)
        {
            // 복제본 생성 (Canvas 자식으로)
            _dragClone = Instantiate(gameObject, _canvas.transform);
            _dragClone.name = BaseName(gameObject.name);

            // 복제본에게 스폰 상태 알림
            var cloneDraggable = _dragClone.GetComponent<Draggable>();
            if (cloneDraggable != null)
            {
                cloneDraggable.SetAsSpawned();
                cloneDraggable._isDragging = true;
                cloneDraggable._canvasGroup.blocksRaycasts = false;

                if (cloneDraggable._image != null)
                {
                    cloneDraggable._image.raycastTarget = false;
                }
            }

            // 복제본 위치를 마우스 위치로
            var cloneRect = _dragClone.GetComponent<RectTransform>();
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint);
            cloneRect.anchoredPosition = localPoint;

            // Debug.Log($"[Draggable] 스티커 복제 및 드래그 시작: {_dragClone.name}");

            // 원본은 드래그 이벤트를 복제본에게 넘김
            eventData.pointerDrag = _dragClone;
            return;
        }

        // 이미 스폰된 복제본인 경우: 직접 드래그
        _isDragging = true;
        _isDroppedToZone = false;

        // 드래그 시작 시 Canvas 자식으로 이동 (좌표계 통일)
        // 현재 월드 위치를 유지하면서 부모 변경
        transform.SetParent(_canvas.transform, true);

        // Canvas 기준 로컬 좌표로 변환
        Vector2 canvasLocalPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out canvasLocalPoint);
        _rectTransform.anchoredPosition = canvasLocalPoint;

        // Raycast 차단 해제 (DropZone 감지를 위해)
        _canvasGroup.blocksRaycasts = false;

        if (_image != null)
        {
            _image.raycastTarget = false;
        }

        // Debug.Log($"[Draggable] 드래그 시작: {gameObject.name}");
    }

    /// <summary>
    /// 드래그 중 - 마우스 따라다니기
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        // 원본은 처리하지 않음
        if (!_isSpawned) return;
        if (!_isDragging) return;

        // Canvas 기준으로 위치 계산
        var canvasRect = _canvas.transform as RectTransform;
        if (canvasRect == null) return;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint))
        {
            _rectTransform.anchoredPosition = localPoint;
        }
    }

    /// <summary>
    /// 드래그 종료
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        // 원본은 처리하지 않음
        if (!_isSpawned) return;

        _isDragging = false;

        // Raycast 다시 활성화
        _canvasGroup.blocksRaycasts = true;

        if (_image != null)
        {
            _image.raycastTarget = true;
        }

        // DeleteZone에서 이미 삭제 처리된 경우 스킵
        if (_isDeleted) return;

        // DropZone에 드롭되지 않았으면 삭제
        if (!_isDroppedToZone)
        {
            // 기존 DropZone에서 제거
            if (_currentDropZone != null)
            {
                _currentDropZone.RemoveSticker(gameObject);
                _currentDropZone = null;
            }

            Destroy(gameObject);
            // Debug.Log($"[Draggable] DropZone 밖 - 스티커 삭제");
        }
        // else
        // {
        //     Debug.Log($"[Draggable] 드래그 종료: {gameObject.name}");
        // }
    }

    /// <summary>
    /// DropZone에 드롭되었을 때 호출 (DropZone에서 호출)
    /// </summary>
    public void OnDroppedToZone(DropZone dropZone)
    {
        _isDroppedToZone = true;

        // 이전 DropZone에서 제거
        if (_currentDropZone != null && _currentDropZone != dropZone)
        {
            _currentDropZone.RemoveSticker(gameObject);
        }

        _currentDropZone = dropZone;
    }

    /// <summary>
    /// 현재 연결된 DropZone 반환
    /// </summary>
    public DropZone GetCurrentDropZone()
    {
        return _currentDropZone;
    }

    /// <summary>
    /// DeleteZone에서 삭제 예정으로 표시 (OnEndDrag에서 중복 삭제 방지)
    /// </summary>
    public void MarkAsDeleted()
    {
        _isDeleted = true;
    }
}
