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
    // 페이드 에니메이션

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

    [SerializeField] private GameObject _currentPanel;
    // 출력 버튼을 누르기 전까지 사용하던 패널

    [SerializeField] private GameObject _changePanel;
    // 출력 버튼을 누른 뒤 보여줄 패널 (예: 인쇄 준비/진행 화면)

    private int _originTimerValue = 0;

    [Header("References")]
    [SerializeField] private PrintController _printController;
    // 실제 캡처/파일 저장/인쇄를 수행하는 PrintController

    [SerializeField] private RawImage _targetRawImage;   // 인쇄할 RawImage
    // PrintController.PrintRawImage 에 전달할 최종 타깃 RawImage

    [Header("Optional")]
    [SerializeField] private GameObject[] _hideWhileCapture; // 캡처 중 숨김
    // 캡처 시 화면에 찍히지 않길 원하는 UI 오브젝트들

    [Header("Countdown")]
    // [SerializeField] private float _countTime = 60f; // 초
    // 출력 대기 카운트다운 시간 (초 단위)

    //[SerializeField] private Text _countdownTextUGUI;           
    [SerializeField] private TextMeshProUGUI _countdownTMP; // TMP 사용 시
    // 카운트다운 숫자를 표시할 TMP 텍스트

    public bool _busy;
    // true 인 동안에는 인쇄 중으로 간주하고 버튼 재클릭/자동 호출 방지

    private Coroutine _countdownRoutine;
    // 카운트다운 코루틴 핸들

#pragma warning disable CS0414
    private bool _autoTriggered = false; // 카운트다운으로 자동 호출했는지 여부(중복 방지)
#pragma warning disable CS0414

    private void Awake()
    {
        if (_outputButton != null)
            _outputButton.onClick.AddListener(OnClickPrint);
        else
            Debug.LogWarning("_outputButton reference is missing");

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
        // 비활성화 시 카운트다운 정지 + 초기화 (필요 시 주석 해제)
        //StopCountdown();
        //SetCountdownText(string.Empty);
        //_autoTriggered = false;
    }

    private void OnDestroy()
    {
        if (_outputButton != null)
            _outputButton.onClick.RemoveListener(OnClickPrint);
    }

    /// <summary>
    /// 사용자가 직접 "출력" 버튼을 눌렀을 때 호출
    /// - 카운트다운 중지
    /// - 패널/이미지 교체
    /// - PrintController 를 통해 인쇄 요청
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
            _changeRawImage.texture = _currentRawImage.texture;        // 출력용 RawImage 텍스처 복사
        }

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

        // -------- 출력 호출 --------
        _busy = true;
        if (_outputButton) _outputButton.interactable = false;

        // 이식해야함 ───────────────────────────────────────────────────────────────────
        _printController.PrintRawImage(
            _targetRawImage,
            onDone: () =>
            {
                // 인쇄 완료 후 후속 처리 지점
                Debug.Log("완료");
                _busy = false;
                if (_outputButton) _outputButton.interactable = true;
                SetCountdownText(string.Empty); // 완료 시 카운트 텍스트 클리어(선택)
            },
            toHideTemporarily: _hideWhileCapture
        );
        // 이식해야함 ───────────────────────────────────────────────────────────────────
    }

    // ===== Countdown =====

    /// <summary>
    /// OutputEnableBroadcaster 이벤트에서 호출되는 코루틴 시작 함수
    /// - 출력 화면이 열릴 때 카운트다운 시작
    /// </summary>
    private void StartCorutineToEvent()
    {
        if (_countdownRoutine == null)
            _countdownRoutine = StartCoroutine(CountdownAndAutoPrint());
    }

    /// <summary>
    /// 카운트다운 코루틴
    /// - 지정 시간 동안 1초 단위로 감소
    /// - 시간이 0이 되면 자동으로 OnClickPrint() 호출 (단, 이미 인쇄 중이면 건너뜀)
    /// </summary>
    private IEnumerator CountdownAndAutoPrint()
    {
        //print("진입:::");
        _autoTriggered = false;

        float remain = Mathf.Max(0f, GameManager.Instance._photoSelectToPrintTimer);
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

    /// <summary>
    /// 남은 시간을 숫자로 변환해서 라벨 갱신
    /// </summary>
    private void UpdateCountdownLabel(float remain, bool force = false)
    {
        int sec = Mathf.CeilToInt(remain);
        SetCountdownText(sec.ToString()); // 필요하면 "초" 등의 접미사 추가 가능
    }

    /// <summary>
    /// 카운트다운 텍스트 설정 (TMP/UGUI 공용 처리 지점)
    /// </summary>
    private void SetCountdownText(string scond)
    {
        if (_countdownTMP) _countdownTMP.text = scond;
        //if (_countdownTextUGUI) _countdownTextUGUI.text = s;
    }

    /// <summary>
    /// 카운트다운 코루틴 중단
    /// </summary>
    private void StopCountdown()
    {
        if (_countdownRoutine != null)
        {
            StopCoroutine(_countdownRoutine);
            _countdownRoutine = null;
        }
    }

    /// <summary>
    /// 외부에서 호출하는 리셋 함수
    /// - 카운트다운 중지
    /// - 카운트 텍스트 초기화
    /// - 자동 호출 플래그 초기화
    /// (구)
    /// </summary>
    // public void ResetPrintButtonHandler()
    // {
    //     StopCountdown();
    //     SetCountdownText(string.Empty);
    //     _autoTriggered = false;
    // }

    /// <summary>
    /// 외부에서 호출하는 "타이머 리셋 + 처음부터 다시 시작" 함수
    /// - 카운트다운 중지
    /// - 텍스트 초기화
    /// - 플래그/상태 초기화
    /// - 다시 CountdownAndAutoPrint() 코루틴 시작
    /// </summary>
    public void ResetAndRestartCountdown()
    {
        // 타이머 origin 값으로 초기화
        GameManager.Instance._photoSelectToPrintTimer = _originTimerValue;

        // 1) 기존 카운트다운 완전히 정지 + 텍스트/플래그 리셋
        StopCountdown();
        SetCountdownText(string.Empty);
        _autoTriggered = false;

        // 인쇄 중 상태도 해제해줘야 다시 눌릴 수 있음
        _busy = false;
        if (_outputButton != null)
            _outputButton.interactable = true;

        // 2) 오브젝트가 활성 상태일 때만 다시 카운트다운 시작
        if (gameObject.activeInHierarchy)
        {
            _countdownRoutine = StartCoroutine(CountdownAndAutoPrint());
        }
        else
        {
            Debug.Log("[PrintButtonHandler] ResetAndRestartCountdown called, but GameObject is inactive.");
        }
    }
}

