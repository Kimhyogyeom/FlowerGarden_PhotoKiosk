using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UiClickBroadcaster : MonoBehaviour
{
    public static event Action<GameObject> OnAnyUIClick; // 최상단 UI 오브젝트(없으면 null)
    [SerializeField] private bool _enableLog = false;

    private EventSystem _eventSystem;
    private PointerEventData _ped;
    private readonly List<RaycastResult> _results = new();

    private void Awake()
    {
        //Debug.Log(gameObject.name);
        _eventSystem = EventSystem.current;
        if (_eventSystem == null)
            Debug.LogError("[UiClickBroadcaster] No EventSystem in scene (need InputSystemUIInputModule).");

        _ped = new PointerEventData(_eventSystem);
    }

    private void Update()
    {
        if (!TryGetPointerDownPosition(out var pos)) return;

        _results.Clear();
        _ped.position = pos;

        // 씬 내 모든 GraphicRaycaster 대상으로 레이캐스트
        _eventSystem.RaycastAll(_ped, _results);

        // 결과가 있으면 가장 위(0번)는 정렬된 최상단 히트
        GameObject top = _results.Count > 0 ? _results[0].gameObject : null;

        if (_enableLog)
        {
            var name = top ? top.name : "(null)";
            Debug.Log($"[UiClickBroadcaster] Down @ {pos} => top: {name} (hits={_results.Count})");
        }

        OnAnyUIClick?.Invoke(top);
    }

    // 터치 > 펜 > 마우스 순으로 좌표/클릭 판정
    private bool TryGetPointerDownPosition(out Vector2 pos)
    {
        // Touch
        if (Touchscreen.current != null)
        {
            var t = Touchscreen.current.primaryTouch;
            if (t.press.wasPressedThisFrame)
            {
                pos = t.position.ReadValue();
                return true;
            }
        }
        // Pen
        if (Pen.current != null && Pen.current.tip.wasPressedThisFrame)
        {
            pos = Pen.current.position.ReadValue();
            return true;
        }
        // Mouse
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            pos = Mouse.current.position.ReadValue();
            return true;
        }

        pos = default;
        return false;
    }
}
