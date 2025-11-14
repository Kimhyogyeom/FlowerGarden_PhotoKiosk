using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StepCountdownUI : MonoBehaviour
{
    [Header("Setting Component")]
    [SerializeField] private FilmingEndCtrl _filmingEndCtrl;
    [SerializeField] private PrintController _printController;
    [SerializeField] private SwingRotateHandle _swingRotateHandle;

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI _stepText;

    //[SerializeField] private TextMeshProUGUI _countdownText;
    [SerializeField] private GameObject _countdownImagesPool;
    [SerializeField] private Image[] _countdownImages;

    [Header("Settings")]
    [SerializeField] private int _totalSteps = 4;
    [SerializeField] private int _intervalSeconds = 3;

    [Header("Step Visuals")]
    [SerializeField] private Image[] _stepImages = new Image[4];

    [Header("Captured Output Slots")]
    [SerializeField] private Image[] _captureSlots = new Image[4];

    [Header("On Complete")]
    [SerializeField] private GameObject _messageObject;
    [SerializeField] private GameObject _lookCameraMessage;

    [Header("Capture Target")]
    [Tooltip("캡처 기준이 되는 RawImage (이 RectTransform 영역을 캡처)")]
    [SerializeField] private RawImage _targetRawImage;

    [Tooltip("최종 인쇄에 사용할 RawImage (옵션). 비우면 인쇄 스킵.")]
    [SerializeField] private RawImage _photoImageForPrint;

    [Header("Progress Slider (Optional)")]
    [Tooltip("각 촬영 단계 진행도를 표시할 슬라이더 (0~1). 비우면 무시.")]
    [SerializeField] private Slider _stepProgressSlider;
    //[SerializeField] private Image[] _stepProgressImages;
    private Coroutine _routine;
    private bool _isRunning;

    [Space(10)]
    [Header("Timing")]
    [SerializeField] private float _delayAfterShot = 2f;

    [SerializeField] private Animator _filmingAnimator;

    [Header("Capture Ignore Objects")]
    [Tooltip("캡처에는 안 찍히게 하고 싶은 UI 오브젝트들")]
    [SerializeField] private Image[] _ignoreForCapture;

    [Header("Setting Object")]
    [SerializeField] private GameObject[] _photoNumberObjs;

    [Header("Mission")]
    [SerializeField] private MissionApplicationCtrl _missionCtrl;
    [SerializeField] private TextMeshProUGUI _missionText;

    public int _missionCount = 0;

    // ================== Public API ==================

    /// <summary>촬영 시퀀스 시작 (중복 호출 방지)</summary>
    public void StartSequence()
    {
        if (_isRunning)
            return;

        // 혹시 남아있을 이전 데이터 정리 (파일은 남기고 UI만 초기화)
        ResetSequence(false);

        _routine = StartCoroutine(RunSequence());
    }

    /// <summary>
    /// 외부에서 호출하는 리셋 함수.
    /// - 코루틴 중지
    /// - 텍스트/메시지 숨김
    /// - 단계 인디케이터 + 캡처 슬롯 정리
    /// - (옵션) photo_raw_*.jpg 삭제
    /// </summary>
    public void ResetSequence(bool deleteSavedPhotos = true)
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        _isRunning = false;

        // 텍스트 초기화
        if (_stepText)
        {
            _stepText.text = string.Empty;
            _stepText.gameObject.SetActive(false);
        }

        if (_countdownImagesPool)
        {
            _countdownImagesPool.SetActive(false);
            _countdownImages[2].gameObject.SetActive(false);
            _countdownImages[1].gameObject.SetActive(false);
            _countdownImages[0].gameObject.SetActive(false);
        }
        //if (_countdownText)
        //{
        //    _countdownText.text = string.Empty;
        //    _countdownText.gameObject.SetActive(false);
        //}

        // 메시지 오브젝트 숨김
        if (_messageObject) _messageObject.SetActive(false);
        if (_lookCameraMessage) _lookCameraMessage.SetActive(false);

        // 스텝/슬롯 초기화
        SetAllStepImageAlpha(0f);
        ClearCapturedSlots();

        // 슬라이더 초기화
        if (_stepProgressSlider)
        {
            _stepProgressSlider.minValue = 0f;
            _stepProgressSlider.maxValue = 1f;
            _stepProgressSlider.value = 0f;
        }

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

    private IEnumerator RunSequence()
    {
        if (!_targetRawImage)
        {
            Debug.LogError("[StepCountdown] _targetRawImage not assigned");
            yield break;
        }

        if (_stepText) _stepText.gameObject.SetActive(true);
        else Debug.LogWarning("_stepText reference is missing");

        if (_countdownImagesPool) _countdownImagesPool.SetActive(true);
        else Debug.LogWarning("_countdownImagesPool reference is missing");
        //if (_countdownText) _countdownText.gameObject.SetActive(true);
        //else Debug.LogWarning("_countdownText reference is missing");        

        _isRunning = true;

        int steps = Mathf.Max(1, _totalSteps);
        float secs = Mathf.Max(1, _intervalSeconds);

        // 슬라이더 초기 보정
        if (_stepProgressSlider)
        {
            _stepProgressSlider.minValue = 0f;
            _stepProgressSlider.maxValue = 1f;
            _stepProgressSlider.value = 0f;
        }

        if (_lookCameraMessage)
        {
            _lookCameraMessage.SetActive(true);
            yield return new WaitForSeconds(3f);
            _lookCameraMessage.SetActive(false);
        }

        for (int step = 1; step <= steps; step++)
        {
            // 상단 텍스트: 현재 컷 표시
            if (_stepText)
                _stepText.text = $"<color=#FF0000>{step}</color> / {steps}";

            ++_missionCount;
            if (_missionCount == 1)
            {
                string msg = _missionCtrl.GetRandomMissionMessage(_missionCount - 1);
                _missionText.text = msg;
            }
            else if (_missionCount == 2)
            {
                string msg = _missionCtrl.GetRandomMissionMessage(_missionCount - 1);
                _missionText.text = msg;
            }
            else if (_missionCount == 3)
            {
                string msg = _missionCtrl.GetRandomMissionMessage(_missionCount - 1);
                _missionText.text = msg;
            }
            else if (_missionCount == 4)
            {
                string msg = _missionCtrl.GetRandomMissionMessage(_missionCount - 1);
                _missionText.text = msg;
            }

            //Debug.Log($"====================={_missionCount}");

            UpdateStepImageAlpha(step - 1);

            // 슬라이더: 이번 단계에서 채워야 할 구간
            float startFill = (step - 1) / (float)steps;
            float endFill = step / (float)steps;

            // 카운트다운 + 슬라이더 애니메이션 동시 진행
            float elapsed = 0f;
            while (elapsed < secs)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / secs);

                // 슬라이더 0.25씩 부드럽게 증가
                if (_stepProgressSlider)
                {
                    _stepProgressSlider.value = Mathf.Lerp(startFill, endFill, t);
                }

                // 카운트다운 텍스트 (3,2,1, 유지)
                if (_countdownImagesPool)
                {
                    float timeLeft = Mathf.Max(0f, secs - elapsed);
                    int display = Mathf.CeilToInt(timeLeft);
                    if (display < 1) display = 1; // 0 대신 1까지만 보이게

                    if (display == 3)
                    {
                        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._numberSfx3);
                        _countdownImages[2].gameObject.SetActive(true);
                        _countdownImages[1].gameObject.SetActive(false);
                        _countdownImages[0].gameObject.SetActive(false);
                    }
                    else if (display == 2)
                    {
                        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._numberSfx2);
                        _countdownImages[2].gameObject.SetActive(false);
                        _countdownImages[1].gameObject.SetActive(true);
                        _countdownImages[0].gameObject.SetActive(false);
                    }
                    else if (display == 1)
                    {
                        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._numberSfx1);
                        _countdownImages[2].gameObject.SetActive(false);
                        _countdownImages[1].gameObject.SetActive(false);
                        _countdownImages[0].gameObject.SetActive(true);
                    }
                }
                //// 카운트다운 텍스트 (3,2,1 유지)
                //if (_countdownText)
                //{
                //    float timeLeft = Mathf.Max(0f, secs - elapsed);
                //    int display = Mathf.CeilToInt(timeLeft);
                //    if (display < 1) display = 1; // 0 대신 1까지만 보이게
                //    _countdownText.text = display.ToString();

                //    if (display == 3)
                //    {
                //        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._numberSfx3);
                //    }
                //    else if (display == 2)
                //    {
                //        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._numberSfx2);
                //    }
                //    else if (display == 1)
                //    {
                //        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._numberSfx1);
                //    }
                //}

                yield return null;
            }

            // 안전하게 최종 값 스냅
            if (_stepProgressSlider)
            {
                _stepProgressSlider.value = endFill;
                //_stepProgressImages[step - 1].color = Color.white;
            }

            // 여기서 "찰칵!"
            yield return StartCoroutine(CaptureRawImage(_targetRawImage.rectTransform, step - 1));
            Debug.Log($"[StepCountdown] 찰칵!");
            _photoNumberObjs[step - 1].gameObject.SetActive(false);

            _filmingAnimator.SetTrigger("Filming");
            SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._photoSFX);
            _swingRotateHandle._animIsStopFlag = true;

            if (_delayAfterShot > 0f)
            {
                yield return new WaitForSeconds(_delayAfterShot);
                _swingRotateHandle._animIsStopFlag = false;
            }
        }

        // 완료 UI 처리
        if (_countdownImagesPool)
        {
            _countdownImagesPool.SetActive(false);
            _countdownImages[2].gameObject.SetActive(false);
            _countdownImages[1].gameObject.SetActive(false);
            _countdownImages[0].gameObject.SetActive(false);
        }
        //if (_countdownText)
        //{
        //    _countdownText.text = string.Empty;
        //    _countdownText.gameObject.SetActive(false);
        //}

        if (_stepText)
            _stepText.gameObject.SetActive(false);

        SetAllStepImageAlpha(0f);

        if (_messageObject)
            _messageObject.SetActive(true);

        _isRunning = false;
        _routine = null;

        // 모든 컷 이후 슬라이더를 확실히 1로
        if (_stepProgressSlider)
            _stepProgressSlider.value = 1f;

        // ===== 완료 후 흐름 =====
        if (_printController != null && _photoImageForPrint != null)
        {
            _printController.PrintRawImage(
                _photoImageForPrint,
                OnPrintCompleted,
                _stepText ? _stepText.gameObject : null,
                _countdownImagesPool ? _countdownImagesPool.gameObject : null,
                //_countdownText ? _countdownText.gameObject : null,
                _messageObject
            );
        }
        else
        {
            OnSequenceCompleted();
        }
    }

    /// <summary>인쇄까지 끝난 후 호출되는 콜백</summary>
    private void OnPrintCompleted()
    {
        OnSequenceCompleted();
    }

    /// <summary>시퀀스 + (선택) 인쇄 완료 후 Ready로 복귀</summary>
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

    private IEnumerator CaptureRawImage(RectTransform target, int stepIndex)
    {
        foreach (var item in _ignoreForCapture)
        {
            item.color = Color.clear;
        }
        // 카운트다운 텍스트는 캡처에 안 보이게 잠깐 숨김
        if (_stepText) _stepText.gameObject.SetActive(false);
        if (_countdownImagesPool)
        {
            _countdownImagesPool.SetActive(false);
            _countdownImages[2].gameObject.SetActive(false);
            _countdownImages[1].gameObject.SetActive(false);
            _countdownImages[0].gameObject.SetActive(false);
        }
        //if (_countdownText) _countdownText.gameObject.SetActive(false);

        yield return new WaitForEndOfFrame();

        var canvas = target.GetComponentInParent<Canvas>();
        Camera cam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;

        Vector3[] wc = new Vector3[4];
        target.GetWorldCorners(wc);
        Vector2 s0 = RectTransformUtility.WorldToScreenPoint(cam, wc[0]);
        Vector2 s2 = RectTransformUtility.WorldToScreenPoint(cam, wc[2]);

        float x = Mathf.Min(s0.x, s2.x);
        float y = Mathf.Min(s0.y, s2.y);
        float w = Mathf.Abs(s2.x - s0.x);
        float h = Mathf.Abs(s2.y - s0.y);

        x = Mathf.Clamp(x, 0, Screen.width);
        y = Mathf.Clamp(y, 0, Screen.height);
        w = Mathf.Clamp(w, 0, Screen.width - x);
        h = Mathf.Clamp(h, 0, Screen.height - y);

        int iw = Mathf.Max(1, Mathf.RoundToInt(w));
        int ih = Mathf.Max(1, Mathf.RoundToInt(h));
        int ix = Mathf.RoundToInt(x);
        int iy = Mathf.RoundToInt(y);

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

            if (img.sprite != null)
            {
                var oldTex = img.sprite.texture;
                Destroy(img.sprite);
                if (oldTex != null) Destroy(oldTex);
            }

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
            Destroy(tex);
        }

        // 텍스트 다시 노출 (다음 카운트다운용)
        if (_stepText) _stepText.gameObject.SetActive(true);
        if (_countdownImagesPool) _countdownImagesPool.SetActive(true);

        foreach (var item in _ignoreForCapture)
        {
            item.color = Color.white;
        }
        //if (_countdownText) _countdownText.gameObject.SetActive(true);
    }

    // ================== Util ==================

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
