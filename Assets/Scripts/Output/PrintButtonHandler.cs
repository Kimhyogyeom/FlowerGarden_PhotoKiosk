using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrintButtonHandler : MonoBehaviour
{
    [Header("Settings Object")]
    [SerializeField] private Button _outputButton;

    [SerializeField] private RawImage _currentRawImage;
    [SerializeField] private RawImage _changeRawImage;
    [SerializeField] private RawImage _changeFakeRawImage;

    [SerializeField] private Image[] _currentImages;
    [SerializeField] private Image[] _changeImages;

    [SerializeField] private GameObject _currentPanel;
    [SerializeField] private GameObject _changePanel;

    [Header("References")]
    [SerializeField] private PrintController _printController;
    [SerializeField] private RawImage _targetRawImage;   // 인쇄할 RawImage

    [Header("Optional")]
    [SerializeField] private GameObject[] _hideWhileCapture; // 캡처 중 숨김

    [Header("Countdown")]
    [SerializeField] private float _countTime = 60f; // 초
    //[SerializeField] private Text _countdownTextUGUI;           
    [SerializeField] private TextMeshProUGUI _countdownTMP; // TMP 사용 시

    public bool _busy;

    private Coroutine _countdownRoutine;

#pragma warning disable CS0414
    private bool _autoTriggered = false; // 카운트다운으로 자동 호출했는지 여부(중복 방지)
#pragma warning disable CS0414

    private void Awake()
    {
        if (_outputButton != null)
            _outputButton.onClick.AddListener(OnClickPrint);
        else
            Debug.LogWarning("_outputButton reference is missing");
    }

    private void OnEnable()
    {
        OutputEnableBroadcaster.OnOutputEnabled += StartCorutineToEvent;
    }

    private void OnDisable()
    {
        OutputEnableBroadcaster.OnOutputEnabled -= StartCorutineToEvent;
        // 비활성화 시 카운트다운 정지 + 초기화
        //StopCountdown();
        //SetCountdownText(string.Empty);
        //_autoTriggered = false;
    }

    private void OnDestroy()
    {
        if (_outputButton != null)
            _outputButton.onClick.RemoveListener(OnClickPrint);
    }

    public void OnClickPrint()
    {
        if (_busy) return;

        // 사용자가 버튼을 눌러 인쇄 시작 → 카운트다운 즉시 중지
        StopCountdown();
        _autoTriggered = false;

        GameManager.Instance.SetState(KioskState.Printing);
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._filmingOutputButton);

        // 필수 레퍼런스 체크
        if (_printController == null || _targetRawImage == null)
        {
            Debug.LogWarning("PrintController/RawImage reference is missing");
            return;
        }

        // -------- 이미지 교체 작업 --------
        if (_changeRawImage != null && _currentRawImage != null)
        {
            if (_changeFakeRawImage != null)
                _changeFakeRawImage.texture = _currentRawImage.texture; // 페이크 RawImage 텍스처 복사
            _changeRawImage.texture = _currentRawImage.texture;        // RawImage 텍스처 복사
        }

        if (_currentImages != null && _changeImages != null)
        {
            int n = Mathf.Min(_currentImages.Length, _changeImages.Length);
            for (int i = 0; i < n; i++)
            {
                var src = _currentImages[i];
                var dst = _changeImages[i];
                if (src == null || dst == null) continue;
                dst.sprite = src.sprite; // Sprite 복사
            }
        }

        // -------- 패널 토글 --------
        if (_currentPanel) _currentPanel.SetActive(false);
        if (_changePanel) _changePanel.SetActive(true);

        // -------- 출력 호출 --------
        _busy = true;
        if (_outputButton) _outputButton.interactable = false;

        _printController.PrintRawImage(
            _targetRawImage,
            onDone: () =>
            {
                Debug.Log("완료"); // 인쇄 완료 후 후속 처리 지점
                _busy = false;
                if (_outputButton) _outputButton.interactable = true;
                SetCountdownText(string.Empty); // 완료 시 카운트 텍스트 클리어(선택)
            },
            toHideTemporarily: _hideWhileCapture
        );
    }

    // ===== Countdown =====
    /// <summary>
    /// 코루틴 실행용 함수 : Event
    /// </summary>
    private void StartCorutineToEvent()
    {
        if (_countdownRoutine == null)
            _countdownRoutine = StartCoroutine(CountdownAndAutoPrint());
    }
    //private void EndCorutineToEvent()
    //{

    //}
    private IEnumerator CountdownAndAutoPrint()
    {
        //print("진입:::");
        _autoTriggered = false;

        float remain = Mathf.Max(0f, _countTime);
        int lastShown = -1;

        // 최초 표기
        UpdateCountdownLabel(remain, force: true);

        // 1초 간격으로 표기(타임스케일 영향 없음)
        while (remain > 0f)
        {
            yield return new WaitForSeconds(1f);
            remain = Mathf.Max(0f, remain - 1f);

            // 같은 숫자 반복 갱신 방지
            if ((int)remain != lastShown)
            {
                UpdateCountdownLabel(remain);
                //print($"{remain}:::");
            }
        }

        // 이미 바깥에서 인쇄가 시작됐다면(= _busy) 자동 호출 생략
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
        SetCountdownText(sec.ToString()); // 필요하면 "초" 붙이거나 포맷 변경
    }

    private void SetCountdownText(string scond)
    {
        if (_countdownTMP) _countdownTMP.text = scond;
        //if (_countdownTextUGUI) _countdownTextUGUI.text = s;
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
    /// 리셋 함수
    /// </summary>
    public void ResetPrintButtonHandler()
    {
        StopCountdown();
        SetCountdownText(string.Empty);
        _autoTriggered = false;
    }
}
