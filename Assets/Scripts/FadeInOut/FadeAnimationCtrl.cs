using System.Diagnostics;
using UnityEngine;

/// <summary>
/// 화면 전환용 페이드 애니메이션 제어 스크립트  
/// - Ready / Select / Filming / Ready 로 이어지는 패널 전환의 “게이트” 역할  
/// - 외부(ReadyPanelTransitionCtrl, FilmingPanelCtrl, InitCtrl, FilmingToSelectCtrl)에서
///   StartFade()를 호출하면 페이드 인/아웃 실행  
/// - 애니메이션 마지막 프레임에서 Animation Event로 OnFadeEnd()가 호출되며,
///   _isStateStep 값에 따라 다음 화면으로 전환
/// </summary>
public class FadeAnimationCtrl : MonoBehaviour
{
    [Header("Setting Component")]
    [SerializeField] private InitCtrl _initCtrl;                    // 초기화 및 패널 전환 총괄 컨트롤러
    [Space(10)]
    [SerializeField] private Animator _fadeAnimator;                // Fade 애니메이션을 재생하는 Animator
    [SerializeField] private ReadyPanelTransitionCtrl _readyPanelTransitionCtrl;  // Ready → Camera 패널 전환 담당
    [SerializeField] private FilmingPanelCtrl _filmingPanelCtrl;    // 프레임 선택 → 촬영 패널 전환 담당
    [SerializeField] private FilmingToSelectCtrl _filmingToSelectCtrl; // 촬영 화면 → 선택 화면으로 돌아갈 때 사용

    [SerializeField] private PaymentCtrl _paymentCtrl;  // 결제 완료 시스템

    [Header("Auto")]
    [SerializeField] private ReadyAutoTransitionCtrl _readyAutoTransitionCtrl;      // 페이드 아웃 됐을때 타이머 호출
    [SerializeField] private SelectAutoTransitionCtrl _selectAutoTransitionCtrl;    // 페이드 아웃 될 때 타이머 호출
    [SerializeField] private AutoShootStartCtrl _autoShootStartCtrl;                // 페이드 아웃 될 때 타이머 호출


    /// <summary>
    /// 페이드 단계 상태 값  
    /// 0 : Ready 화면에서 "시작하기" 버튼을 눌러 Camera 패널로 넘어갈 때  
    /// 1 : 프레임 선택 → 촬영 패널로 전환할 때  
    /// 2 : 촬영 종료 후 Ready(대기) 화면으로 복귀할 때  
    /// 100 : 촬영 화면에서 뒤로 가기(Back) 버튼 클릭 시, 선택 화면으로 복귀할 때 사용되는 임시 상태
    /// </summary>
    public int _isStateStep = 0;

    /// <summary>
    /// 페이드 시작 (외부에서 버튼 클릭 시 호출)  
    /// - Animator의 "Fade" Bool 파라미터를 true로 설정하여 페이드 인 시작  
    /// - 페이드 인 사운드 재생
    /// </summary>
    public void StartFade()
    {
        if (_fadeAnimator != null)
        {
            _fadeAnimator.SetBool("Fade", true);
            SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._fadeIn);
        }
        else
        {
            UnityEngine.Debug.LogWarning("_fadeAnimator reference is missing");
        }
    }

    private void Update()
    {
        // 디버그용 (상태 값 확인용)
        // UnityEngine.Debug.Log($"_isStateStep : {_isStateStep}");
    }

    /// <summary>
    /// 애니메이션 이벤트(Animation Event)에서 호출됨  
    /// - 페이드 애니메이션이 끝나는 타이밍에 Animator 상태 복구  
    /// - _isStateStep 상태 값에 따라 다음 패널 전환/초기화 로직 실행
    /// </summary>
    public void OnFadeEnd()
    {
        CoroutineAllStopFunction();
        if (_fadeAnimator != null)
        {
            // 페이드 애니메이션 플래그 초기화 및 페이드 아웃 사운드 재생
            _fadeAnimator.SetBool("Fade", false);
            SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._fadeOut);

            if (_isStateStep == -1)
            {
                _isStateStep = 0;
                if (_paymentCtrl != null)
                {
                    _paymentCtrl.OnCallbackEnd();
                    _readyAutoTransitionCtrl.AutoTransitionTimer();
                }
                else
                {
                    UnityEngine.Debug.LogWarning("_paymentCtrl reference is missing");
                }
            }
            // 0단계: Ready 화면에서 "시작하기" 버튼 클릭 후 → 카메라 패널로 전환
            else if (_isStateStep == 0)
            {
                _isStateStep = 1;

                // Ready → Camera 전환
                if (_readyPanelTransitionCtrl != null)
                {
                    _readyPanelTransitionCtrl.OnFadeFinished();
                    _selectAutoTransitionCtrl.StartAutoTransitionTimer();
                }
                else
                {
                    UnityEngine.Debug.LogWarning("_readyPanelTransitionCtrl reference is missing");
                }
            }
            // 1단계: 프레임 선택 화면에서 "사진 찍기" 버튼 클릭 후 → 촬영 패널로 전환
            else if (_isStateStep == 1)
            {
                _isStateStep = 2;

                if (_filmingPanelCtrl != null)
                {
                    _filmingPanelCtrl.PanelChanger();
                    _autoShootStartCtrl.StartAutoTransitionTimer();
                }
                else
                {
                    UnityEngine.Debug.LogWarning("_filmingPanelCtrl reference is missing");
                }
            }
            // 2단계: 촬영 및 출력 플로우가 끝난 뒤 → Ready(결제/대기) 화면으로 복귀
            else if (_isStateStep == 2)
            {
                // 현재 스텝 최대 값은 2  
                // 2까지 처리 후에는 다시 0으로 초기화하여 다음 루프를 위한 준비
                _isStateStep = -1;
                _initCtrl.PanaelActiveCtrl();
            }
            // 100단계: 촬영 화면에서 Back 버튼 사용 시  
            // - _isStateStep를 100으로 설정해 진입  
            // - 여기서 1로 변경 후, FilmingToSelectCtrl을 통해 선택 화면으로 복귀
            else if (_isStateStep == 100)
            {
                UnityEngine.Debug.Log("_isStateStep : greater than 100");
                _isStateStep = 1;
                _filmingToSelectCtrl.PanaelActiveCtrl();
            }
            // 그 외 값: 특별 처리 없음 (디버그 용도)
            else
            {
                UnityEngine.Debug.Log("_isStateStep : else");
                // 별도 처리 없음
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning("_fadeAnimator reference is missing");
        }
    }
    private void CoroutineAllStopFunction()
    {
        _readyAutoTransitionCtrl.StopAndResetTimer();
        _selectAutoTransitionCtrl.StopAutoTransitionTimer();
        _autoShootStartCtrl.ResetAutoShootStartCtrl();
    }
}
