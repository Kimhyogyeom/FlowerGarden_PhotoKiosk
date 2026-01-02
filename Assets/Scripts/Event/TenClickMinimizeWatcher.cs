using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 지정된 버튼을 10번 클릭하면 앱을 최소화(바탕화면으로 보냄)하는 스크립트
/// </summary>
public class TenClickMinimizeWatcher : MonoBehaviour
{
    #region Windows API
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_MINIMIZE = 6;
    #endregion

    [Header("Target")]
    [SerializeField] private Button _targetButton;

    [Header("Settings")]
    [SerializeField, Min(1)] private int _requiredClicks = 10;

    private int _count = 0;
    private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>(32);

    private void Awake()
    {
        if (_targetButton == null)
            Debug.LogWarning("[TenClickMinimizeWatcher] Target Button is not assigned.");

        if (EventSystem.current == null)
            Debug.LogWarning("[TenClickMinimizeWatcher] EventSystem이 씬에 없습니다. (UI Raycast 안될 수 있음)");
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

        if (_count >= _requiredClicks)
        {
            // Debug.Log("[TenClickMinimizeWatcher] 10번 클릭 감지 - 앱 최소화");
            MinimizeWindow();
            _count = 0;
        }
    }

    private void MinimizeWindow()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        IntPtr hWnd = GetActiveWindow();
        if (hWnd != IntPtr.Zero)
        {
            ShowWindow(hWnd, SW_MINIMIZE);
        }
#else
        // Debug.Log("[TenClickMinimizeWatcher] Windows가 아닌 환경에서는 최소화가 동작하지 않습니다.");
#endif
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
