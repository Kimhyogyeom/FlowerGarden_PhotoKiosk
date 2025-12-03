using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 출력 버튼 + 자동 카운트다운 컨트롤러
/// - 출력 버튼 클릭 시: 패널/이미지 교체 → PrintController 호출 → 인쇄 진행
/// - OutputEnableBroadcaster 이벤트 수신 시: 카운트다운 시작
///   → 시간이 다 되면 자동으로 OnClickPrint() 호출(자동 인쇄)
/// </summary>
public class PrintButtonHandler : MonoBehaviour
{
    [Header("Component Setting")]
    [SerializeField] private FadeAnimationCtrl _fadeAnimationCtrl;
    [SerializeField] private PrintPhotoImageMapping _imageMapping;

    [Header("Settings Object")]
    [SerializeField] private Button _outputButton;
    // 출력(인쇄) 버튼

    [SerializeField] private RawImage _currentRawImage;
    // 현재까지 작업된 사진(원본) RawImage

    [SerializeField] private RawImage _changeRawImage;
    // 인쇄 직전/후에 보여줄 변경된 RawImage

    [SerializeField] private RawImage _changeFakeRawImage;
    // 필요 시, 임시/페이크로 사용하는 RawImage (애니메이션 등 구성용)

    [SerializeField] private Image[] _currentImages;
    // 현재 화면에 표시 중인 썸네일/미리보기 이미지들

    [SerializeField] private Image[] _changeImages;
    // 출력 화면에서 사용할 썸네일/미리보기 이미지들

    [Header("Panels")]
    [SerializeField] private GameObject _currentPanel;
    // 출력 버튼을 누르기 전까지 사용하던 패널

    [SerializeField] private GameObject _changePanel;
    // 출력 버튼을 누른 뒤 보여줄 패널 (예: 인쇄 준비/진행 화면)

    [SerializeField] private int _originTimerValue = 0;

    [Header("References")]
    [SerializeField] private PrintController _printController;
    // 실제 캡처/파일 저장/인쇄를 수행하는 PrintController

    [Header("Print Target (Hight / Width)")]
    [SerializeField] private Image _targetRawImage;        // Hight 모드 인쇄 대상
    [SerializeField] private Image _targetRawImageWidth;   // Width 모드 인쇄 대상

    [Header("Optional")]
    [SerializeField] private GameObject[] _hideWhileCapture; // 캡처 중 숨김
    // 캡처 시 화면에 찍히지 않길 원하는 UI 오브젝트들

    [Header("Countdown")]
    [SerializeField] private TextMeshProUGUI _countdownTMP; // TMP 사용 시
    // 카운트다운 숫자를 표시할 TMP 텍스트

    public bool _busy;
    // true 인 동안에는 인쇄 중으로 간주하고 버튼 재클릭/자동 호출 방지

    private Coroutine _countdownRoutine;
#pragma warning disable CS0414
    private bool _autoTriggered = false; // 카운트다운으로 자동 호출했는지 여부(중복 방지)
#pragma warning restore CS0414

    // ─────────────────────────────────────────────────────────────
    // 모드 헬퍼 / 출력 대상 선택 (나머지 이미지는 전부 공용 사용)
    // ─────────────────────────────────────────────────────────────

    private bool IsHightMode
    {
        get
        {
            if (GameManager.Instance == null) return true;
            return GameManager.Instance.CurrentMode == KioskMode.Hight;
        }
    }

    private Image TargetImage
    {
        get
        {
            // Kiosk 모드에 따라 최종 출력 대상만 분기
            return IsHightMode ? _targetRawImage : _targetRawImageWidth;
        }
    }

    private void Awake()
    {
        if (_outputButton != null)
            _outputButton.onClick.AddListener(OnClickPrint);
        else
            Debug.LogWarning("_outputButton reference is missing");

        if (GameManager.Instance != null)
            _originTimerValue = GameManager.Instance._photoSelectToPrintTimer;
    }

    private void OnEnable()
    {
        // 출력 화면이 활성화될 때 이벤트 등록
        OutputEnableBroadcaster.OnOutputEnabled += StartCorutineToEvent;
    }

    private void OnDisable()
    {
        OutputEnableBroadcaster.OnOutputEnabled -= StartCorutineToEvent;
    }

    private void OnDestroy()
    {
        if (_outputButton != null)
            _outputButton.onClick.RemoveListener(OnClickPrint);
    }

    /// <summary>
    /// 사용자가 직접 "출력" 버튼을 눌렀을 때 호출
    /// - 지금은 페이드만 시작, 실제 작업은 FadeEndCallBack 에서
    /// </summary>
    public void OnClickPrint()
    {
        _fadeAnimationCtrl.StartFade();
    }

    /// <summary>
    /// 페이드 끝나면 호출될 녀석
    /// </summary>
    public void FadeEndCallBack()
    {
        if (_busy) return;

        GameManager.Instance.SetState(KioskState.Printing);

        // 사용자가 버튼을 눌러 인쇄 시작 → 카운트다운 즉시 중지
        StopCountdown();
        _autoTriggered = false;

        // 필수 레퍼런스 체크 (모드에 따라 TargetImage만 분기)
        if (_printController == null || TargetImage == null)
        {
            Debug.LogWarning("PrintController/TargetImage reference is missing");
            return;
        }

        // -------- 이미지 교체 작업 --------
        // RawImage 텍스처 복사 (공용)
        if (_changeRawImage != null && _currentRawImage != null)
        {
            if (_changeFakeRawImage != null)
                _changeFakeRawImage.texture = _currentRawImage.texture; // 페이크 RawImage 텍스처 복사

            _changeRawImage.texture = _currentRawImage.texture;        // 출력용 RawImage 텍스처 복사
        }

        // 썸네일 Sprite 복사 (공용)
        if (_currentImages != null && _changeImages != null)
        {
            int n = Mathf.Min(_currentImages.Length, _changeImages.Length);
            for (int i = 0; i < n; i++)
            {
                var src = _currentImages[i];
                var dst = _changeImages[i];
                if (src == null || dst == null) continue;
                dst.sprite = src.sprite; // 썸네일 Sprite 복사
            }
        }

        // -------- 패널 토글 --------
        if (_currentPanel) _currentPanel.SetActive(false);
        if (_changePanel) _changePanel.SetActive(true);

        // -------- 출력 호출 준비 --------
        _busy = true;
        if (_outputButton) _outputButton.interactable = false;

        // 인쇄용 프레임/그리드 이미지 매핑
        if (_imageMapping != null)
        {
            _imageMapping.ImageMappingCallBack();
        }

        // -------- 실제 인쇄 호출 --------
        _printController.PrintRawImage(
            TargetImage,
            onDone: () =>
            {
                Debug.Log("인쇄 완료");
                _busy = false;
                if (_outputButton) _outputButton.interactable = true;
                SetCountdownText(string.Empty); // 완료 시 카운트 텍스트 클리어(선택)
            },
            toHideTemporarily: _hideWhileCapture
        );

        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._buttonClickSound);
    }

    // ===== Countdown =====

    private void StartCorutineToEvent()
    {
        if (_countdownRoutine == null)
            _countdownRoutine = StartCoroutine(CountdownAndAutoPrint());
    }

    private IEnumerator CountdownAndAutoPrint()
    {
        _autoTriggered = false;

        float remain = _originTimerValue;
        int lastShown = -1;

        // 최초 표기
        UpdateCountdownLabel(remain, force: true);

        // 1초 간격으로 표기
        while (remain > 0f)
        {
            yield return new WaitForSeconds(1f);
            remain = Mathf.Max(0f, remain - 1f);

            if ((int)remain != lastShown)
            {
                UpdateCountdownLabel(remain);
            }
        }

        // 이미 인쇄 중이면 자동 호출 생략
        if (!_busy && gameObject.activeInHierarchy)
        {
            _autoTriggered = true;
            OnClickPrint(); // 자동 인쇄
        }

        _countdownRoutine = null;
    }

    private void UpdateCountdownLabel(float remain, bool force = false)
    {
        int sec = Mathf.CeilToInt(remain);
        SetCountdownText(sec.ToString());
    }

    private void SetCountdownText(string second)
    {
        if (_countdownTMP) _countdownTMP.text = second;
    }

    private void StopCountdown()
    {
        if (_countdownRoutine != null)
        {
            StopCoroutine(_countdownRoutine);
            _countdownRoutine = null;
        }
    }

    /// <summary>
    /// 외부에서 호출하는 "타이머 리셋 + 처음부터 다시 시작" 함수
    /// </summary>
    public void ResetAndRestartCountdown()
    {
        StopCountdown();
        SetCountdownText(string.Empty);
        _autoTriggered = false;

        _busy = false;
        if (_outputButton != null)
            _outputButton.interactable = true;

        // 필요하면 여기서 다시 StartCoroutine 호출
        // if (gameObject.activeInHierarchy)
        //     _countdownRoutine = StartCoroutine(CountdownAndAutoPrint());
    }
}
