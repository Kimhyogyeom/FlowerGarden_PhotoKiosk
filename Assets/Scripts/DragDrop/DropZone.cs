using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 스티커 드롭 영역
/// - 스티커가 드롭되면 프레임의 자식으로 추가
/// - 드롭된 스티커 목록 관리
/// - 리셋 시 모든 스티커 삭제
/// </summary>
public class DropZone : MonoBehaviour, IDropHandler
{
    [Header("Drop Zone Settings")]
    [Tooltip("드롭된 스티커의 부모가 될 Transform (없으면 자기 자신)")]
    [SerializeField] private RectTransform _stickerParent;

    // 드롭된 스티커 목록
    private List<GameObject> _droppedStickers = new List<GameObject>();

    private RectTransform _rectTransform;
    private Camera _uiCamera;
    private Canvas _canvas;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null)
        {
            _uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        }

        // 스티커 부모가 없으면 자기 자신 사용
        if (_stickerParent == null)
        {
            _stickerParent = _rectTransform;
        }
    }

    /// <summary>
    /// IDropHandler 구현 - 스티커가 드롭되었을 때 호출
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        var draggable = eventData.pointerDrag.GetComponent<Draggable>();
        if (draggable == null) return;

        // 드롭된 스티커를 프레임 자식으로 설정
        GameObject sticker = eventData.pointerDrag;
        sticker.transform.SetParent(_stickerParent, true);

        // 드롭 위치를 로컬 좌표로 변환
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _stickerParent, eventData.position, _uiCamera, out localPoint))
        {
            var stickerRect = sticker.GetComponent<RectTransform>();
            stickerRect.anchoredPosition = localPoint;
        }

        // 목록에 추가
        if (!_droppedStickers.Contains(sticker))
        {
            _droppedStickers.Add(sticker);
        }

        // Draggable에게 드롭 성공 알림
        draggable.OnDroppedToZone(this);

        // Debug.Log($"[DropZone] 스티커 드롭 완료: {sticker.name}, 총 {_droppedStickers.Count}개");
    }

    /// <summary>
    /// 스티커 목록에서 제거 (삭제 시 호출)
    /// </summary>
    public void RemoveSticker(GameObject sticker)
    {
        if (_droppedStickers.Contains(sticker))
        {
            _droppedStickers.Remove(sticker);
            // Debug.Log($"[DropZone] 스티커 제거: {sticker.name}, 남은 개수: {_droppedStickers.Count}");
        }
    }

    /// <summary>
    /// 모든 스티커 삭제 (리셋 시 호출)
    /// </summary>
    public void ClearAllStickers()
    {
        // Debug.Log($"[DropZone] 모든 스티커 삭제 시작, 총 {_droppedStickers.Count}개");

        foreach (var sticker in _droppedStickers)
        {
            if (sticker != null)
            {
                Destroy(sticker);
            }
        }

        _droppedStickers.Clear();

        // Debug.Log("[DropZone] 모든 스티커 삭제 완료");
    }

    /// <summary>
    /// 드롭된 스티커 개수 반환
    /// </summary>
    public int GetStickerCount()
    {
        return _droppedStickers.Count;
    }

    /// <summary>
    /// 드롭된 스티커 목록 반환 (읽기 전용)
    /// </summary>
    public IReadOnlyList<GameObject> GetDroppedStickers()
    {
        return _droppedStickers.AsReadOnly();
    }

    /// <summary>
    /// 스티커 부모 Transform 반환 (프린트 시 사용)
    /// </summary>
    public RectTransform GetStickerParent()
    {
        return _stickerParent;
    }

    /// <summary>
    /// 스티커 부모 Transform 설정 (런타임에서 사용)
    /// </summary>
    public void SetStickerParent(RectTransform parent)
    {
        _stickerParent = parent != null ? parent : _rectTransform;
    }

    /// <summary>
    /// 초기화 (런타임에서 AddComponent 후 호출)
    /// </summary>
    public void Initialize(RectTransform stickerParent = null)
    {
        if (_rectTransform == null)
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        if (_canvas == null)
        {
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null)
            {
                _uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            }
        }

        _stickerParent = stickerParent != null ? stickerParent : _rectTransform;
    }
}
