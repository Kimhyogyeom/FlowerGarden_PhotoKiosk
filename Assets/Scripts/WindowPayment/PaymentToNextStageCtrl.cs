using UnityEngine;

/// <summary>
/// 결제 대기 화면 → 다음 촬영 단계 패널 전환 컨트롤러
/// - 결제가 승인되었을 때 호출해서 패널 전환 + 상태 변경
/// - 다음 상태(KioskState)는 인스펙터에서 설정(Ready, Select, Filming 등)
/// </summary>
public class PaymentToNextStageCtrl : MonoBehaviour
{
    [Header("Component Settings")]
    [SerializeField] private FadeAnimationCtrl _fadeAnimationCtrl;
    [Header("Panel Settings")]
    [SerializeField] private GameObject _waitingForPaymentPanel;
    // 결제 대기 화면 패널 (결제 중에 보여주는 패널)

    [SerializeField] private GameObject _nextPanel;
    // 결제 완료 후 보여줄 다음 패널
    // 예) Ready 패널 또는 촬영 화면(카메라) 패널

    [Header("Kiosk State")]
    [SerializeField] private KioskState _nextState = KioskState.Filming;
    // 결제 후 전환할 Kiosk 상태
    // 예) Ready, Select, Filming 등 필요에 따라 변경

    /// <summary>
    /// [외부에서 호출] 결제 완료 시 패널 전환
    /// - PaymentCtrl.OnPaymentApproved() 같은 곳에서 호출해주면 됨
    /// </summary>
    public void OnPaymentCompleted()
    {
        // 키오스크 상태 변경
        if (GameManager.Instance != null)
        {
            // _fadeAnimationCtrl.StartFade();
            GameManager.Instance.SetState(_nextState);
        }
        else
        {
            Debug.LogWarning("[PaymentToNextStageCtrl] GameManager.Instance is null");
        }

        // 패널 전환: 결제 대기 OFF, 다음 단계 ON
        if (_waitingForPaymentPanel != null)
        {
            _waitingForPaymentPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[PaymentToNextStageCtrl] _waitingForPaymentPanel reference is missing");
        }

        if (_nextPanel != null)
        {
            _nextPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[PaymentToNextStageCtrl] _nextPanel reference is missing");
        }

        // 필요하면 여기에서 효과음도 추가 가능
        // SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._paymentSuccess);
    }

    /// <summary>
    /// [선택] 초기화용 함수
    /// - 씬 로드 시 결제 대기만 켜두고, 다음 패널은 꺼두고 싶을 때 호출
    ///   (Start()에서 자동으로 호출하거나, 다른 초기화 스크립트에서 호출 가능)
    /// </summary>
    public void ResetPanels()
    {
        if (_waitingForPaymentPanel != null)
            _waitingForPaymentPanel.SetActive(true);

        if (_nextPanel != null)
            _nextPanel.SetActive(false);
    }
}
