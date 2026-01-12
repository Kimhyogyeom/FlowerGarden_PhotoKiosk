using UnityEngine;

/// <summary>
/// 모드(세로/가로)에 따라 오브젝트 활성화/비활성화 토글
/// - 세로 모드(Hight): _hightObjects 활성화, _widthObjects 비활성화
/// - 가로 모드(Width): _widthObjects 활성화, _hightObjects 비활성화
/// - GameManager.OnModeChanged 이벤트 구독하여 자동 적용
/// </summary>
public class ModeObjectToggle : MonoBehaviour
{
    [Header("세로 모드(Hight)에서 활성화할 오브젝트들")]
    [SerializeField] private GameObject[] _hightObjects;

    [Header("가로 모드(Width)에서 활성화할 오브젝트들")]
    [SerializeField] private GameObject[] _widthObjects;

    private void OnEnable()
    {
        // 이벤트 구독
        GameManager.OnModeChanged += OnModeChanged;

        // 현재 모드로 초기 적용
        ApplyMode();
    }

    private void Start()
    {
        // OnEnable 시점에 GameManager가 준비 안 됐을 수 있으므로 Start에서 한 번 더 적용
        ApplyMode();
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        GameManager.OnModeChanged -= OnModeChanged;
    }

    /// <summary>
    /// 모드 변경 이벤트 핸들러
    /// </summary>
    private void OnModeChanged(KioskMode newMode)
    {
        ApplyMode(newMode == KioskMode.Hight);
    }

    /// <summary>
    /// 현재 모드에 맞게 오브젝트 활성화/비활성화 적용
    /// </summary>
    public void ApplyMode()
    {
        bool isHightMode = GameManager.Instance != null && GameManager.Instance.CurrentMode == KioskMode.Hight;
        ApplyMode(isHightMode);
    }

    /// <summary>
    /// 지정된 모드에 맞게 오브젝트 활성화/비활성화 적용
    /// </summary>
    private void ApplyMode(bool isHightMode)
    {
        // 세로 모드용 오브젝트
        if (_hightObjects != null)
        {
            foreach (var obj in _hightObjects)
            {
                if (obj != null)
                    obj.SetActive(isHightMode);
            }
        }

        // 가로 모드용 오브젝트
        if (_widthObjects != null)
        {
            foreach (var obj in _widthObjects)
            {
                if (obj != null)
                    obj.SetActive(!isHightMode);
            }
        }
    }
}
