using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 크로마키 패널 컨트롤러
/// </summary>
public class ChromakeyPanelCtrl : MonoBehaviour
{
    [Header("Component")]
    [SerializeField] private FadeAnimationCtrl _fadeAnimationCtrl;

    [Header("Step Panel Ctrl")]
    [SerializeField] private GameObject _currentPanel;  // 현재 패널
    [SerializeField] private GameObject _nextPanel;     // 다음 패널 (바뀔녀석)
    [SerializeField] private Button _nextButton;        // 다음 패널로 넘어가는 "버튼"

    void Awake()
    {
        _nextButton.onClick.AddListener(OnFadeStart);
    }
    /// <summary>
    /// 버튼 클릭하면 호출될 함수
    /// 애니메이션 실행할것
    /// </summary>
    public void OnFadeStart()
    {
        _fadeAnimationCtrl.StartFade();
    }
    /// <summary>
    /// 외부(Fade-Animation 호출용)
    /// Fade Out때 불림
    /// </summary>
    public void OnPanelChange()
    {
        if (_currentPanel != null) _currentPanel.SetActive(false);
        if (_nextPanel != null) _nextPanel.SetActive(true);

        // Payment로 상태 변경
        GameManager.Instance.SetState(KioskState.Payment);
    }
}
