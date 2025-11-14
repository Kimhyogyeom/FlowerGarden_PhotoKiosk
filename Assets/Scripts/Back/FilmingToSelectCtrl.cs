using UnityEngine;
using UnityEngine.UI;

public class FilmingToSelectCtrl : MonoBehaviour
{
    [Header("Component Settings")]
    [SerializeField] private FadeAnimationCtrl _fadeAnimationCtrl;

    [Header("Object Settings")]
    [SerializeField] private Button _filmingToSelectButton;

    [Header("Panel Settings")]
    [SerializeField] private GameObject _currentPanel;
    [SerializeField] private GameObject _changePanel;

    private void Awake()
    {
        if (_filmingToSelectButton != null)
        {
            _filmingToSelectButton.onClick.AddListener(OnFilimingToSelectCtrl);
        }
        else
        {
            Debug.LogWarning("_filmingToSelectButton reference is missing");
        }
    }
    public void OnFilimingToSelectCtrl()
    {
        GameManager.Instance.SetState(KioskState.Select);
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._backButton);

        _fadeAnimationCtrl._isStateStep = 100;
        _fadeAnimationCtrl.StartFade();
    }
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
    // ??????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????
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
