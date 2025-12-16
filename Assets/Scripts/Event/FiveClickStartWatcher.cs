using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class FiveClickStartWatcher : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PaymentHttpTester paymentHttpTester;

    [Header("Target")]
    [SerializeField] private Button _targetButton;

    [Header("Settings")]
    [SerializeField, Min(1)] private int _requiredClicks = 5;
    [SerializeField] private bool _resetCountAfterStart = true;

    [Header("Events (Optional)")]
    [SerializeField] private UnityEvent _onStart;

    private int _count = 0;
    private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>(32);

    private void Awake()
    {
        if (_targetButton == null)
            Debug.LogWarning("[FiveClickStartWatcher] Target Button is not assigned.");

        if (EventSystem.current == null)
            Debug.LogWarning("[FiveClickStartWatcher] EventSystem이 씬에 없습니다. (UI가 Raycast 안될 수 있음)");
    }

    private void Update()
    {
        if (_targetButton == null) return;
        if (EventSystem.current == null) return;

        // 입력(클릭/터치) 시작 프레임만 처리
        if (!IsPointerDownThisFrame(out Vector2 pointerPos))
            return;

        bool clickedTarget = IsPointerOverTargetButton(pointerPos);

        if (clickedTarget)
        {
            _count++;

            if (_count >= _requiredClicks)
            {
                Debug.Log("시작");

                if (paymentHttpTester != null)
                {
                    // paymentHttpTester._amount 가 public이어야 함
                    paymentHttpTester._amount = 10;
                }
                else
                {
                    Debug.LogWarning("[FiveClickStartWatcher] paymentHttpTester가 할당되지 않았습니다.");
                }

                _onStart?.Invoke();

                if (_resetCountAfterStart)
                    _count = 0;
            }
        }
        else
        {
            // 타겟 버튼이 아닌 다른 곳 클릭 => 초기화
            _count = 0;
        }
    }

    private bool IsPointerDownThisFrame(out Vector2 pointerPos)
    {
        pointerPos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        // 신 Input System
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            pointerPos = Mouse.current.position.ReadValue();
            return true;
        }

        if (Touchscreen.current != null)
        {
            var t = Touchscreen.current.primaryTouch;
            if (t.press.wasPressedThisFrame)
            {
                pointerPos = t.position.ReadValue();
                return true;
            }
        }

        return false;
#else
        // 구 Input
        if (Input.GetMouseButtonDown(0))
        {
            pointerPos = Input.mousePosition;
            return true;
        }

        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                pointerPos = touch.position;
                return true;
            }
        }

        return false;
#endif
    }

    private bool IsPointerOverTargetButton(Vector2 pointerPos)
    {
        _raycastResults.Clear();

        var ped = new PointerEventData(EventSystem.current)
        {
            position = pointerPos
        };

        EventSystem.current.RaycastAll(ped, _raycastResults);

        if (_raycastResults == null || _raycastResults.Count == 0)
            return false;

        // ✅ 핵심: "전부 검사"해서 타겟(또는 자식)이 하나라도 있으면 true
        Transform targetTf = _targetButton.transform;
        for (int i = 0; i < _raycastResults.Count; i++)
        {
            var go = _raycastResults[i].gameObject;
            if (go == null) continue;

            var tf = go.transform;
            if (tf == targetTf || tf.IsChildOf(targetTf))
                return true;
        }

        return false;
    }
}
