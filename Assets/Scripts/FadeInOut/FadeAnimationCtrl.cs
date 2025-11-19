using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;

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
    [SerializeField] private InitCtrl _initCtrl;
    // 초기화 및 패널 전환 총괄 컨트롤러

    [Space(10)]
    [SerializeField] private Animator _fadeAnimator;
    // Fade 애니메이션을 재생하는 Animator
    [SerializeField] private ReadyPanelTransitionCtrl _readyPanelTransitionCtrl;
    // Ready → Camera 패널 전환 담당
    [SerializeField] private FilmingPanelCtrl _filmingPanelCtrl;
    // 프레임 선택 → 촬영 패널 전환 담당
    // [SerializeField] private FilmingToSelectCtrl _filmingToSelectCtrl;s
    // 촬영 화면 → 선택 화면으로 돌아갈 때 사용

    [SerializeField] private PaymentCtrl _paymentCtrl;
    // 결제 완료 시스템
    [SerializeField] private QuantityToPaymentCtrl _quantityToPaymentCtrl;
    // 수량 -> 결제 컨트롤러
    [SerializeField] private PaymentToNextStageCtrl _paymentToNextStageCtrl;
    // 결제 -> 결제완료 자동 : (결제 완료 -> 필름 패널로 변경)
    [SerializeField] private HomButtonCtrl _homeButtonCtrl;
    // 홈 버튼 누르면 실행될 제어 컨트롤러 

    [SerializeField] private PaymentWaitingPanelTransitionCtrl _paymentWatingPanelTranstionCtrl;
    // 이거 결제 -> 결제 대기 할 때 로직 컨트롤러

    [SerializeField] private PrintButtonHandler _printButtonHandler;

    [SerializeField] private CapturedPhotoPanelCtrl _capturePhotoPanelCtrl;
    // 촬영 끝나고 포토 선택 화면으로 페이드 인아웃 되게끔?


    // 근데 오토 안 쓸 예정 (최대한 디자인 된 PDF 파일 따라하고 추후 시간날때 할 예정?)
    [Header("Auto")]
    [SerializeField] private ReadyAutoTransitionCtrl _readyAutoTransitionCtrl;      // 페이드 아웃 됐을때 타이머 호출
    [SerializeField] private SelectAutoTransitionCtrl _selectAutoTransitionCtrl;    // 페이드 아웃 될 때 타이머 호출
    [SerializeField] private AutoShootStartCtrl _autoShootStartCtrl;                // 페이드 아웃 될 때 타이머 호출

    // [결제 시스템 없는 버전으로 테스트용 추가]
    [SerializeField] private GameObject _panelWaitingForPayment;
    [SerializeField] private GameObject _panelPayment;


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
        print("StartFade 호출이요~!");

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
        print("OnFadeEnd 호출이요~!");

        // CoroutineAllStopFunction();
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
                    // _readyAutoTransitionCtrl.AutoTransitionTimer();
                }
                else
                {
                    UnityEngine.Debug.LogWarning("_paymentCtrl reference is missing");
                }
            }
            // 0단계: Ready 화면에서 "시작하기" 버튼 클릭 후 → 선택 화면으로 변경
            else if (_isStateStep == 0)
            {
                _isStateStep = 1;

                // Ready → Camera 전환
                if (_readyPanelTransitionCtrl != null)
                {
                    _readyPanelTransitionCtrl.OnFadeFinished();
                    // _selectAutoTransitionCtrl.StartAutoTransitionTimer();
                }
                else
                {
                    UnityEngine.Debug.LogWarning("_readyPanelTransitionCtrl reference is missing");
                }
            }
            // 1단계: 프레임 선택 화면에서 "사진 찍기" 버튼 클릭 후 → 촬영 패널로 전환
            // 였는데 수량 화면 전환으로 바뀔 예정
            // 프레임 선택  화면에서 수량 화면으로 전환
            else if (_isStateStep == 1)
            {
                _isStateStep = 2;

                if (_filmingPanelCtrl != null)
                {
                    _filmingPanelCtrl.PanelChanger();
                    // _autoShootStartCtrl.StartAutoTransitionTimer();
                    // print("여기 실행됨?");
                }
                else
                {
                    UnityEngine.Debug.LogWarning("_filmingPanelCtrl reference is missing");
                }
            }
            // // ──────────────────────────────────────────────────────────────────────────────────────────────
            // // [기존 버전]
            // // 수량 화면에서 결제 화면으로 전환
            // else if (_isStateStep == 2)
            // {
            //     // print("222222222222222");
            //     _isStateStep = 3;
            //     _quantityToPaymentCtrl.ObjectActiveCtrl();
            // }
            // // 2단계: 촬영 및 출력 플로우가 끝난 뒤 → Ready(결제/대기) 화면으로 복귀
            // // 였는데 결제 화면에서 촬영 시작 화면으로 바뀔 예정
            // // 결제화면에서 결제 진행중 화면으로 전환되어야함            
            // // 일단 안씀 ㄱㄷ
            // else if (_isStateStep == 3)
            // {
            //     // print("3333333333333");
            //     _isStateStep = 4;
            //     _paymentWatingPanelTranstionCtrl.FadeEndCallBack();
            //     // _paymentWatingPanelTranstionCtrl.OnClickGoToPayment();
            // }
            // // ──────────────────────────────────────────────────────────────────────────────────────────────
            // ──────────────────────────────────────────────────────────────────────────────────────────────
            // [수정 버전 - Test]
            // 수량 화면에서 결제 화면으로 전환
            else if (_isStateStep == 2)
            {
                // print("222222222222222");
                _isStateStep = 5;
                _quantityToPaymentCtrl.ObjectActiveCtrl();
                _paymentToNextStageCtrl.OnPaymentCompleted();
                if (_panelPayment.activeSelf) _panelPayment.SetActive(false);
                if (_panelWaitingForPayment.activeSelf) _panelWaitingForPayment.SetActive(false);

            }
            // ──────────────────────────────────────────────────────────────────────────────────────────────
            // 결제 완료 -> 사진 촬영 패널로 변경
            else if (_isStateStep == 4)
            {
                // print("44444444444444");
                _isStateStep = 5;
                _paymentToNextStageCtrl.OnPaymentCompleted();
            }
            // 사진 촬영 패널에서 촬영 시작으로 변경
            else if (_isStateStep == 5)
            {
                _isStateStep = 6;
                _filmingPanelCtrl.FadeEndCallBack();
            }
            // 촬영 시작 패널에서 포토 수량 선택으로 할 것임
            else if (_isStateStep == 6)
            {
                _isStateStep = 7;
                _capturePhotoPanelCtrl.FadeEndCallBack();
            }
            // 포토 수량 선택에서 프린트 상태로
            else if (_isStateStep == 7)
            {
                _isStateStep = 8;
                _printButtonHandler.FadeEndCallBack();
            }
            // 프린트 상태에서 리셋 상태로 초기화
            else if (_isStateStep == 8)
            {
                _isStateStep = 0;
                _initCtrl.PanaelActiveCtrl(); // 초기화하는녀석
            }
            // ────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            // 홈 버튼 클릭
            // ────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            // 100단계: 촬영 화면에서 Back 버튼 사용 시  
            // - _isStateStep를 100으로 설정해 진입  
            // - 여기서 1로 변경 후, FilmingToSelectCtrl을 통해 선택 화면으로 복귀
            // [1117] 사라질 예정.
            else if (_isStateStep == 100)
            {
                UnityEngine.Debug.Log("_isStateStep : greater than 100");
                _isStateStep = 1;
                // _filmingToSelectCtrl.PanaelActiveCtrl();
            }
            // 101단계: 프레임 선택 화면에서 홈 화면을 클릭했을 때 실행될꺼임
            else if (_isStateStep == 101)
            {
                _isStateStep = 0;
                _homeButtonCtrl.ObjectsActiveCtrlSel();
            }
            // 102단계 프레임 -> 선택 화면 -> 수량 화면에서 홈 화면을 클릭했을 때 실행될꺼임
            else if (_isStateStep == 102)
            {
                _isStateStep = 0;
                _homeButtonCtrl.ObjectsActiveCtrlSel();
            }
            // 103단계 프레임 -수량 -> 결제 화면에서 홈 화면을 클릭했을 때 실행될꺼임
            else if (_isStateStep == 103)
            {
                _isStateStep = 0;
                _homeButtonCtrl.ObjectsActiveCtrlSel();
            }
            // ────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            // 뒤로가기 버튼 클릭
            // ────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            // 201단계 수량 화면에서 뒤로가기 버튼을 클릭했을 때 실행될거임 (수량 -> 선택 화면)
            else if (_isStateStep == 201)
            {
                _isStateStep = 1;
                _homeButtonCtrl.ObjectsActiveCtrlQua();
            }
            // 202단계 수량 화면에서 뒤로가기 버튼을 클릭했을 때 실행될거임 (수량 -> 선택 화면)
            else if (_isStateStep == 202)
            {
                _isStateStep = 2;
                _homeButtonCtrl.ObjectsActiveCtrlPay();
            }
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
