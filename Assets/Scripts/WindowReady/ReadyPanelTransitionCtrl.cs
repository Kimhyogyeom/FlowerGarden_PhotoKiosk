using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [Ready Window → Next Step] 전환 컨트롤러  
/// - 시작 버튼 클릭 시 FadeAnimationCtrl을 통해 페이드 효과 실행  
/// - 페이드 완료 후 ReadyPanel 비활성화, CameraPanel 활성화
/// </summary>
public class ReadyPanelTransitionCtrl : MonoBehaviour
{
    [Header("Setting Component")]
    [SerializeField] private FadeAnimationCtrl _fadeAnimationCtrl;  

    [Header("Setting Object")]
    [SerializeField] private GameObject _readyPanel;    
    [SerializeField] private GameObject _cameraPanel;   
    [SerializeField] private Button _startButton;       

    /// <summary>
    /// 버튼 클릭 이벤트 등록
    /// </summary>
    private void Awake()
    {        
        if (_startButton != null)
        {
            _startButton.onClick.AddListener(OnReadyClicked);
        }
        else
        {
            Debug.LogWarning("_startButton reference is missing");
        }
    }

    /// <summary>
    /// 메모리 누수 방지용 리스너 해제
    /// </summary>
    private void OnDestroy()
    {
        if (_startButton != null)
        {
            _startButton.onClick.RemoveListener(OnReadyClicked);
        }
        else
        {
            Debug.LogWarning("_startButton reference is missing");
        }
    }

    /// <summary>
    /// 시작 버튼 클릭 시 호출  
    /// → 페이드 애니메이션 실행 요청
    /// </summary>
    private void OnReadyClicked()
    {
        if (_fadeAnimationCtrl != null)
        {
            GameManager.Instance.SetState(KioskState.Select);
            _fadeAnimationCtrl.StartFade();
            SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._startButton);
        }
        else
        {
            Debug.LogWarning("_fadeAnimationCtrl reference is missing");
        }
    }

    /// <summary>
    /// FadeAnimationCtrl에서 페이드 완료 이벤트를 받았을 때 실행  
    /// → ReadyPanel 비활성화, CameraPanel 활성화
    /// </summary>
    public void OnFadeFinished()
    {
        if (_readyPanel != null && _cameraPanel != null)
        {
            _readyPanel.SetActive(false);
            _cameraPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("_readyPanel or _cameraPanel reference is missing");
        }
    }
}

