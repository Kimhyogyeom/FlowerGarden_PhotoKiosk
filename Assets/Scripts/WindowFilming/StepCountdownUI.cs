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
    [SerializeField] private PrintController _printController;
    [SerializeField] private SwingRotateHandle _swingRotateHandle;

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI _stepText;
    [SerializeField] private GameObject _countdownTextParent;
    [SerializeField] private TextMeshProUGUI _countdownText;

    [Header("Settings")]
    [SerializeField] private int _totalSteps = 4;
    [SerializeField] private int _countdownSeconds = 5;

    [Header("Step Visuals")]
    [SerializeField] private Image[] _stepImages = new Image[4];

    [Header("Captured Output Slots (Optional Preview)")]
    [SerializeField] private Image[] _captureSlots = new Image[4];
    // 이건 UI 미리보기용. 실제 데이터는 _capturedSprites에 저장.

    [Header("On Complete")]
    [SerializeField] private GameObject _messageObject;

    [Header("Capture Target")]
    [Tooltip("캡처 기준이 되는 RawImage (이 RectTransform 영역을 캡처)")]
    [SerializeField] private RawImage _targetRawImage;

    [Tooltip("최종 인쇄에 사용할 RawImage (옵션). 비우면 인쇄 스킵.")]
    [SerializeField] private RawImage _photoImageForPrint;

    [Header("Progress Slider (Optional)")]
    [Tooltip("각 촬영 단계 진행도를 표시할 슬라이더 (0~1). 비우면 무시.")]
    [SerializeField] private Slider _stepProgressSlider;

    private Coroutine _routine;
    private bool _isRunning;

    [Space(10)]
    [Header("Timing")]
    [SerializeField] private float _delayAfterShot = 2f;
    [SerializeField] private Animator _filmingAnimator;

    [Header("Capture Ignore Objects")]
    [Tooltip("캡처에는 안 찍히게 하고 싶은 UI 오브젝트들")]
    [SerializeField] private Image[] _ignoreForCapture;

    [Header("Mission")]
    [SerializeField] private MissionApplicationCtrl _missionCtrl;
    [SerializeField] private MissionTextAnimatorSlide _missionAnimator;
    [SerializeField] private TextMeshProUGUI _missionText;

    public int _missionCount = 0;

    // === 내부에 실제 캡처 이미지를 들고 있을 배열 ===
    private Sprite[] _capturedSprites;

    private void Awake()
    {
        EnsureCapturedArray();
    }

    private void EnsureCapturedArray()
    {
        int steps = Mathf.Max(1, _totalSteps);

        // 이미 있고 길이가 같으면 패스
        if (_capturedSprites != null && _capturedSprites.Length == steps)
            return;

        // 기존 것 정리
        if (_capturedSprites != null)
        {
            for (int i = 0; i < _capturedSprites.Length; i++)
            {
                if (_capturedSprites[i] != null)
                {
                    var tex = _capturedSprites[i].texture;
                    Destroy(_capturedSprites[i]);
                    if (tex != null) Destroy(tex);
                }
            }
        }

        _capturedSprites = new Sprite[steps];
    }

    // ================== Public API ==================

    public void StartSequence()
    {
        if (_isRunning)
            return;

        ResetSequence(false);
        EnsureCapturedArray();

        _routine = StartCoroutine(RunSequence());
    }

    public void ResetSequence(bool deleteSavedPhotos = true)
    {
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
        }
        if (_countdownTextParent)
        {
            _countdownTextParent.gameObject.SetActive(false);
        }

        // 슬라이더 초기화
        if (_stepProgressSlider)
        {
            _stepProgressSlider.minValue = 0f;
            _stepProgressSlider.maxValue = 1f;
            _stepProgressSlider.value = 0f;
        }

        // 캡처 슬롯/내부 스프라이트 정리
        ClearCapturedSlots();
        ClearCapturedSprites();

        // 스텝 인디케이터 초기화
        SetAllStepImageAlpha(0f);

        // 파일 삭제 (옵션)
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

        EnsureCapturedArray();

        if (_stepText) _stepText.gameObject.SetActive(true);
        else Debug.LogWarning("_stepText reference is missing");

        if (_countdownText) _countdownText.gameObject.SetActive(true);
        else Debug.LogWarning("_countdownText reference is missing");

        if (_countdownTextParent) _countdownTextParent.gameObject.SetActive(true);

        _isRunning = true;

        int steps = Mathf.Max(1, _totalSteps);
        int countdownStart = Mathf.Max(1, _countdownSeconds);

        if (_stepProgressSlider)
        {
            _stepProgressSlider.minValue = 0f;
            _stepProgressSlider.maxValue = 1f;
            _stepProgressSlider.value = 0f;
        }

        for (int step = 1; step <= steps; step++)
        {
            if (_stepText)
                _stepText.text = $"<color=#FF0000>{step}</color> / {steps}";

            // 미션
            ++_missionCount;
            if (_missionCtrl != null)
            {
                int missionIndex = Mathf.Clamp(_missionCount - 1, 0, 3);
                string msg = _missionCtrl.GetRandomMissionMessage(missionIndex);

                if (_missionAnimator != null)
                    _missionAnimator.SetTextAndSlideIn(msg);
                else if (_missionText != null)
                    _missionText.text = msg;
            }

            UpdateStepImageAlpha(step - 1);

            float startFill = (step - 1) / (float)steps;
            float endFill = step / (float)steps;

            // 카운트다운
            int current = countdownStart;
            while (current > 0)
            {
                if (_countdownText)
                    _countdownText.text = current.ToString();

                if (current == 3)
                    SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._numberSfx3);
                else if (current == 2)
                    SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._numberSfx2);
                else if (current == 1)
                    SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._numberSfx1);

                if (_stepProgressSlider)
                {
                    float normalized = (countdownStart - current + 1) / (float)countdownStart;
                    _stepProgressSlider.value = Mathf.Lerp(startFill, endFill, normalized);
                }

                yield return new WaitForSeconds(1f);
                current--;
            }

            if (_countdownText)
                _countdownText.text = string.Empty;

            if (_stepProgressSlider)
                _stepProgressSlider.value = endFill;

            // 실제 캡처
            yield return StartCoroutine(CaptureRawImage(_targetRawImage.rectTransform, step - 1));
            Debug.Log($"[StepCountdown] 찰칵! stepIndex={step - 1}");

            // 촬영 연출
            if (_filmingAnimator != null)
                _filmingAnimator.SetTrigger("Filming");

            SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._photoSFX);

            if (_swingRotateHandle != null)
                _swingRotateHandle._animIsStopFlag = true;

            if (_delayAfterShot > 0f)
            {
                yield return new WaitForSeconds(_delayAfterShot);
                if (_swingRotateHandle != null)
                    _swingRotateHandle._animIsStopFlag = false;
            }
        }

        // 완료 UI 처리
        if (_countdownText)
        {
            _countdownText.text = string.Empty;
            _countdownText.gameObject.SetActive(false);
        }
        if (_countdownTextParent)
            _countdownTextParent.gameObject.SetActive(false);

        if (_stepText)
            _stepText.gameObject.SetActive(false);

        SetAllStepImageAlpha(0f);

        _isRunning = false;
        _routine = null;

        if (_stepProgressSlider)
            _stepProgressSlider.value = 1f;

        // 인쇄까지
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
            OnSequenceCompleted();
        }
    }

    private void OnPrintCompleted()
    {
        OnSequenceCompleted();
    }

    private void OnSequenceCompleted()
    {
        if (_filmingEndCtrl != null)
            _filmingEndCtrl.StartReturn();
        else
            Debug.LogWarning("[StepCountdown] _filmingEndCtrl is not assigned");
    }

    // ================== Capture ==================

    private IEnumerator CaptureRawImage(RectTransform target, int stepIndex)
    {
        EnsureCapturedArray();

        // 캡처 제외 UI 투명
        if (_ignoreForCapture != null)
        {
            foreach (var item in _ignoreForCapture)
                if (item != null) item.color = Color.clear;
        }

        // 텍스트 숨김
        if (_stepText) _stepText.gameObject.SetActive(false);
        if (_countdownText) _countdownText.gameObject.SetActive(false);
        if (_countdownTextParent) _countdownTextParent.gameObject.SetActive(false);

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

        string folderPath = Application.persistentDataPath;
        Directory.CreateDirectory(folderPath);
        string filename = $"photo_raw_{stepIndex + 1}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
        string savePath = Path.Combine(folderPath, filename);
        File.WriteAllBytes(savePath, tex.EncodeToJPG(95));
        Debug.Log($"[찰칵] 저장 완료: {savePath} ({iw}x{ih} from {ix},{iy})");

        // === 내부 배열에 항상 저장 ===
        if (stepIndex >= 0 && stepIndex < _capturedSprites.Length)
        {
            // 기존 것 있으면 정리
            if (_capturedSprites[stepIndex] != null)
            {
                var oldTex = _capturedSprites[stepIndex].texture;
                Destroy(_capturedSprites[stepIndex]);
                if (oldTex != null) Destroy(oldTex);
            }

            var spr = Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);

            spr.name = $"Captured_{stepIndex + 1}_{DateTime.Now:HHmmssfff}";
            _capturedSprites[stepIndex] = spr;

            // 미리보기 슬롯이 있으면 거기에도 표시
            if (_captureSlots != null &&
                stepIndex < _captureSlots.Length &&
                _captureSlots[stepIndex] != null)
            {
                var img = _captureSlots[stepIndex];
                img.sprite = spr;
                img.preserveAspect = true;

                var c = img.color;
                c.a = 1f;
                img.color = c;
            }
        }
        else
        {
            Debug.LogWarning($"[Capture] stepIndex {stepIndex} out of range for _capturedSprites");
            // 이 경우 tex를 더 이상 쓸 곳이 없으니 정리
            Destroy(tex);
        }

        // 숨겨놨던 UI 복원
        if (_stepText) _stepText.gameObject.SetActive(true);
        if (_countdownText) _countdownText.gameObject.SetActive(true);
        if (_countdownTextParent) _countdownTextParent.gameObject.SetActive(true);

        if (_ignoreForCapture != null)
        {
            foreach (var item in _ignoreForCapture)
                if (item != null) item.color = Color.white;
        }
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
                img.sprite = null;
            }

            var c = img.color;
            c.a = 0f;
            img.color = c;
        }
    }

    private void ClearCapturedSprites()
    {
        if (_capturedSprites == null) return;

        for (int i = 0; i < _capturedSprites.Length; i++)
        {
            if (_capturedSprites[i] != null)
            {
                var tex = _capturedSprites[i].texture;
                Destroy(_capturedSprites[i]);
                if (tex != null) Destroy(tex);
            }
            _capturedSprites[i] = null;
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

    // ================== Public Accessors ==================

    public int GetCaptureCount()
    {
        return _capturedSprites != null ? _capturedSprites.Length : 0;
    }

    public Sprite GetCapturedSprite(int index)
    {
        if (_capturedSprites == null) return null;
        if (index < 0 || index >= _capturedSprites.Length) return null;
        return _capturedSprites[index];
    }
}
