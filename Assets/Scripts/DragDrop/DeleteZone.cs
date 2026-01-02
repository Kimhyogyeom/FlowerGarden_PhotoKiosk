using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 스티커 삭제 영역 (휴지통)
/// - 스티커를 드래그해서 이 영역에 드롭하면 삭제됨
/// - 휴지통 이미지나 버튼에 붙여서 사용
/// </summary>
public class DeleteZone : MonoBehaviour, IDropHandler
{
    [Header("Settings")]
    [Tooltip("삭제 시 효과음 재생 여부")]
    [SerializeField] private bool _playSoundOnDelete = true;

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
