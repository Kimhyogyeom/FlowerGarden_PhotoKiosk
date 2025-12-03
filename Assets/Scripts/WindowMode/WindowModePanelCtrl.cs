using UnityEngine;
using UnityEngine.UI;

public class WindowModePanelCtrl : MonoBehaviour
{
    [Header("Component Setting")]
    [SerializeField] private FadeAnimationCtrl _fadeAnimationCtrl;
    [SerializeField] private FramePanelScaleInCtrl _framePanelScaleInCtrl;

    [Header("프레임 가로")]
    [SerializeField] private Button _frameWidth;            // 프레임 가로
    [SerializeField] private GameObject _frameWidthLine;    // 프레임 라인

    [Header("프레임 세로")]
    [SerializeField] private Button _frameHight;            // 프레임 세로    
    [SerializeField] private GameObject _frameHightLine;    // 프레임 라인

    [Header("다음 버튼")]
    [SerializeField] private Button _nextButton;    // 다음 버튼

    [Header("Object Setting")]
    [SerializeField] private GameObject _currentPanel;
    [SerializeField] private GameObject _changePanel;

    [Header("Frame Object Setting")]
    [SerializeField] private GameObject _frameHightObject;
    [SerializeField] private GameObject _frameWidthObject;
    [SerializeField] private bool _hightWidthFlag = true;
    void Awake()
    {
        // 가로/세로 프레임 모드
        _frameWidth.onClick.AddListener(OnClickFrameWidth);
        _frameHight.onClick.AddListener(OnClickFrameHight);
        // 페이드 스타트
        _nextButton.onClick.AddListener(OnClickFadeStart);
    }
    /// <summary>
    /// 프레임 가로 클릭
    /// </summary>
    private void OnClickFrameWidth()
    {
        GameManager.Instance.SetMode(KioskMode.Hight);
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._buttonClickSound);

        _frameWidthLine.SetActive(true);
        _frameHightLine.SetActive(false);

        _hightWidthFlag = true;
        FrameObjectSetting(_hightWidthFlag);
        _framePanelScaleInCtrl._selectedIndex = 0;
    }
    /// <summary>
    /// 프레임 세로 클릭
    /// </summary>
    private void OnClickFrameHight()
    {
        GameManager.Instance.SetMode(KioskMode.Width);
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._buttonClickSound);

        _frameWidthLine.SetActive(false);
        _frameHightLine.SetActive(true);

        _hightWidthFlag = false;
        FrameObjectSetting(_hightWidthFlag);
        _framePanelScaleInCtrl._selectedIndex = 3;
    }

    /// <summary>
    /// 페이드 스타트 
    /// </summary>
    private void OnClickFadeStart()
    {
        _fadeAnimationCtrl.StartFade();
    }

    public void FadeFinishEvent()
    {
        GameManager.Instance.SetState(KioskState.Select);
        if (_currentPanel != null) _currentPanel.SetActive(false);
        if (_changePanel != null) _changePanel.SetActive(true);
    }
    private void FrameObjectSetting(bool flag)
    {
        if (flag)
        {
            _frameHightObject.SetActive(true);
            _frameWidthObject.SetActive(false);
        }
        else
        {
            _frameHightObject.SetActive(false);
            _frameWidthObject.SetActive(true);
        }
    }
    public void ModeAllReset()
    {
        // 기본 모드로 되돌리기
        OnClickFrameWidth();
    }
}
