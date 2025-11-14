using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// UI 클릭 브로드캐스터
/// - 터치/펜/마우스 입력을 감지해서
///   해당 프레임에 클릭된 화면 좌표 기준으로 UI 레이캐스트를 수행
/// - 최상단(맨 위에 보이는) UI GameObject를 찾아서 OnAnyUIClick 이벤트로 방송
/// - 씬 어딘가에 하나만 두고, 구독 쪽에서 "어떤 UI가 클릭됐는지" 공통 처리용으로 사용
/// </summary>
public class UiClickBroadcaster : MonoBehaviour
{
    /// <summary>
    /// 어떤 UI가 클릭되었는지 방송하는 정적 이벤트
    /// - 인자: 최상단 UI GameObject (없으면 null)
    /// - 예) UiClickBroadcaster.OnAnyUIClick += go => { ... };
    /// </summary>
    public static event Action<GameObject> OnAnyUIClick; // 최상단 UI 오브젝트(없으면 null)

    [SerializeField]
    private bool _enableLog = false;   // true면 클릭 시 디버그 로그 출력

    private EventSystem _eventSystem;  // 현재 씬의 EventSystem
    private PointerEventData _ped;     // 레이캐스트용 PointerEventData
    private readonly List<RaycastResult> _results = new(); // 레이캐스트 결과 리스트

    /// <summary>
    /// 초기화: EventSystem 확보 및 PointerEventData 생성
    /// </summary>
    private void Awake()
    {
        //Debug.Log(gameObject.name);
        _eventSystem = EventSystem.current;
        if (_eventSystem == null)
            Debug.LogError("[UiClickBroadcaster] No EventSystem in scene (need InputSystemUIInputModule).");

        _ped = new PointerEventData(_eventSystem);
    }

    /// <summary>
    /// 매 프레임 입력을 확인해서 "이번 프레임에 클릭/터치 Down이 있었는지" 체크
    /// - 있었다면 해당 좌표 기준으로 UI 레이캐스트 수행 후
    ///   최상단 UI GameObject를 OnAnyUIClick 이벤트로 알림
    /// </summary>
    private void Update()
    {
        if (!TryGetPointerDownPosition(out var pos)) return;

        _results.Clear();
        _ped.position = pos;

        // 씬 내 모든 GraphicRaycaster 대상으로 레이캐스트 수행
        _eventSystem.RaycastAll(_ped, _results);

        // 결과가 있으면 가장 위(0번)는 정렬된 최상단 히트 오브젝트
        GameObject top = _results.Count > 0 ? _results[0].gameObject : null;

        if (_enableLog)
        {
            var name = top ? top.name : "(null)";
            Debug.Log($"[UiClickBroadcaster] Down @ {pos} => top: {name} (hits={_results.Count})");
        }

        OnAnyUIClick?.Invoke(top);
    }

    /// <summary>
    /// 입력 장치별로 "이번 프레임에 Down이 발생한 위치"를 가져오는 함수
    /// - 우선순위: 터치 > 펜 > 마우스
    /// - Down이 없으면 false 반환
    /// </summary>
    private bool TryGetPointerDownPosition(out Vector2 pos)
    {
        // Touch (터치)
        if (Touchscreen.current != null)
        {
            var t = Touchscreen.current.primaryTouch;
            if (t.press.wasPressedThisFrame)
            {
                pos = t.position.ReadValue();
                return true;
            }
        }

        // Pen (펜 입력)
        if (Pen.current != null && Pen.current.tip.wasPressedThisFrame)
        {
            pos = Pen.current.position.ReadValue();
            return true;
        }

        // Mouse (마우스 왼쪽 버튼)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            pos = Mouse.current.position.ReadValue();
            return true;
        }

        pos = default;
        return false;
    }
}
