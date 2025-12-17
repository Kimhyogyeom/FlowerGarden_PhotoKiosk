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

    [Header("받아온 이미지(활/비활 시킬 오브젝트)")]
    [SerializeField] private GameObject _receivedImageObject;

    [Header("Settings")]
    [SerializeField, Min(1)] private int _requiredClicks = 10;

    [Header("Amounts")]
    [SerializeField] private int _amountOnFirstToggle = 10;
    [SerializeField] private int _amountOnSecondToggle = 5000;

    [Header("Events (Optional)")]
    [SerializeField] private UnityEvent _onFirstToggle;   // 이미지 ON 되었을 때
    [SerializeField] private UnityEvent _onSecondToggle;  // 이미지 OFF 되었을 때

    private int _count = 0;
    private bool _isImageOn = false; // 현재 이미지가 켜져있는 상태인지
    private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>(32);

    private void Awake()
    {
        if (_targetButton == null)
            Debug.LogWarning("[FiveClickStartWatcher] Target Button is not assigned.");

        if (EventSystem.current == null)
            Debug.LogWarning("[FiveClickStartWatcher] EventSystem이 씬에 없습니다. (UI Raycast 안될 수 있음)");

        if (_receivedImageObject != null)
            _receivedImageObject.SetActive(false);
    }

    private void Update()
    {
        if (_targetButton == null) return;
        if (EventSystem.current == null) return;

        if (!IsPointerDownThisFrame(out Vector2 pointerPos))
            return;

        bool clickedTarget = IsPointerOverTargetButton(pointerPos);

        if (!clickedTarget)
        {
            _count = 0;
            return;
        }

        _count++;

        if (_count < _requiredClicks)
            return;

        // 10번 채움 => 토글 동작 실행
        if (!_isImageOn)
        {
            // [1번째 10클릭] amount=10 + 이미지 ON
            if (paymentHttpTester != null)
            {
                // ⚠ paymentHttpTester._amount 가 public이어야 함
                paymentHttpTester._amount = _amountOnFirstToggle;
            }
            else
            {
                Debug.LogWarning("[FiveClickStartWatcher] paymentHttpTester가 할당되지 않았습니다.");
            }

            if (_receivedImageObject != null)
                _receivedImageObject.SetActive(true);

            _isImageOn = true;
            _onFirstToggle?.Invoke();
        }
        else
        {
            // [2번째 10클릭] amount=5000 + 이미지 OFF
            if (paymentHttpTester != null)
            {
                paymentHttpTester._amount = _amountOnSecondToggle;
            }
            else
            {
                Debug.LogWarning("[FiveClickStartWatcher] paymentHttpTester가 할당되지 않았습니다.");
            }

            if (_receivedImageObject != null)
                _receivedImageObject.SetActive(false);

            _isImageOn = false;
            _onSecondToggle?.Invoke();
        }

        // 다음 10클릭을 위해 카운트 리셋
        _count = 0;
    }

    private bool IsPointerDownThisFrame(out Vector2 pointerPos)
    {
        pointerPos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
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

        if (_raycastResults.Count == 0)
            return false;

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
