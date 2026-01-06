using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 스티커 삭제 영역 (휴지통)
/// - 스티커를 드래그해서 이 영역에 드롭하면 삭제됨
/// - 휴지통 이미지나 버튼에 붙여서 사용
/// </summary>
public class DeleteZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Settings")]
    [Tooltip("삭제 시 효과음 재생 여부")]
    [SerializeField] private bool _playSoundOnDelete = true;

    [Header("Hover Animation")]
    [Tooltip("호버 시 스케일 배율")]
    [SerializeField] private float _hoverScale = 2f;
    [Tooltip("스케일 애니메이션 속도")]
    [SerializeField] private float _animationSpeed = 10f;

    private Vector3 _originalScale;
    private Vector3 _targetScale;

    private void Awake()
    {
        _originalScale = transform.localScale;
        _targetScale = _originalScale;
    }

    private void Update()
    {
        // 부드러운 스케일 애니메이션
        if (transform.localScale != _targetScale)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * _animationSpeed);
        }
    }

    /// <summary>
    /// 드래그 중인 오브젝트가 영역에 들어왔을 때
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 드래그 중일 때만 반응
        if (eventData.pointerDrag == null) return;
        if (eventData.pointerDrag.GetComponent<Draggable>() == null) return;

        _targetScale = _originalScale * _hoverScale;
    }

    /// <summary>
    /// 드래그 중인 오브젝트가 영역을 벗어났을 때
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        _targetScale = _originalScale;
    }

    /// <summary>
    /// IDropHandler 구현 - 스티커가 드롭되었을 때 호출
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        var draggable = eventData.pointerDrag.GetComponent<Draggable>();
        if (draggable == null) return;

        // 삭제 예정 표시 (OnEndDrag에서 중복 삭제 방지)
        draggable.MarkAsDeleted();

        // DropZone에서 스티커 제거
        var dropZone = draggable.GetCurrentDropZone();
        if (dropZone != null)
        {
            dropZone.RemoveSticker(eventData.pointerDrag);
        }

        // 스티커 삭제
        // Debug.Log($"[DeleteZone] 스티커 삭제: {eventData.pointerDrag.name}");
        Destroy(eventData.pointerDrag);

        // 효과음 재생
        if (_playSoundOnDelete && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._buttonClickSound);
        }
    }
}
