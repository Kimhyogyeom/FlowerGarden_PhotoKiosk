using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 촬영 단계(컷) 카운트다운과 캡처, 미션, 인쇄까지 전체 시퀀스를 관리하는 컨트롤러
/// - 각 스텝마다 카운트다운(텍스트: 5,4,3,2,1) 표시
/// - 지정된 영역을 캡처하여 이미지 슬롯에 표시 및 파일 저장
/// - 모든 촬영 후 인쇄(선택) 및 대기화면으로 복귀
/// </summary>
public class StepCountdownUI : MonoBehaviour
{
    [Header("Setting Component")]
    [SerializeField] private FilmingEndCtrl _filmingEndCtrl;
    // 촬영 종료 후 Ready 화면으로 복귀를 담당하는 컨트롤러

    [SerializeField] private PrintController _printController;
    // 최종 촬영본을 인쇄하는 컨트롤러 (옵션)

    [SerializeField] private SwingRotateHandle _swingRotateHandle;
    // 촬영 시 멈췄다가, 촬영 후 다시 회전하게 만들기 위한 핸들

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI _stepText;
    // 현재 촬영 컷 표시 (예: 1 / 4)

    [SerializeField] private GameObject _countdownTextParent;
    [SerializeField] private TextMeshProUGUI _countdownText;
    // 5,4,3,2,1 카운트다운을 보여줄 TMP 텍스트

    [Header("Settings")]
    [SerializeField] private int _totalSteps = 4;
    // 전체 촬영 컷 수

    [SerializeField] private int _countdownSeconds = 5;
    // 각 촬영 전에 카운트다운을 몇 초부터 시작할지 (예: 5 → 5,4,3,2,1)

    [Header("Step Visuals")]
    [SerializeField] private Image[] _stepImages = new Image[4];
    // 상단 등에서 현재 몇 번째 스텝인지 표시하는 인디케이터 이미지

    [Header("Captured Output Slots")]
    [SerializeField] private Image[] _captureSlots = new Image[4];
    // 촬영된 이미지를 미리보기로 보여줄 슬롯들

    [Header("On Complete")]
    [SerializeField] private GameObject _messageObject;
    // 모든 촬영이 끝났을 때 보여줄 완료 메시지 오브젝트

    [Header("Capture Target")]
    [Tooltip("캡처 기준이 되는 RawImage (이 RectTransform 영역을 캡처)")]
    [SerializeField] private RawImage _targetRawImage;
    // 화면 캡처 기준이 되는 RawImage, 여기 영역이 최종 캡처 영역

    [Tooltip("최종 인쇄에 사용할 RawImage (옵션). 비우면 인쇄 스킵.")]
    [SerializeField] private RawImage _photoImageForPrint;
    // 인쇄할 때 사용할 RawImage (레이아웃이 적용된 최종 이미지)

    [Header("Progress Slider (Optional)")]
    [Tooltip("각 촬영 단계 진행도를 표시할 슬라이더 (0~1). 비우면 무시.")]
    [SerializeField] private Slider _stepProgressSlider;
    // 전체 촬영 과정의 진행 상황(0~1)을 보여주는 슬라이더

    private Coroutine _routine;
    // 현재 실행 중인 촬영 시퀀스 코루틴

    private bool _isRunning;
    // 촬영 시퀀스 실행 중 여부 (중복 시작 방지)

    [Space(10)]
    [Header("Timing")]
    [SerializeField] private float _delayAfterShot = 2f;
    // 한 컷 촬영 후 다음 컷으로 넘어가기 전에 기다리는 시간

    [SerializeField] private Animator _filmingAnimator;
    // 셔터 연출 등 촬영 애니메이션용 Animator

    [Header("Capture Ignore Objects")]
    [Tooltip("캡처에는 안 찍히게 하고 싶은 UI 오브젝트들")]
    [SerializeField] private Image[] _ignoreForCapture;
    // 캡처 시 잠깐 투명하게 만들고 다시 복원할 UI 이미지들

    [Header("Setting Object")]
    [SerializeField] private GameObject[] _photoNumberObjs;
    // 각 컷 번호를 표시하는 오브젝트들 (촬영 후 비활성화)

    [Header("Mission")]
    [SerializeField] private MissionApplicationCtrl _missionCtrl;
    [SerializeField] private MissionTextAnimatorSlide _missionAnimator;
    // 미션 문구를 관리하는 컨트롤러

    [SerializeField] private TextMeshProUGUI _missionText;
    // 현재 미션 텍스트를 표시할 TMP

    public int _missionCount = 0;
    // 현재까지 몇 번째 미션을 보여줬는지 카운트

    // ================== Public API ==================

    /// <summary>
    /// 촬영 시퀀스 시작 (중복 호출 방지)
    /// - 이전 진행 중이던 시퀀스 초기화
    /// - RunSequence 코루틴 시작
    /// </summary>
    public void StartSequence()
    {
        if (_isRunning)
            return;

        // 혹시 남아있을 이전 데이터 정리 (파일은 남기고 UI만 초기화)
        ResetSequence(false);

        _routine = StartCoroutine(RunSequence());
    }

    /// <summary>
    /// 외부에서 호출하는 리셋 함수
    /// - 코루틴 중지
    /// - 텍스트/카운트다운/메시지 숨김
    /// - 단계 인디케이터 + 캡처 슬롯 초기화
    /// - (옵션) 저장된 photo_raw_*.jpg 삭제
    /// </summary>
    public void ResetSequence(bool deleteSavedPhotos = true)
    {
        // 진행 중인 시퀀스 코루틴 정지
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        _isRunning = false;

        // 스텝 텍스트 초기화
        if (_stepText)
        {
            _stepText.text = string.Empty;
            _stepText.gameObject.SetActive(false);
        }

        // 카운트다운 텍스트 초기화
        if (_countdownText)
        {
            _countdownText.text = string.Empty;
            _countdownText.gameObject.SetActive(false);
            _countdownTextParent.gameObject.SetActive(false);
        }

        // 메시지 오브젝트 숨김
        if (_messageObject) _messageObject.SetActive(false);

        // 슬라이더 초기화
        if (_stepProgressSlider)
        {
            _stepProgressSlider.minValue = 0f;
            _stepProgressSlider.maxValue = 1f;
            _stepProgressSlider.value = 0f;
        }

        // 캡처 슬롯 정리
        ClearCapturedSlots();

        // 스텝 인디케이터 초기화
        SetAllStepImageAlpha(0f);

        // 저장된 사진 삭제 (선택)
        if (deleteSavedPhotos)
        {
            try
            {
                string dir = Application.persistentDataPath;
                if (Directory.Exists(dir))
                {
                    var files = Directory.GetFiles(dir, "photo_raw_*.jpg");
                    foreach (var f in files)
                    {
                        try { File.Delete(f); }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"[StepReset] delete failed: {f} - {e.Message}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[StepReset] delete photos error: {e.Message}");
            }
        }

        Debug.Log("[StepReset] StepCountdownUI 초기화 완료");
    }

    // ================== Sequence ==================

    /// <summary>
    /// 전체 촬영 시퀀스를 진행하는 코루틴
    /// - 각 단계별 카운트다운, 슬라이더, 미션, 캡처 처리
    /// - 마지막에 인쇄 요청 및 종료 처리
    /// </summary>
    private IEnumerator RunSequence()
    {
        if (!_targetRawImage)
        {
            Debug.LogError("[StepCountdown] _targetRawImage not assigned");
            yield break;
        }

        // 스텝 텍스트, 카운트다운 텍스트 활성화
        if (_stepText) _stepText.gameObject.SetActive(true);
        else Debug.LogWarning("_stepText reference is missing");

        if (_countdownText)
        {
            _countdownText.gameObject.SetActive(true);
            _countdownTextParent.gameObject.SetActive(true);
        }
        else Debug.LogWarning("_countdownText reference is missing");

        _isRunning = true;

        int steps = Mathf.Max(1, _totalSteps);
        int countdownStart = Mathf.Max(1, _countdownSeconds); // 예: 5 → 5,4,3,2,1

        // 슬라이더 초기값 세팅
        if (_stepProgressSlider)
        {
            _stepProgressSlider.minValue = 0f;
            _stepProgressSlider.maxValue = 1f;
            _stepProgressSlider.value = 0f;
        }

        // 각 스텝 반복
        for (int step = 1; step <= steps; step++)
        {
            // 상단 텍스트: 현재 컷 표시 (예: 1 / 4)
            if (_stepText)
                _stepText.text = $"<color=#FF0000>{step}</color> / {steps}";

            // 미션 카운트 증가 및 미션 텍스트 설정
            ++_missionCount;
            {
                int missionIndex = Mathf.Clamp(_missionCount - 1, 0, 3);
                string msg = _missionCtrl.GetRandomMissionMessage(missionIndex);
                _missionAnimator.SetTextAndSlideIn(msg);
                // _missionText.text = msg;  // 애니메이터에서 처리 중
            }

            // 스텝 인디케이터 알파 갱신
            UpdateStepImageAlpha(step - 1);

            // 슬라이더: 이번 스텝에서 채울 구간 계산
            float startFill = (step - 1) / (float)steps;
            float endFill = step / (float)steps;

            // 카운트다운 (텍스트 5 → 4 → 3 → 2 → 1)
            int current = countdownStart;
            while (current > 0)
            {
                if (_countdownText)
                    _countdownText.text = current.ToString();

                // 마지막 3,2,1 구간에서만 음성/효과음 사용 (원래 로직 유지)
                if (current == 3)
                {
                    SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._numberSfx3);
                }
                else if (current == 2)
                {
                    SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._numberSfx2);
                }
                else if (current == 1)
                {
                    SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._numberSfx1);
                }

                // 슬라이더 업데이트 (초 단위로 계단식 증가)
                if (_stepProgressSlider)
                {
                    float normalized = (countdownStart - current + 1) / (float)countdownStart;
                    _stepProgressSlider.value = Mathf.Lerp(startFill, endFill, normalized);
                }

                yield return new WaitForSeconds(1f);
                current--;
            }

            // 안전하게 최종 슬라이더 값 스냅
            if (_stepProgressSlider)
            {
                _stepProgressSlider.value = endFill;
            }

            // 여기서 "찰칵!" 실제 화면 캡처
            yield return StartCoroutine(CaptureRawImage(_targetRawImage.rectTransform, step - 1));
            Debug.Log($"[StepCountdown] 찰칵!");
            _photoNumberObjs[step - 1].gameObject.SetActive(false);

            // 촬영 애니메이션, 사운드, 회전 정지
            _filmingAnimator.SetTrigger("Filming");
            SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._photoSFX);
            _swingRotateHandle._animIsStopFlag = true;

            // 컷 간 대기 시간
            if (_delayAfterShot > 0f)
            {
                yield return new WaitForSeconds(_delayAfterShot);
                _swingRotateHandle._animIsStopFlag = false;
            }
        }

        // 완료 UI 처리
        if (_countdownText)
        {
            _countdownText.text = string.Empty;
            _countdownText.gameObject.SetActive(false);
        }

        if (_stepText)
            _stepText.gameObject.SetActive(false);

        // 스텝 인디케이터 초기화
        SetAllStepImageAlpha(0f);

        // 완료 메시지 노출
        if (_messageObject)
            _messageObject.SetActive(true);

        _isRunning = false;
        _routine = null;

        // 모든 컷 이후 슬라이더를 확실히 1로
        if (_stepProgressSlider)
            _stepProgressSlider.value = 1f;

        // ===== 완료 후 흐름 =====
        // 인쇄 대상과 PrintController가 둘 다 세팅되어 있는 경우에는 인쇄까지 수행
        if (_printController != null && _photoImageForPrint != null)
        {
            _printController.PrintRawImage(
                _photoImageForPrint,
                OnPrintCompleted,
                _stepText ? _stepText.gameObject : null,
                _messageObject
            );
        }
        else
        {
            // 인쇄를 사용하지 않으면 바로 시퀀스 완료 처리
            OnSequenceCompleted();
        }
    }

    /// <summary>
    /// 인쇄까지 끝난 후 호출되는 콜백
    /// </summary>
    private void OnPrintCompleted()
    {
        OnSequenceCompleted();
    }

    /// <summary>
    /// 촬영 시퀀스 + (선택) 인쇄 완료 후 Ready로 복귀
    /// </summary>
    private void OnSequenceCompleted()
    {
        if (_filmingEndCtrl != null)
        {
            _filmingEndCtrl.StartReturn();
        }
        else
        {
            Debug.LogWarning("[StepCountdown] _filmingEndCtrl is not assigned");
        }
    }

    // ================== Capture ==================

    /// <summary>
    /// 지정된 RectTransform 영역을 화면에서 캡처하여
    /// - 파일로 저장(photo_raw_xxx.jpg)
    /// - 슬롯 이미지에 주입
    /// 하는 코루틴
    /// </summary>
    private IEnumerator CaptureRawImage(RectTransform target, int stepIndex)
    {
        // 캡처에 포함시키지 않을 UI들을 투명 처리
        foreach (var item in _ignoreForCapture)
        {
            if (item != null)
                item.color = Color.clear;
        }

        // 텍스트/카운트다운도 캡처에 안 보이도록 잠시 숨김
        if (_stepText) _stepText.gameObject.SetActive(false);
        if (_countdownText)
        {
            _countdownText.gameObject.SetActive(false);
            _countdownTextParent.gameObject.SetActive(false);
        }

        // 렌더링이 모두 끝난 프레임 마지막 시점까지 대기
        yield return new WaitForEndOfFrame();

        var canvas = target.GetComponentInParent<Canvas>();
        Camera cam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;

        // 월드 좌표 → 스크린 좌표 변환
        Vector3[] wc = new Vector3[4];
        target.GetWorldCorners(wc);
        Vector2 s0 = RectTransformUtility.WorldToScreenPoint(cam, wc[0]);
        Vector2 s2 = RectTransformUtility.WorldToScreenPoint(cam, wc[2]);

        float x = Mathf.Min(s0.x, s2.x);
        float y = Mathf.Min(s0.y, s2.y);
        float w = Mathf.Abs(s2.x - s0.x);
        float h = Mathf.Abs(s2.y - s0.y);

        // 화면 영역 안으로 클램프
        x = Mathf.Clamp(x, 0, Screen.width);
        y = Mathf.Clamp(y, 0, Screen.height);
        w = Mathf.Clamp(w, 0, Screen.width - x);
        h = Mathf.Clamp(h, 0, Screen.height - y);

        int iw = Mathf.Max(1, Mathf.RoundToInt(w));
        int ih = Mathf.Max(1, Mathf.RoundToInt(h));
        int ix = Mathf.RoundToInt(x);
        int iy = Mathf.RoundToInt(y);

        // 캡처 텍스처 생성 및 ReadPixels
        var tex = new Texture2D(iw, ih, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(ix, iy, iw, ih), 0, 0);
        tex.Apply();

        // 파일 저장
        string folderPath = Application.persistentDataPath;
        Directory.CreateDirectory(folderPath);
        string filename = $"photo_raw_{stepIndex + 1}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
        string savePath = Path.Combine(folderPath, filename);
        File.WriteAllBytes(savePath, tex.EncodeToJPG(95));
        Debug.Log($"[찰칵] 저장 완료: {savePath} ({iw}x{ih} from {ix},{iy})");

        // 슬롯에 주입
        bool hasSlot = (_captureSlots != null &&
                        stepIndex >= 0 &&
                        stepIndex < _captureSlots.Length &&
                        _captureSlots[stepIndex] != null);

        if (hasSlot)
        {
            var img = _captureSlots[stepIndex];

            // 기존 스프라이트 및 텍스처 정리
            if (img.sprite != null)
            {
                var oldTex = img.sprite.texture;
                Destroy(img.sprite);
                if (oldTex != null) Destroy(oldTex);
            }

            // 새 스프라이트 생성 및 적용
            var spr = Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);

            spr.name = $"Captured_{stepIndex + 1}_{DateTime.Now:HHmmssfff}";
            img.sprite = spr;
            img.preserveAspect = true;

            var c = img.color;
            c.a = 1f;
            img.color = c;
        }
        else
        {
            // 슬롯이 없으면 텍스처는 사용하지 않으므로 정리
            Destroy(tex);
        }

        // 숨겨놨던 텍스트/카운트다운 다시 보여주기
        if (_stepText) _stepText.gameObject.SetActive(true);
        if (_countdownText)
        {
            _countdownText.gameObject.SetActive(true);
            _countdownTextParent.gameObject.SetActive(true);
        }




        // 캡처에서 제외했던 UI 색상 복원
        foreach (var item in _ignoreForCapture)
        {
            if (item != null)
                item.color = Color.white;
        }
    }

    // ================== Util ==================

    /// <summary>
    /// 캡처 슬롯 이미지 초기화
    /// - 기존 스프라이트 및 텍스처 제거
    /// - 알파 0으로 설정
    /// </summary>
    private void ClearCapturedSlots()
    {
        if (_captureSlots == null) return;

        for (int i = 0; i < _captureSlots.Length; i++)
        {
            var img = _captureSlots[i];
            if (!img) continue;

            if (img.sprite != null)
            {
                var tex = img.sprite.texture;
                Destroy(img.sprite);
                if (tex != null) Destroy(tex);
            }

            img.sprite = null;

            var c = img.color;
            c.a = 0f;
            img.color = c;
        }
    }

    /// <summary>
    /// 모든 스텝 인디케이터의 알파를 일괄 설정
    /// </summary>
    private void SetAllStepImageAlpha(float alpha)
    {
        if (_stepImages == null) return;

        foreach (var img in _stepImages)
        {
            if (!img) continue;
            var c = img.color;
            c.a = alpha;
            img.color = c;
        }
    }

    /// <summary>
    /// 현재 활성 스텝 인디케이터만 알파 1, 나머지는 0으로 설정
    /// </summary>
    private void UpdateStepImageAlpha(int activeIndex)
    {
        if (_stepImages == null || _stepImages.Length == 0) return;

        for (int i = 0; i < _stepImages.Length; i++)
        {
            var img = _stepImages[i];
            if (!img) continue;

            var c = img.color;
            c.a = (i == activeIndex) ? 1f : 0f;
            img.color = c;
        }
    }
}
