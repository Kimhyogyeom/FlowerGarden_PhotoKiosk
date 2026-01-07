using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 촬영 안내 패널 컨트롤러
/// - 결제 완료 후 촬영 전 "카메라를 봐주세요" 안내 화면
/// - 다음 버튼 클릭 시 촬영 시작
/// </summary>
public class GuidePanelCtrl : MonoBehaviour
{
    [Header("Component")]
    [SerializeField] private FadeAnimationCtrl _fadeAnimationCtrl;

    [Header("Panel Settings")]
    [SerializeField] private GameObject _paymentPanel;
    // 결제 패널 (Guide 패널이 켜질 때 꺼짐)

    [SerializeField] private GameObject _guidePanel;
    // 촬영 안내 패널 (결제 완료 시 활성화)

    [SerializeField] private GameObject _cameraWindowPanel;
    // 카메라 윈도우 패널 (Guide 완료 후 활성화)

    [Header("Button")]
    [SerializeField] private Button _nextButton;
    // "다음" 버튼 - 촬영 시작으로 이동

    private void Awake()
    {
        if (_nextButton != null)
        {
            _nextButton.onClick.AddListener(OnClickNext);
        }
        else
        {
            Debug.LogWarning("[GuidePanelCtrl] _nextButton reference is missing");
        }
    }

    private void OnDestroy()
    {
        if (_nextButton != null)
        {
            _nextButton.onClick.RemoveListener(OnClickNext);
        }
    }

    /// <summary>
    /// 결제 완료 후 호출 (FadeAnimationCtrl에서 호출)
    /// - 결제 패널 끄고 Guide 패널 켬
    /// </summary>
    public void OnPaymentCompleted()
    {
        // 결제 패널 비활성화
        if (_paymentPanel != null)
            _paymentPanel.SetActive(false);

        // 안내 패널 활성화
        if (_guidePanel != null)
            _guidePanel.SetActive(true);
        else
            Debug.LogWarning("[GuidePanelCtrl] _guidePanel reference is missing");

        // 키오스크 상태를 Guide로 설정
        GameManager.Instance.SetState(KioskState.Guide);

        // Guide 패널 TTS 재생
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._windowGuideSound);
    }

    /// <summary>
    /// "다음" 버튼 클릭 시 호출
    /// - 페이드 후 촬영 시작
    /// </summary>
    private void OnClickNext()
    {
        // Guide → Filming Start 전환
        _fadeAnimationCtrl.SetState(FadeState.GuideToFilmingStart);
        _fadeAnimationCtrl.StartFade();

        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._buttonClickSound);
    }

    /// <summary>
    /// Guide 완료 후 호출 (FadeAnimationCtrl에서 호출)
    /// - Guide 패널 끄고 카메라 윈도우 켬
    /// </summary>
    public void OnGuideCompleted()
    {
        // 안내 패널 비활성화
        if (_guidePanel != null)
            _guidePanel.SetActive(false);

        // 카메라 윈도우 활성화
        if (_cameraWindowPanel != null)
            _cameraWindowPanel.SetActive(true);
    }

    /// <summary>
    /// 리셋 시 호출 (InitCtrl 등에서 호출)
    /// </summary>
    public void ResetGuidePanel()
    {
        if (_guidePanel != null)
            _guidePanel.SetActive(false);
    }
}
