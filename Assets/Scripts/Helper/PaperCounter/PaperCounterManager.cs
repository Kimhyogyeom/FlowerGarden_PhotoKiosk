using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 용지 카운터 관리 시스템
/// - 초기값: 0
/// - 버튼 10번 클릭: 설정된 값으로 세팅 (용지 교체 완료)
/// - 프린트할 때마다: -2씩 감소
/// - 0이 되면: 용지 부족 UI 활성화
/// - JSON 파일로 빌드파일 옆에 저장 (재부팅해도 유지)
/// - 용지 교체/프린트 기록 로그
/// </summary>
public class PaperCounterManager : MonoBehaviour
{
    private const string FILE_NAME = "paper_counter.json";
    private const int DEFAULT_PAPER_COUNT = 690;
    private const int PRINT_DECREMENT = 2;

    [Header("운영자 리셋 버튼")]
    [SerializeField] private Image _resetButtonImage;
    [SerializeField, Min(1)] private int _requiredClicks = 10;
    [SerializeField] private int _resetPaperCount = 690;

    [Header("용지 부족 UI")]
    [SerializeField] private GameObject _paperEmptyUI;

    [Header("디버그")]
    [SerializeField] private Text _debugCountText;

    private PaperCounterData _data;
    private int _clickCount;
    private string _filePath;
    private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>(32);

    public static PaperCounterManager Instance { get; private set; }

    /// <summary>
    /// 현재 용지 카운트
    /// </summary>
    public int CurrentCount => _data?.currentCount ?? 0;

    /// <summary>
    /// 용지가 비었는지 여부
    /// </summary>
    public bool IsPaperEmpty => CurrentCount <= 0;

    /// <summary>
    /// 용지 카운트 변경 이벤트
    /// </summary>
    public event Action<int> OnCountChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 빌드 파일 옆에 JSON 파일 경로 설정
        _filePath = Path.Combine(Application.dataPath, "..", FILE_NAME);
        _filePath = Path.GetFullPath(_filePath); // 정규화

        LoadData();
        UpdateUI();

        Debug.Log($"[PaperCounter] 초기화 완료. 파일 경로: {_filePath}");
    }

    private void Update()
    {
        CheckResetButtonClick();
    }

    /// <summary>
    /// 프린트 시 호출 - 용지 2장 감소
    /// </summary>
    public void OnPrintCompleted()
    {
        if (_data.currentCount > 0)
        {
            int before = _data.currentCount;
            _data.currentCount = Mathf.Max(0, _data.currentCount - PRINT_DECREMENT);
            _data.totalPrintCount++;

            // 기록 추가
            var log = new PaperLog
            {
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                action = "PRINT",
                beforeCount = before,
                afterCount = _data.currentCount,
                message = $"프린트 완료 (-{PRINT_DECREMENT}장)"
            };
            _data.logs.Add(log);

            // 로그가 너무 많으면 오래된 것 삭제 (최대 500개 유지)
            while (_data.logs.Count > 500)
            {
                _data.logs.RemoveAt(0);
            }

            SaveData();
            UpdateUI();
            OnCountChanged?.Invoke(_data.currentCount);

            Debug.Log($"[PaperCounter] 프린트 완료. 남은 용지: {_data.currentCount}");
        }
    }

    /// <summary>
    /// 용지 리셋 (설정된 값으로)
    /// </summary>
    public void ResetPaperCount()
    {
        int before = _data.currentCount;
        _data.currentCount = _resetPaperCount;
        _data.lastResetTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _data.resetCount++;

        // 기록 추가
        var log = new PaperLog
        {
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            action = "RESET",
            beforeCount = before,
            afterCount = _data.currentCount,
            message = $"용지 교체 완료 ({_resetPaperCount}장으로 리셋)"
        };
        _data.logs.Add(log);

        SaveData();
        UpdateUI();
        OnCountChanged?.Invoke(_data.currentCount);

        Debug.Log($"[PaperCounter] 용지 리셋 완료. 현재 용지: {_data.currentCount}");
    }

    private void LoadData()
    {
        if (File.Exists(_filePath))
        {
            try
            {
                string json = File.ReadAllText(_filePath);
                _data = JsonUtility.FromJson<PaperCounterData>(json);
                Debug.Log($"[PaperCounter] 저장된 데이터 로드: {_data.currentCount}장");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PaperCounter] JSON 로드 실패: {e.Message}");
                _data = new PaperCounterData();
            }
        }
        else
        {
            _data = new PaperCounterData();
            Debug.Log("[PaperCounter] 새 데이터 생성 (초기값: 0)");
        }
    }

    private void SaveData()
    {
        try
        {
            string json = JsonUtility.ToJson(_data, true);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[PaperCounter] JSON 저장 실패: {e.Message}");
        }
    }

    private void UpdateUI()
    {
        // 용지 부족 UI 활성화/비활성화
        if (_paperEmptyUI != null)
        {
            _paperEmptyUI.SetActive(_data.currentCount <= 0);
        }

        // 디버그 텍스트 업데이트
        if (_debugCountText != null)
        {
            _debugCountText.text = $"용지: {_data.currentCount}";
        }
    }

    private void CheckResetButtonClick()
    {
        if (_resetButtonImage == null) return;
        if (EventSystem.current == null) return;

        if (!IsPointerDownThisFrame(out Vector2 pointerPos))
            return;

        bool clickedTarget = IsPointerOverTarget(pointerPos);

        if (!clickedTarget)
        {
            _clickCount = 0;
            return;
        }

        _clickCount++;
        Debug.Log($"[PaperCounter] 리셋 버튼 클릭: {_clickCount}/{_requiredClicks}");

        if (_clickCount >= _requiredClicks)
        {
            ResetPaperCount();
            _clickCount = 0;
        }
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

    private bool IsPointerOverTarget(Vector2 pointerPos)
    {
        _raycastResults.Clear();

        var ped = new PointerEventData(EventSystem.current)
        {
            position = pointerPos
        };

        EventSystem.current.RaycastAll(ped, _raycastResults);

        if (_raycastResults.Count == 0)
            return false;

        Transform targetTf = _resetButtonImage.transform;
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

#if UNITY_EDITOR
    [ContextMenu("테스트: 용지 리셋")]
    private void TestReset()
    {
        ResetPaperCount();
    }

    [ContextMenu("테스트: 프린트 1회")]
    private void TestPrint()
    {
        OnPrintCompleted();
    }

    [ContextMenu("테스트: 용지 0으로")]
    private void TestSetZero()
    {
        int before = _data.currentCount;
        _data.currentCount = 0;

        var log = new PaperLog
        {
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            action = "TEST_ZERO",
            beforeCount = before,
            afterCount = 0,
            message = "테스트: 0으로 설정"
        };
        _data.logs.Add(log);

        SaveData();
        UpdateUI();
        OnCountChanged?.Invoke(_data.currentCount);
    }

    [ContextMenu("JSON 파일 열기")]
    private void OpenJsonFile()
    {
        if (File.Exists(_filePath))
        {
            System.Diagnostics.Process.Start(_filePath);
        }
        else
        {
            Debug.Log("[PaperCounter] JSON 파일이 아직 없습니다.");
        }
    }
#endif
}

/// <summary>
/// JSON 저장용 데이터 클래스
/// </summary>
[Serializable]
public class PaperCounterData
{
    public int currentCount = 0;
    public string lastResetTime = "";
    public int resetCount = 0;
    public int totalPrintCount = 0;
    public List<PaperLog> logs = new List<PaperLog>();
}

/// <summary>
/// 용지 기록 로그
/// </summary>
[Serializable]
public class PaperLog
{
    public string timestamp;
    public string action;      // PRINT, RESET, TEST_ZERO
    public int beforeCount;
    public int afterCount;
    public string message;
}
