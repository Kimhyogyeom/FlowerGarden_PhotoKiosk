using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 촬영 화면에서 프레임 선택 화면(Select)으로 돌아가는 컨트롤러
/// - 뒤로 가기 버튼 클릭 시 페이드 애니메이션을 요청
/// - 페이드가 끝난 뒤 패널을 실제로 전환(FadeAnimationCtrl 쪽에서 호출)
/// </summary>
public class FilmingToSelectCtrl : MonoBehaviour
{
    [Header("Component Settings")]
    [SerializeField] private FadeAnimationCtrl _fadeAnimationCtrl;   // 페이드 애니메이션을 담당하는 컨트롤러

    [Header("Object Settings")]
    [SerializeField] private Button _filmingToSelectButton;          // 촬영 → 선택 화면으로 돌아가는 버튼

    [Header("Panel Settings")]
    [SerializeField] private GameObject _currentPanel;               // 현재(촬영) 패널
    [SerializeField] private GameObject _changePanel;                // 바뀔(프레임 선택) 패널

    private void Awake()
    {
        // 버튼이 정상적으로 연결되어 있으면 클릭 이벤트 등록
        if (_filmingToSelectButton != null)
        {
            _filmingToSelectButton.onClick.AddListener(OnFilimingToSelectCtrl);
        }
        else
        {
            Debug.LogWarning("_filmingToSelectButton reference is missing");
        }
    }

    /// <summary>
    /// 촬영 화면에서 "뒤로 가기" 버튼 클릭 시 호출
    /// - 상태를 Select로 변경
    /// - 뒤로가기 사운드 재생
    /// - FadeAnimationCtrl에 state step(100) 설정 후 페이드 시작
    ///   (페이드 종료 후 FadeAnimationCtrl에서 다시 Select 패널로 전환)
    /// </summary>
    public void OnFilimingToSelectCtrl()
    {
        GameManager.Instance.SetState(KioskState.Select);
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._backButton);

        // 100은 FadeAnimationCtrl에서 "촬영 → 선택 화면으로 복귀" 케이스를 구분하기 위한 값
        _fadeAnimationCtrl._isStateStep = 100;
        _fadeAnimationCtrl.StartFade();
    }

    /// <summary>
    /// 실제 패널 전환 (촬영 → 선택)
    /// - FadeAnimationCtrl.OnFadeEnd()에서 호출
    /// </summary>
    public void PanaelActiveCtrl()
    {
        if (_currentPanel != null && _changePanel != null)
        {
            _currentPanel.SetActive(false);
            _changePanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("_currentPanel or _changePanel reference is missing");
        }
    }

    /// <summary>
    /// 뒤로 가기 버튼 활성화
    /// - 초기 상태나 리셋 시 다시 보이게 할 때 사용
    /// </summary>
    public void ButtonActive()
    {
        if (_filmingToSelectButton != null)
        {
            _filmingToSelectButton.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("_filmingToSelectButton reference is missing");
        }
    }

    /// <summary>
    /// 뒤로 가기 버튼 비활성화
    /// - 촬영 중에는 뒤로 갈 수 없게 숨기기 위해 사용
    /// </summary>
    public void ButtonInActive()
    {
        if (_filmingToSelectButton != null)
        {
            _filmingToSelectButton.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("_filmingToSelectButton reference is missing");
        }
    }
}
