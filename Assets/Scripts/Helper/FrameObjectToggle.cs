using UnityEngine;

/// <summary>
/// 모드(세로/가로) + 프레임 인덱스(0,1,2)에 따라 오브젝트 활성화/비활성화 토글
/// - 총 6개 오브젝트 중 하나만 활성화, 나머지는 비활성화
/// - PhotoFrameSelectCtrl.OnFrameSelected 이벤트 구독
/// - GameManager.OnModeChanged 이벤트 구독
/// </summary>
public class FrameObjectToggle : MonoBehaviour
{
    [Header("세로 모드(Hight) 프레임별 오브젝트")]
    [Tooltip("인덱스 0: 빨강, 1: 파랑, 2: 검정")]
    [SerializeField] private GameObject[] _hightFrameObjects = new GameObject[3];

    [Header("가로 모드(Width) 프레임별 오브젝트")]
    [Tooltip("인덱스 0: 빨강, 1: 파랑, 2: 검정")]
    [SerializeField] private GameObject[] _widthFrameObjects = new GameObject[3];

    // 현재 선택된 프레임 인덱스 (모드별로 저장)
    private int _currentHightIndex = 0;
    private int _currentWidthIndex = 0;

    private void OnEnable()
    {
        // 이벤트 구독
        PhotoFrameSelectCtrl.OnFrameSelected += OnFrameSelected;
        GameManager.OnModeChanged += OnModeChanged;

        // 현재 상태로 초기 적용
        ApplyCurrentState();
    }

    private void Start()
    {
        // OnEnable 시점에 GameManager가 준비 안 됐을 수 있으므로 Start에서 한 번 더 적용
        ApplyCurrentState();
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        PhotoFrameSelectCtrl.OnFrameSelected -= OnFrameSelected;
        GameManager.OnModeChanged -= OnModeChanged;
    }

    /// <summary>
    /// 프레임 선택 변경 이벤트 핸들러
    /// </summary>
    private void OnFrameSelected(KioskMode mode, int index)
    {
        // 해당 모드의 인덱스 저장
        if (mode == KioskMode.Hight)
            _currentHightIndex = index;
        else
            _currentWidthIndex = index;

        // 현재 모드와 일치하면 바로 적용
        if (GameManager.Instance != null && GameManager.Instance.CurrentMode == mode)
        {
            ApplyFrame(mode, index);
        }
    }

    /// <summary>
    /// 모드 변경 이벤트 핸들러
    /// </summary>
    private void OnModeChanged(KioskMode newMode)
    {
        // 모드 변경 시 해당 모드의 저장된 인덱스로 적용
        int index = (newMode == KioskMode.Hight) ? _currentHightIndex : _currentWidthIndex;
        ApplyFrame(newMode, index);
    }

    /// <summary>
    /// 현재 GameManager 상태 기준으로 적용
    /// </summary>
    public void ApplyCurrentState()
    {
        if (GameManager.Instance == null) return;

        KioskMode mode = GameManager.Instance.CurrentMode;
        int index = (mode == KioskMode.Hight) ? _currentHightIndex : _currentWidthIndex;
        ApplyFrame(mode, index);
    }

    /// <summary>
    /// 지정된 모드와 인덱스에 맞게 오브젝트 활성화/비활성화
    /// </summary>
    private void ApplyFrame(KioskMode mode, int index)
    {
        // 모든 오브젝트 비활성화
        DeactivateAll();

        // 해당 모드/인덱스 오브젝트만 활성화
        if (mode == KioskMode.Hight)
        {
            if (_hightFrameObjects != null && index >= 0 && index < _hightFrameObjects.Length)
            {
                if (_hightFrameObjects[index] != null)
                    _hightFrameObjects[index].SetActive(true);
            }
        }
        else
        {
            if (_widthFrameObjects != null && index >= 0 && index < _widthFrameObjects.Length)
            {
                if (_widthFrameObjects[index] != null)
                    _widthFrameObjects[index].SetActive(true);
            }
        }
    }

    /// <summary>
    /// 모든 오브젝트 비활성화
    /// </summary>
    private void DeactivateAll()
    {
        if (_hightFrameObjects != null)
        {
            foreach (var obj in _hightFrameObjects)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }

        if (_widthFrameObjects != null)
        {
            foreach (var obj in _widthFrameObjects)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }
    }
}
