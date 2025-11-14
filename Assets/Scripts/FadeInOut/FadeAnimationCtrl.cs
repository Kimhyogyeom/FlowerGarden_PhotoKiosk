using System.Diagnostics;
using UnityEngine;

/// <summary>
/// 화면 전환용 페이드 애니메이션 제어 스크립트  
/// - ReadyPanelTransitionCtrl로부터 호출받아 Fade 애니메이션 실행  
/// - 애니메이션 종료 시 ReadyPanelTransitionCtrl에 완료 신호 전달
/// </summary>
public class FadeAnimationCtrl : MonoBehaviour
{
    [Header("Setting Component")]
    [SerializeField] private InitCtrl _initCtrl;
    [Space(10)]
    [SerializeField] private Animator _fadeAnimator;
    [SerializeField] private ReadyPanelTransitionCtrl _readyPanelTransitionCtrl;
    [SerializeField] private FilmingPanelCtrl _filmingPanelCtrl;
    [SerializeField] private FilmingToSelectCtrl _filmingToSelectCtrl;

    public int _isStateStep = 0;

    /// <summary>
    /// Fade 시작 (버튼 클릭 시 ReadyPanelTransitionCtrl에서 호출)
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
        //print($"_isStateStep : {_isStateStep}");
    }
    /// <summary>
    /// 애니메이션 이벤트(Animation Event)에서 호출됨  
    /// - Fade 종료 시 Animator 상태 복원 및 패널 전환 요청
    /// </summary>
    public void OnFadeEnd()
    {
        if (_fadeAnimator != null)
        {
            _fadeAnimator.SetBool("Fade", false);
            SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._fadeOut);

            // 래드화면에서 "시작하기" 눌러서 넘어갈때
            if (_isStateStep == 0)
            {
                //UnityEngine.Debug.Log("_isStateStep : 0");
                _isStateStep = 1;
                // ReadyPanelTransitionCtrl에 완료 신호 전달
                if (_readyPanelTransitionCtrl != null)
                {
                    _readyPanelTransitionCtrl.OnFadeFinished();
                }
                else
                {
                    UnityEngine.Debug.LogWarning("_readyPanelTransitionCtrl reference is missing");
                }
            }
            // 사진찍기 버튼 클릭해서 카메라 화면으로 이동할때
            else if (_isStateStep == 1)
            {
                //UnityEngine.Debug.Log("_isStateStep : 1");
                _isStateStep = 2;

                if (_filmingPanelCtrl != null)
                {
                    _filmingPanelCtrl.PanelChanger();
                }
                else
                {
                    UnityEngine.Debug.LogWarning("_filmingPanelCtrl reference is missing");
                }
            }
            // 촬영 끝나고 래디화면으로 돌아갈때
            else if (_isStateStep == 2)
            {
                // 현재 스탭 Max = 2
                // step > 2 일 경우 초기화 : 0
                //UnityEngine.Debug.Log("_isStateStep : 2");
                _isStateStep = 0;
                _initCtrl.PanaelActiveCtrl();
            }
            // 카메라 화면해서 백 버튼 클릭시    : 패널 추가 될 수 있어서 초기 상태로 돌아가는 로직은 큰 숫자로 설정
            // 백 버튼 클릭 후 _isStateStep = 100으로 변경 -> 아래 로직에서 _isStateStep = 1로 변경
            // 흐름 : 백2 클릭전 -> 백100 클릭후 -> 셀렉화면 1
            else if (_isStateStep == 100)
            {
                UnityEngine.Debug.Log("_isStateStep : greater than 100");
                _isStateStep = 1;
                _filmingToSelectCtrl.PanaelActiveCtrl();
            }
            else
            {
                UnityEngine.Debug.Log("_isStateStep : else");
                // nothing
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning("_fadeAnimator reference is missing");
        }



    }
}


