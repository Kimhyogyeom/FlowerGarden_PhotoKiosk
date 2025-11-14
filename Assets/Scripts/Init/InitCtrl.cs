// ´ë±â

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InitCtrl : MonoBehaviour
{
    [Header("Add")]
    [SerializeField] private Button _initButton;
    [SerializeField] private TextMeshProUGUI _initText;
    private Coroutine _resetCallbackRoutine = null;
    [SerializeField] private int _successToBackTime = 10;

    [Header("Setting Component")]
    [SerializeField] private PhotoFrameSelectCtrl _photoFrameSelectCtrl;
    [SerializeField] private PrintController _printController;
    [SerializeField] private FadeAnimationCtrl _fadeAnimationCtrl;
    [SerializeField] private PrintButtonHandler _printButtonHandler;
    [SerializeField] private StepCountdownUI _stepCountdownUI;
    [SerializeField] private FilmingToSelectCtrl _filmingToSelectCtrl;
    [SerializeField] private FilmingEndCtrl _filmingEndCtrl;

    [Header("Setting Object")]
    [SerializeField] private Button _photoButton;
    [SerializeField] private GameObject _photoButtonFake;
    [SerializeField] private Image _photoImage;
    [SerializeField] private TextMeshProUGUI _buttonText;
    private ColorBlock _originColor;
    [Space(10)]
    [SerializeField] private GameObject _currentPanel;  // ÇöÀç ÀÎ¼â ¿Ï·á ÈÄ ÆÐ³Î
    [SerializeField] private GameObject _changePanel;   // Ã¼ÀÎÁö µÉ ÆÐ³Î (ÇöÀç °áÁ¦ ÆÐ³ÎÀÓ)
    [SerializeField] private GameObject _cameraFocus;   // Ä«¸Þ¶ó Á¶ÁØÁ¡

    [Header("Filming")]
    [SerializeField] private GameObject _stepsObject;   // 1~5 ½ºÅÜ 
    [SerializeField] private string _takePictureString = "»çÁøÂï±â";
    [SerializeField] private TextMeshProUGUI _exitMessageText;
    [SerializeField, TextArea(4, 5)]
    private string _exitMessageString = "»çÁø ÃÔ¿µÀÌ Á¾·áµÇ¾ú½À´Ï´Ù.\n»çÁøÀ» Ãâ·ÂÇÏ¼¼¿ä.";

    [SerializeField] private GameObject _exitMessage;

    [SerializeField] private GameObject[] _photoNumberObjs;
    [SerializeField] private TextMeshProUGUI _missionText;

    [Header("Test")]
    [SerializeField] private GameObject _startFilming;
    [SerializeField] private GameObject _endFilming;
    [SerializeField] private Button _endFilimgButton;       

    [SerializeField] private GameObject _filimgObject;           // ÃÔ¿µ Áß ¹öÆ° ¿ÀºêÁ§Æ®
    [SerializeField] private GameObject _finishedFilimgObject;   // ÃÔ¿µ ³¡ ¹öÆ° ¿ÀºêÁ§Æ®
    [SerializeField] private Image _progressFillImage;

#pragma warning disable CS0414
    [Range(1f, 10f)]
    [Header("TimeScale Value")]
    [Tooltip("±âº» : 1, ¸ß½º : 10")]
    [SerializeField] private float _timeScale = 1.0f;
#pragma warning disable CS0414

    private void Awake()
    {
        // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
        // Test ¼Óµµ Áõ°¡
        Time.timeScale = 1f;
        // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

        _initButton.onClick.AddListener(ResetManager);
        _originColor = _photoButton.colors;
    }
    
    /// <summary>
    /// ºñÈ°¼ºÈ­ µÉ ¶§ È¤½Ã ¸ð¸¦ ÄÚ·çÆ¾ ½ÇÇà ¹æÁö
    /// </summary>
    private void OnDisable()
    {
        if (_resetCallbackRoutine != null)
        {
            StopCoroutine(_resetCallbackRoutine);
            _resetCallbackRoutine = null;
        }
    }

    /// <summary>
    /// ÆÄ±«µÉ ¶§ È¤½Ã ¸ð¸¦ ÄÚ·çÆ¾ ½ÇÇà ¹æÁö? ¹æ¾î¿ëÀÎµ¥ ÆÄ±«µÉ ÀÏÀº ¾øÀ»µí
    /// </summary>
    private void OnDestroy()
    {
        if (_resetCallbackRoutine != null)
        {
            StopCoroutine(_resetCallbackRoutine);
            _resetCallbackRoutine = null;
        }
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡ ¹öÆ° Å¬¸¯ ¾ÈÇßÀ»¶§ È£Ãâ ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ÇöÀç 180ÃÊ µ¿¾È "µ¹¾Æ°¡±â" ¹öÆ°À» Å¬¸¯ÇÏÁö ¾ÊÀ¸¸é, ÀÚµ¿À¸·Î ÃÊ±â È­¸éÀ¸·Î µÇµ¹¾Æ°¨
    public void ResetCallBack()
    {        
        //print("111");
        if (_resetCallbackRoutine != null)
        {
            //print("222");
            StopCoroutine(_resetCallbackRoutine);
            _resetCallbackRoutine = null;
            // ÅØ½ºÆ®µµ ÃÊ±âÈ­            
        }        
        _resetCallbackRoutine = StartCoroutine(ResetCallBackCoroutine());
    }

    private IEnumerator ResetCallBackCoroutine()
    {        
        for (int i = _successToBackTime; i >= 1; i--)
        {
            if (_initText != null)
                _initText.text = $"{i}\nµ¹¾Æ°¡±â";

            yield return new WaitForSeconds(1f);
        }

        //print("¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡");
        //print("¹öÆ° Å¬¸¯ ¾ÈÇÏ¸é Àý´ë ½ÇÇà ¾ÈµÅ¾ßÇÔ ...");
        //print("¹Ý´ë·Î ¹öÆ° Å¬¸¯ ¾ÈÇÏ¸é ½ÇÇàµÅ¾ßÇÏÁö ÇÏÇÏÇÏÇÏÇÏÇÏÇÏÇÏÇãÇÏ¤ÃÇÏ¤ÃÇÏ¤ÃÇÏ¤ÃÇÏÇãÇÏ");
        //print("¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡");

        ResetManager();        
    }
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡  
    // ¤·¤·
    /// <summary>
    /// ¸®¼Â ÃÑ °ü¸®ÀÚ
    /// </summary>
    private void ResetManager()
    {
        if (_resetCallbackRoutine != null)
        {
            StopCoroutine(_resetCallbackRoutine);
            _resetCallbackRoutine = null;
        }

        // ÄÚ·çÆ¾Àº ÃÊ±âÈ­
        _resetCallbackRoutine = null;

        // ÅØ½ºÆ®µµ ÃÊ±âÈ­
        if (_initText != null)
            _initText.text = "5\nµ¹¾Æ°¡±â";

        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._outputSuccess);
        _fadeAnimationCtrl.StartFade();
        // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
        FrameSelectReset();     // ÇÁ·¹ÀÓ °ü·Ã ¸®¼Â
        FilmingPanelReset();    // ÃÔ¿µ ÆÐ³Î °ü·Ã ¸®¼Â
        CaptureReset();         // Ä¸Ã³ °ü·Ã ¸®¼Â
        PrintHandlerReset();    // ÇÚµé·¯ °ü·Ã ¸®¼Â
        PrintReset();           // ÇÁ¸°Æ® °ü·Ã ¸®¼Â        
        // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
        ButtonReset();          // ¹öÆ° °ü·Ã ¸®¼Â : °ËÅä ÇÊ¿ä
    }

    /// <summary>
    /// ÇÁ·¹ÀÓ ¼±ÅÃ °ü·Ã ¸®¼Â
    /// </summary>
    private void FrameSelectReset()
    {
        _photoFrameSelectCtrl.AllReset();
    }

    /// <summary>
    /// ÃÔ¿µ ÆÐ³Î °ü·Ã ¸®¼Â
    /// </summary>
    private void FilmingPanelReset()
    {
        _stepsObject.SetActive(true);

        _photoButton.colors = _originColor;
        _buttonText.color = Color.black;
        _buttonText.text = _takePictureString;

        _photoButtonFake.SetActive(false);

        _exitMessageText.text = _exitMessageString;
        _exitMessage.SetActive(false);

        _cameraFocus.SetActive(true);

        // ¹Ì¼Ç ÅØ½ºÆ® Ä«¿îÆ® ÃÊ±âÈ­
        _stepCountdownUI._missionCount = 0;
        // ¹Ì¼Ç ÅØ½ºÆ® ÃÊ±âÈ­
        _missionText.text = "";

        // Æ÷Åä > (ºñÈ°¼ºÈ­ µÇ´ø ·ÎÁ÷ ³Ñ¹ö ÀÌ¹ÌÁö ´Ù½Ã È°¼ºÈ­)
        foreach (var item in _photoNumberObjs)
        {
            item.SetActive(true);
        }
    }

    /// <summary>
    /// Ä¸Ã³ °ü·Ã ¸®¼Â
    /// </summary>
    private void CaptureReset()
    {
        _stepCountdownUI.ResetSequence();
    }

    /// <summary>
    /// ÇÁ¸°Æ® ÇÚµé·¯ °ü·Ã ¸®¼Â
    /// ÇöÀç) Ãâ·ÂÁß -> Ãâ·ÂÇÏ±â ¹öÆ°À» Å¬¸¯ÇÏÁö ¾Ê¾ÒÀ» ¶§ ÀÚµ¿À¸·Î ³Ñ¾î°¡±â À§ÇÑ ¼¼ÆÃ ÃÊ±âÈ­
    /// </summary>
    private void PrintHandlerReset()
    {
        _printButtonHandler.ResetPrintButtonHandler();
    }

    /// <summary>
    /// ÇÁ¸°Æ® ¸®¼Â °ü·Ã
    /// </summary>
    private void PrintReset()
    {
        _printController.ResetPrintState();
    }

    /// <summary>
    /// ÃÊ±âÈ­ ÇÒ ¹öÆ° °ü·Ã
    /// Ä«¸Þ¶ó ¾ø´Â °ü·ÃÀ¸·Î ÀÓ½Ã Å×½ºÆ® ÁøÇàÁßÀÎµ¥
    /// ·ÎÁ÷À» ¿©±â¿¡ ÀÛ¼ºÇØµµ µÉÁö ÀÛ¼º ¹× °ËÅä ÇÊ¿ä
    /// </summary>
    private void ButtonReset()
    {
        _startFilming.SetActive(true);
        _endFilming.SetActive(false);
        _endFilimgButton.interactable = true;
        _printButtonHandler._busy = false;

        _filimgObject.SetActive(true);
        _finishedFilimgObject.SetActive(false);

        // ÇÁ·Î±×·¡½º¹Ù ÃÊ±âÈ­
        _progressFillImage.fillAmount = 0;

        // µÚ·Î°¡±â ¹öÆ°
        _filmingToSelectCtrl.ButtonActive();
    }

    /// <summary>
    /// ÃÔ¿µ Á¾·á -> ·¡µð È­¸éÇÁ·¹ÀÓÀ¸·Î ÀüÈ¯
    /// 1110 ÇöÀç ¼öÁ¤ÁßÀÓ
    /// </summary>
    public void PanaelActiveCtrl()
    {
        GameManager.Instance.SetState(KioskState.WaitingForPayment);
        _currentPanel.SetActive(false);
        _changePanel.SetActive(true);
    }
}
