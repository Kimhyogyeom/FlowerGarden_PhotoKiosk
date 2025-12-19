// PrintController.cs (Image + RawImage + Bridge 대응, 고화질 PNG + 캡처 임시 스케일 업 + NCP Upload + QR 버전)
// ✅ 포함 기능
// - 캡처 순간에만 RectTransform 임시 확대(x2 등)해서 더 큰 픽셀로 캡처 → 최종 다운샘플 품질 개선
// - 확대 시 화면 밖으로 나가면 자동으로 스케일을 줄여 화면 안에 들어오게(옵션)
// - HiRes(ScreenCapture) 우선, 없으면 ReadPixels 폴백
// - NCP Object Storage 업로드 + 업로드 URL로 QR 생성 후 UI에 표시(옵션)

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class PrintController : MonoBehaviour
{
    [Header("Component Settings")]
    [SerializeField] private FramePanelScaleInCtrl _framePanelScaleInCtrl;

    [Header("Manual / Auto Confirm")]
    [Tooltip("true 면 '사진 인쇄' 창에서 사용자가 직접 [인쇄]를 누르게 함 (자동 Enter 안 보냄)")]
    [SerializeField] private bool _useManualPrintDialog = false;

    [Header("Bridge Settings")]
    [SerializeField] private bool _usePrinterBridge = true;
    [SerializeField] private string _bridgeExeName = "PhotoPrinterBridge.exe";
    [SerializeField, Min(1)] private int _bridgeCopies = 1;
    [SerializeField] private float _bridgeTimeoutSeconds = 60f;

    [Header("Timing")]
    [SerializeField] private float _captureStartDelay = 1f;

    [Header("Component")]
    [SerializeField] private OutputSuccessCtrl _outputSuccessCtrl;
    [SerializeField] private GameObject _background1;
    [SerializeField] private GameObject _background2;
    [SerializeField] private GameObject _background3;

    [Header("Print Settings")]
    [Tooltip("같은 이미지를 몇 번 출력할지 (기본 2장)")]
    [SerializeField, Min(1)] public int _printCount = 2;

    [Header("Capture Source")]
    [Tooltip("target에 RawImage가 있을 때 texture를 직접 복사할지 여부 (단, 자식 포함 캡처에서는 사용 불가)")]
    [SerializeField] private bool _captureFromSourceTexture = true;

    [Tooltip("자식 UI까지 포함해 캡처")]
    [SerializeField] private bool _includeChildrenInCapture = true;

    [Header("High-Res Capture (Recommended)")]
    [Tooltip("true면 ScreenCapture.CaptureScreenshot(superSize)로 고해상도 캡처 후 잘라냄 (화질 개선)")]
    [SerializeField] private bool _useHiResScreenshotCapture = true;

    [Tooltip("스크린샷 배율(1=기존 화면해상도). 6 이상 권장 (고화질)")]
    [SerializeField, Range(1, 10)] private int _screenshotSuperSize = 6;

    [Tooltip("스크린샷 파일 생성 대기 타임아웃(초)")]
    [SerializeField] private float _screenshotTimeoutSeconds = 3f;

    [Tooltip("캡처 후 임시 스크린샷 파일 삭제")]
    [SerializeField] private bool _deleteTempScreenshotFile = true;

    [Header("Capture Temp Scale (네가 원하는 기능)")]
    [Tooltip("캡처 직전에만 target을 임시로 확대해서 더 큰 픽셀로 캡처")]
    [SerializeField] private bool _useTempScaleDuringCapture = true;

    [Tooltip("임시 확대 배수 (예: 2 = x2). 고화질 원하면 3 권장")]
    [SerializeField, Range(1f, 4f)] private float _tempCaptureScale = 2f;

    [Tooltip("임시 확대 시 화면 밖으로 나가면 자동으로 스케일을 줄여서 화면에 맞춤")]
    [SerializeField] private bool _autoFitTempScaleToScreen = true;

    [Tooltip("화면에 딱 맞추면 가장자리 살짝 잘릴 수 있어서 여유(0.95~0.99 추천)")]
    [SerializeField, Range(0.8f, 1f)] private float _fitScreenMargin = 0.92f;

    public enum RotationMode { None, ForceCW, ForceCCW }

    [Header("Landscape Rotation (가로 모드 회전 방향)")]
    [Tooltip("가로 모드(KioskMode.Width)일 때 적용할 회전 방향")]
    [SerializeField] private RotationMode _landscapeRotation = RotationMode.ForceCW;

    [Header("Transform")]
    [Tooltip("저장 전에 회전 (세로 모드일 때만 적용됨)")]
    [SerializeField] private RotationMode _rotation = RotationMode.None;

    [Tooltip("저장 전에 좌우(거울) 뒤집기")]
    [SerializeField] private bool _mirrorHorizontally = false;

    public enum ResampleMode { None, Cover, Fit, ExactCrop, Stretch, ScaleUp }

    [Header("Resample Mode")]
    [Tooltip("세로 모드에서 출력 크기에 맞춰 리샘플링 할지 (Stretch 권장: 크롭/여백 없이 강제 리사이즈)")]
    [SerializeField] private ResampleMode _portraitResample = ResampleMode.Stretch;

    [Tooltip("가로 모드에서 출력 크기에 맞춰 리샘플링 할지 (Stretch 권장: 크롭/여백 없이 강제 리사이즈)")]
    [SerializeField] private ResampleMode _landscapeResample = ResampleMode.Stretch;

    [Header("Output Size (Printer Target)")]
    [Tooltip("세로 4x6 기준 해상도. 1800x2400(400DPI급) 권장. 가로모드는 자동으로 스왑.")]
    [SerializeField] private int _outputWidth = 1800;
    [SerializeField] private int _outputHeight = 2400;

    [Header("Windows Print Target (레거시 폴백용)")]
    [Tooltip("printto에 사용할 프린터 이름 (비우면 OS 기본 print 사용)")]
    [SerializeField] private string _printerName = "DS-RX1";

    [Header("Progress UI (Optional)")]
    [SerializeField] private GameObject _progressRoot;
    [SerializeField] private Image _progressFill;

    [Header("Cover 옵션 (Portrait/Landscape 공용)")]
    [SerializeField, Range(1f, 1.1f)] private float _coverBleed = 1.02f;
    [SerializeField, Range(-1f, 1f)] private float _coverBiasX = 0f;
    [SerializeField, Range(-1f, 1f)] private float _coverBiasY = 0.08f;
    [SerializeField, Min(0)] private int _postCropInsetPx = 0;

    [Header("ExactCrop 비율 조정 (잘림 방지)")]
    [Tooltip("세로 모드: 상하가 더 보이게 비율 조정 (0.05 = 약 1cm 더 보임)")]
    [SerializeField, Range(0f, 0.15f)] private float _exactCropPortraitExtra = 0.05f;

    [Tooltip("가로 모드: 좌우가 더 보이게 비율 조정 (0.05 = 약 1cm 더 보임)")]
    [SerializeField, Range(0f, 0.15f)] private float _exactCropLandscapeExtra = 0.05f;

    [Header("Stretch 비율 조정")]
    [Tooltip("세로 모드: 상하를 살짝 더 길게 (0.02 = 2% 더 김)")]
    [SerializeField, Range(0f, 0.1f)] private float _stretchPortraitExtraHeight = 0.02f;

    [Tooltip("가로 모드: 좌우를 살짝 더 길게 (0.02 = 2% 더 김)")]
    [SerializeField, Range(0f, 0.1f)] private float _stretchLandscapeExtraWidth = 0.02f;

    [Header("ScaleUp 비율 조정 (여백 제거용)")]
    [Tooltip("세로 모드: Y축 확대 비율 (0.03 = 3% 확대, 상하 여백 제거)")]
    [SerializeField, Range(0f, 0.15f)] private float _scaleUpPortraitY = 0.03f;

    [Tooltip("가로 모드: X축 확대 비율 (0.03 = 3% 확대, 좌우 여백 제거)")]
    [SerializeField, Range(0f, 0.15f)] private float _scaleUpLandscapeX = 0.03f;

    [Header("Orientation Safety (Optional)")]
    [Tooltip("모드(가로/세로)와 결과 텍스처의 방향이 다르면 자동으로 90도 회전 보정")]
    [SerializeField] private bool _autoFixOrientationByAspect = true;

    [Tooltip("자동 보정 회전 방향")]
    [SerializeField] private RotationMode _autoFixRotationDir = RotationMode.ForceCW;

    // ==========================
    // ✅ NCP Upload + QR
    // ==========================
    [Header("NCP Upload + QR (Optional)")]
    [SerializeField] private bool _enableUploadAndQr = true;
    [SerializeField] private NcpObjectStorageUploader _ncpUploader;

    [SerializeField] private string _ncpObjectKeyPrefix = "photos/kiosk/";
    [SerializeField] private bool _ncpPublicRead = true;

    [SerializeField] private RawImage _qrRawImage;
    [SerializeField] private TextMeshProUGUI _qrUrlText;
    [SerializeField] private GameObject _qrPanel;
    [SerializeField] private int _qrSize = 512;

    public enum QrTiming { AfterSave_BeforePrint, AfterPrintDone }
    [SerializeField] private QrTiming _qrTiming = QrTiming.AfterSave_BeforePrint;

    // ==========================
    // 초기값 백업
    // ==========================
    private bool _initCaptureFromSourceTexture;
    private bool _initIncludeChildrenInCapture;
    private RotationMode _initRotation;
    private bool _initMirrorHorizontally;
    private int _initOutputWidth;
    private int _initOutputHeight;
    private string _initPrinterName;
    private float _initCoverBleed;
    private float _initCoverBiasX;
    private float _initCoverBiasY;
    private int _initPostCropInsetPx;
    private int _initPrintCount;
    private RotationMode _initLandscapeRotation;
    private ResampleMode _initPortraitResample;
    private ResampleMode _initLandscapeResample;

    private bool _initUseHiRes;
    private int _initSuperSize;
    private float _initShotTimeout;
    private bool _initDeleteTemp;
    private bool _initAutoFixAspect;
    private RotationMode _initAutoFixDir;

    private bool _initUseTempScale;
    private float _initTempScale;
    private bool _initAutoFitTempScale;
    private float _initFitMargin;

    [Header("Save Transform")]
    [SerializeField] private bool _rotate180OnSave = true;
    private void Awake()
    {
        _initCaptureFromSourceTexture = _captureFromSourceTexture;
        _initIncludeChildrenInCapture = _includeChildrenInCapture;
        _initRotation = _rotation;
        _initMirrorHorizontally = _mirrorHorizontally;
        _initOutputWidth = _outputWidth;
        _initOutputHeight = _outputHeight;
        _initPrinterName = _printerName;
        _initCoverBleed = _coverBleed;
        _initCoverBiasX = _coverBiasX;
        _initCoverBiasY = _coverBiasY;
        _initPostCropInsetPx = _postCropInsetPx;
        _initPrintCount = _printCount;
        _initLandscapeRotation = _landscapeRotation;
        _initPortraitResample = _portraitResample;
        _initLandscapeResample = _landscapeResample;

        _initUseHiRes = _useHiResScreenshotCapture;
        _initSuperSize = _screenshotSuperSize;
        _initShotTimeout = _screenshotTimeoutSeconds;
        _initDeleteTemp = _deleteTempScreenshotFile;
        _initAutoFixAspect = _autoFixOrientationByAspect;
        _initAutoFixDir = _autoFixRotationDir;

        _initUseTempScale = _useTempScaleDuringCapture;
        _initTempScale = _tempCaptureScale;
        _initAutoFitTempScale = _autoFitTempScaleToScreen;
        _initFitMargin = _fitScreenMargin;

        if (_qrPanel) _qrPanel.SetActive(false);
    }

    public void ResetPrintState(bool deleteSavedPhotos = true)
    {
        StopAllCoroutines();
        StopProgressUI();

        _captureFromSourceTexture = _initCaptureFromSourceTexture;
        _includeChildrenInCapture = _initIncludeChildrenInCapture;
        _rotation = _initRotation;
        _mirrorHorizontally = _initMirrorHorizontally;
        _outputWidth = _initOutputWidth;
        _outputHeight = _initOutputHeight;
        _printerName = _initPrinterName;
        _coverBleed = _initCoverBleed;
        _coverBiasX = _initCoverBiasX;
        _coverBiasY = _initCoverBiasY;
        _postCropInsetPx = _initPostCropInsetPx;
        _printCount = _initPrintCount;
        _landscapeRotation = _initLandscapeRotation;
        _portraitResample = _initPortraitResample;
        _landscapeResample = _initLandscapeResample;

        _useHiResScreenshotCapture = _initUseHiRes;
        _screenshotSuperSize = _initSuperSize;
        _screenshotTimeoutSeconds = _initShotTimeout;
        _deleteTempScreenshotFile = _initDeleteTemp;
        _autoFixOrientationByAspect = _initAutoFixAspect;
        _autoFixRotationDir = _initAutoFixDir;

        _useTempScaleDuringCapture = _initUseTempScale;
        _tempCaptureScale = _initTempScale;
        _autoFitTempScaleToScreen = _initAutoFitTempScale;
        _fitScreenMargin = _initFitMargin;

        if (_qrPanel) _qrPanel.SetActive(false);

        if (deleteSavedPhotos)
        {
            try
            {
                string dir = Application.persistentDataPath;
                if (Directory.Exists(dir))
                {
                    var pngFiles = Directory.GetFiles(dir, "photo_raw_*.png");
                    var jpgFiles = Directory.GetFiles(dir, "photo_raw_*.jpg");
                    var qrPngFiles = Directory.GetFiles(dir, "photo_qr_*.png");

                    foreach (var f in pngFiles) { try { File.Delete(f); } catch { } }
                    foreach (var f in jpgFiles) { try { File.Delete(f); } catch { } }
                    foreach (var f in qrPngFiles) { try { File.Delete(f); } catch { } }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Print] Reset: delete photos error: {e.Message}");
            }
        }

        Debug.Log("[Print] ResetPrintState: 초기화 완료");
    }

    // ===== 외부 API =====

    public void PrintRawImage(Image image, Action onDone, params GameObject[] toHideTemporarily)
    {
        if (!image) { Debug.LogError("[Print] Image is null"); onDone?.Invoke(); return; }
        PrintUIArea(image.rectTransform, onDone, toHideTemporarily);
    }

    public void PrintRawImage(RawImage rawImage, Action onDone, params GameObject[] toHideTemporarily)
    {
        if (!rawImage) { Debug.LogError("[Print] RawImage is null"); onDone?.Invoke(); return; }
        PrintUIArea(rawImage.rectTransform, onDone, toHideTemporarily);
    }

    public void PrintUIArea(RectTransform target, Action onDone, params GameObject[] toHideTemporarily)
    {
        if (!target) { Debug.LogError("[Print] target RectTransform is null"); onDone?.Invoke(); return; }
        StartCoroutine(CaptureAndPrintRoutine(target, onDone, toHideTemporarily));
    }

    // ===== Main Routine =====

    private IEnumerator CaptureAndPrintRoutine(RectTransform target, Action onDone, GameObject[] toHide)
    {
        if (!target) { Debug.LogError("[Print] CaptureAndPrintRoutine: target is null"); onDone?.Invoke(); yield break; }

        if (_captureStartDelay > 0f)
            yield return new WaitForSeconds(_captureStartDelay);

        ToggleObjects(toHide, false);

        Transform oldParent = target.parent;

        Vector3 oldLocalPosition = target.localPosition;
        Vector2 oldAnchoredPosition = target.anchoredPosition;
        Vector3 oldLocalScale = target.localScale;
        Quaternion oldLocalRotation = target.localRotation;

        Vector2 oldPivot = target.pivot;
        Vector2 oldAnchorMin = target.anchorMin;
        Vector2 oldAnchorMax = target.anchorMax;

        Vector2 oldRectSize = target.rect.size;
        Vector2 oldOffsetMin = target.offsetMin;
        Vector2 oldOffsetMax = target.offsetMax;

        Canvas canvas = target.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[Print] Canvas를 찾을 수 없습니다.");
            ToggleObjects(toHide, true);
            onDone?.Invoke();
            yield break;
        }

        // 중앙으로 임시 이동
        target.SetParent(canvas.transform, false);
        target.localRotation = Quaternion.identity;

        target.pivot = new Vector2(0.5f, 0.5f);
        target.anchorMin = new Vector2(0.5f, 0.5f);
        target.anchorMax = new Vector2(0.5f, 0.5f);
        target.anchoredPosition = Vector2.zero;

        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, oldRectSize.x);
        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, oldRectSize.y);

        // ✅ 캡처 직전에만 스케일 업
        float appliedScale = 1f;
        if (_useTempScaleDuringCapture && _tempCaptureScale > 1.001f)
        {
            appliedScale = Mathf.Max(1f, _tempCaptureScale);
            target.localScale = Vector3.one * appliedScale;

            if (_autoFitTempScaleToScreen)
            {
                appliedScale = FitTempScaleToScreen(target, canvas, appliedScale, _fitScreenMargin);
                target.localScale = Vector3.one * appliedScale;
            }

            Debug.Log($"[Print] TempScale applied: x{appliedScale:0.###}");
        }
        else
        {
            target.localScale = Vector3.one;
        }

        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();

        Debug.Log($"[Print] ===== 캡처 시작 =====");
        Debug.Log($"[Print] Target: {target.name}");
        Debug.Log($"[Print] oldRectSize: {oldRectSize}");
        Debug.Log($"[Print] nowRectSize: {target.rect.size}, tempScale=x{appliedScale:0.###}");
        Debug.Log($"[Print] Screen: {Screen.width}x{Screen.height}");

        Texture2D tex = null;

        // (1) RawImage 단독(자식 미포함)일 때만: 소스 텍스처 복사
        if (_captureFromSourceTexture && !_includeChildrenInCapture)
        {
            var raw = target.GetComponent<RawImage>();
            if (raw && raw.texture)
            {
                tex = CopyRawImageAsSeen(raw);
                Debug.Log("[Print] CopyRawImageAsSeen 사용 (RawImage.texture 기반)");
            }
        }

        // (2) 캡처
        if (tex == null)
        {
            if (_useHiResScreenshotCapture && _screenshotSuperSize > 1)
            {
                Texture2D hiResTex = null;
                yield return StartCoroutine(CaptureByHiResScreenshot(target, canvas, t => hiResTex = t));
                tex = hiResTex;
            }

            if (tex == null)
            {
                tex = _includeChildrenInCapture
                    ? CaptureRectTransformAreaIncludingChildren(target)
                    : CaptureRectTransformArea(target);

                if (tex != null)
                    Debug.Log($"[Print] ReadPixels 기반 캡처 완료: {tex.width}x{tex.height}");
            }
        }

        // 원복
        target.SetParent(oldParent, false);

        target.localPosition = oldLocalPosition;
        target.anchoredPosition = oldAnchoredPosition;
        target.localScale = oldLocalScale;
        target.localRotation = oldLocalRotation;

        target.pivot = oldPivot;
        target.anchorMin = oldAnchorMin;
        target.anchorMax = oldAnchorMax;

        target.offsetMin = oldOffsetMin;
        target.offsetMax = oldOffsetMax;

        ToggleObjects(toHide, true);

        if (tex == null)
        {
            Debug.LogError("[Print] Capture failed (null texture)");
            onDone?.Invoke();
            yield break;
        }

        // 2) 미러 / 회전 / 리샘플 처리
        if (_mirrorHorizontally)
            tex = MirrorX(tex);

        bool isLandscapeMode = false;
        if (GameManager.Instance != null)
        {
            isLandscapeMode = (GameManager.Instance.CurrentMode == KioskMode.Width);
            Debug.Log(isLandscapeMode ? "[Print] KioskMode.Width 감지 -> 가로 모드" : "[Print] KioskMode.Height 감지 -> 세로 모드");
        }
        else
        {
            Debug.LogWarning("[Print] GameManager.Instance가 null입니다. 기본 세로 모드로 진행합니다.");
        }

        RotationMode effectiveRotation = isLandscapeMode ? _landscapeRotation : _rotation;

        switch (effectiveRotation)
        {
            case RotationMode.ForceCW: tex = Rotate90CW(tex); Debug.Log("[Print] 시계방향 90도 회전 완료"); break;
            case RotationMode.ForceCCW: tex = Rotate90CCW(tex); Debug.Log("[Print] 반시계방향 90도 회전 완료"); break;
            default: Debug.Log("[Print] 회전 없음"); break;
        }

        if (isLandscapeMode)
        {
            tex = MirrorX(tex);
            Debug.Log("[Print] 가로 모드: 좌우 반전 적용");
        }

        if (_autoFixOrientationByAspect)
        {
            bool wantLandscape = isLandscapeMode;
            bool isTexLandscape = tex.width >= tex.height;

            if (wantLandscape != isTexLandscape)
            {
                Debug.LogWarning($"[Print] 방향 불일치 감지 (wantLandscape={wantLandscape}, tex={tex.width}x{tex.height}) -> 자동 보정 회전");
                tex = (_autoFixRotationDir == RotationMode.ForceCCW) ? Rotate90CCW(tex) : Rotate90CW(tex);
            }
        }

        ResampleMode mode = isLandscapeMode ? _landscapeResample : _portraitResample;
        if (mode != ResampleMode.None)
        {
            int w = Mathf.Max(8, _outputWidth);
            int h = Mathf.Max(8, _outputHeight);

            if (isLandscapeMode)
            {
                int tmp = w; w = h; h = tmp;
            }

            Debug.Log($"[Print] BEFORE RESAMPLE tex={tex.width}x{tex.height}, target={w}x{h}, mode={mode}, isLandscape={isLandscapeMode}");

            Texture2D resampled = null;
            if (mode == ResampleMode.Cover) resampled = MakePortraitCover(tex, w, h, _coverBiasX, _coverBiasY, _postCropInsetPx);
            else if (mode == ResampleMode.Fit) resampled = MakePortraitFit(tex, w, h);
            else if (mode == ResampleMode.ExactCrop)
            {
                float extra = isLandscapeMode ? _exactCropLandscapeExtra : _exactCropPortraitExtra;
                resampled = MakeExactCrop(tex, w, h, extra, isLandscapeMode);
            }
            else if (mode == ResampleMode.Stretch)
            {
                float extraRatio = isLandscapeMode ? _stretchLandscapeExtraWidth : _stretchPortraitExtraHeight;
                resampled = MakeStretch(tex, w, h, extraRatio, isLandscapeMode);
            }
            else if (mode == ResampleMode.ScaleUp)
            {
                float scaleRatio = isLandscapeMode ? _scaleUpLandscapeX : _scaleUpPortraitY;
                resampled = MakeScaleUp(tex, w, h, scaleRatio, isLandscapeMode);
            }

            if (resampled != null)
            {
                Destroy(tex);
                tex = resampled;
                Debug.Log($"[Print] AFTER RESAMPLE tex={tex.width}x{tex.height}");
            }
            else
            {
                Debug.LogWarning("[Print] RESAMPLE 결과가 null이라 원본 tex 유지");
            }
        }

        // 4) 파일 저장
        string folderPath = Application.persistentDataPath;
        Directory.CreateDirectory(folderPath);
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // QR용 이미지 저장 (캡처가 뒤집혀 있으므로 180도+좌우반전으로 정방향 만들기)
        string qrSavePath = null;
        if (_enableUploadAndQr && _ncpUploader != null)
        {
            // QR용: 정방향으로 만들기 위해 180도 회전 + 좌우반전
            Texture2D qrTex = CopyTexture(tex);
            qrTex = Rotate180(qrTex);
            qrTex = MirrorX(qrTex);
            if (isLandscapeMode)
            {
                qrTex = MirrorX(qrTex);
            }

            string qrFilename = $"photo_qr_{timestamp}.png";
            qrSavePath = Path.Combine(folderPath, qrFilename);
            File.WriteAllBytes(qrSavePath, qrTex.EncodeToPNG());
            Destroy(qrTex);
            Debug.Log($"[Print] QR용 저장 완료 (정방향): {qrSavePath}");
        }

        // 프린터용: 180도 회전 + 좌우반전 (프린터가 뒤집어서 출력하므로 미리 뒤집어둠)
        if (_rotate180OnSave)
        {
            tex = Rotate180(tex);
            tex = MirrorX(tex);

            // 가로모드일 때 추가 좌우반전
            if (isLandscapeMode)
            {
                tex = MirrorX(tex);
                Debug.Log("[Print] 180도 회전 + 좌우반전 + 가로모드 추가 반전 적용 완료");
            }
            else
            {
                Debug.Log("[Print] 180도 회전 + 좌우반전 적용 완료 (세로모드)");
            }
        }

        // 프린터용 이미지 저장
        string filename = $"photo_raw_{timestamp}.png";
        string savePath = Path.Combine(folderPath, filename);

        File.WriteAllBytes(savePath, tex.EncodeToPNG());
        Debug.Log($"[Print] 프린터용 저장 완료: {savePath} ({tex.width}x{tex.height})");
        Destroy(tex);

        // ✅ 저장 직후 업로드+QR (옵션) - QR용 파일 사용
        if (_enableUploadAndQr && _ncpUploader != null && _qrTiming == QrTiming.AfterSave_BeforePrint && qrSavePath != null)
        {
            StartCoroutine(UploadAndShowQrRoutine(qrSavePath));
        }

        // 4) 인쇄
        StartProgressUI();

        if (_usePrinterBridge)
        {
            yield return StartCoroutine(PrintViaBridgeAndNotify(savePath, isLandscapeMode));
        }
        else
        {
            int safeCount = Mathf.Max(1, _printCount);
            for (int i = 0; i < safeCount; i++)
            {
                Debug.Log($"[Print] (Legacy) {_printCount}장 중 {i + 1}번째 출력 시작");
                yield return StartCoroutine(PrintAndNotifyLegacy(savePath));
            }
        }

        // ✅ 인쇄 끝난 뒤 업로드+QR (옵션) - QR용 파일 사용
        if (_enableUploadAndQr && _ncpUploader != null && _qrTiming == QrTiming.AfterPrintDone && qrSavePath != null)
        {
            yield return StartCoroutine(UploadAndShowQrRoutine(qrSavePath));
        }

        StopProgressUI();
        onDone?.Invoke();
    }

    // ==========================
    // ✅ Upload + QR routine
    // ==========================
    private IEnumerator UploadAndShowQrRoutine(string localPngPath)
    {
        if (string.IsNullOrWhiteSpace(localPngPath) || !File.Exists(localPngPath))
        {
            Debug.LogWarning("[QR] local png not found: " + localPngPath);
            yield break;
        }

        if (_ncpUploader == null)
        {
            Debug.LogWarning("[QR] uploader is null");
            yield break;
        }

        byte[] pngBytes = null;
        try
        {
            pngBytes = File.ReadAllBytes(localPngPath);
        }
        catch (Exception e)
        {
            Debug.LogError("[QR] file read fail: " + e.Message);
            yield break;
        }

        string fileName = Path.GetFileName(localPngPath);
        string prefix = _ncpObjectKeyPrefix.TrimStart('/').TrimEnd('/');
        string objectKey = (string.IsNullOrEmpty(prefix) ? "" : (prefix + "/")) + fileName;

        string url = null;
        string err = null;

        yield return StartCoroutine(_ncpUploader.UploadPngBytesRoutine(
            pngBytes,
            objectKey,
            onSuccess: u => url = u,
            onError: e => err = e,
            makePublicReadOverride: _ncpPublicRead
        ));

        if (!string.IsNullOrEmpty(err))
        {
            Debug.LogError("[QR] 업로드 실패:\n" + err);
            yield break;
        }

        Debug.Log("[QR] 업로드 성공 URL: " + url);

        // QR 생성 (너 프로젝트에 이미 있는 걸 사용)
        Texture2D qrTex = QrCodeGenerator_ZXing.Generate(url, _qrSize, 1);

        if (_qrRawImage) _qrRawImage.texture = qrTex;
        if (_qrUrlText) _qrUrlText.text = url;
        if (_qrPanel) _qrPanel.SetActive(true);
    }

    /// <summary>
    /// 임시 스케일이 너무 커서 화면 밖으로 나가면, 화면 안에 들어오게 스케일을 자동으로 줄임
    /// </summary>
    private float FitTempScaleToScreen(RectTransform target, Canvas canvas, float desiredScale, float margin01)
    {
        margin01 = Mathf.Clamp(margin01, 0.8f, 1f);

        Rect r = _includeChildrenInCapture
            ? GetScreenRectIncludingChildren(target, canvas)
            : GetScreenRect(target, canvas);

        if (r.width <= 0.01f || r.height <= 0.01f)
            return desiredScale;

        float maxW = Screen.width * margin01;
        float maxH = Screen.height * margin01;

        if (r.width <= maxW && r.height <= maxH)
            return desiredScale;

        float ratioW = maxW / r.width;
        float ratioH = maxH / r.height;
        float mul = Mathf.Min(ratioW, ratioH);

        float fitted = Mathf.Max(1f, desiredScale * mul);
        Debug.LogWarning($"[Print] TempScale too big -> fitted x{fitted:0.###} (from x{desiredScale:0.###}) rect={r.width:0.0}x{r.height:0.0}");
        return fitted;
    }

    // ===== 고해상도 스크린샷 기반 캡처 (콜백 방식) =====
    private IEnumerator CaptureByHiResScreenshot(RectTransform target, Canvas canvas, Action<Texture2D> onDone)
    {
        Rect captureRect = _includeChildrenInCapture
            ? GetScreenRectIncludingChildren(target, canvas)
            : GetScreenRect(target, canvas);

        if (captureRect.width < 2 || captureRect.height < 2)
        {
            Debug.LogWarning($"[Print] HiResCapture: invalid rect {captureRect}");
            onDone?.Invoke(null);
            yield break;
        }

        int superSize = Mathf.Clamp(_screenshotSuperSize, 1, 10);
        string tempPath = Path.Combine(Application.persistentDataPath, $"__tmp_capture_{DateTime.Now:HHmmssfff}.png");

        ScreenCapture.CaptureScreenshot(tempPath, superSize);
        yield return new WaitForEndOfFrame();

        float waited = 0f;
        float timeout = Mathf.Max(0.2f, _screenshotTimeoutSeconds);
        while (!File.Exists(tempPath) && waited < timeout)
        {
            waited += 0.05f;
            yield return new WaitForSeconds(0.05f);
        }

        if (!File.Exists(tempPath))
        {
            Debug.LogWarning("[Print] HiResCapture: screenshot file not found -> fallback to ReadPixels");
            onDone?.Invoke(null);
            yield break;
        }

        Texture2D full = null;
        Texture2D cropped = null;

        try
        {
            byte[] png = File.ReadAllBytes(tempPath);

            full = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!full.LoadImage(png, false))
            {
                Debug.LogWarning("[Print] HiResCapture: LoadImage failed");
                onDone?.Invoke(null);
                yield break;
            }

            int x = Mathf.RoundToInt(captureRect.x * superSize);
            int y = Mathf.RoundToInt(captureRect.y * superSize);
            int w = Mathf.RoundToInt(captureRect.width * superSize);
            int h = Mathf.RoundToInt(captureRect.height * superSize);

            x = Mathf.Clamp(x, 0, full.width - 1);
            y = Mathf.Clamp(y, 0, full.height - 1);
            w = Mathf.Clamp(w, 1, full.width - x);
            h = Mathf.Clamp(h, 1, full.height - y);

            Debug.Log($"[Print] HiResCapture(full={full.width}x{full.height}, superSize={superSize}) crop={w}x{h} at ({x},{y})");

            Color32[] src = full.GetPixels32();
            cropped = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] dst = new Color32[w * h];

            for (int yy = 0; yy < h; yy++)
            {
                int srcRow = (y + yy) * full.width;
                int dstRow = yy * w;
                for (int xx = 0; xx < w; xx++)
                    dst[dstRow + xx] = src[srcRow + (x + xx)];
            }

            cropped.SetPixels32(dst);
            cropped.Apply(false);

            onDone?.Invoke(cropped);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Print] HiResCapture exception: {e.Message}");
            onDone?.Invoke(null);
        }
        finally
        {
            if (full) Destroy(full);
            if (_deleteTempScreenshotFile) { try { File.Delete(tempPath); } catch { } }
        }
    }

    // ===== RawImage를 '화면처럼'(uvRect 반영) 복사 =====
    private Texture2D CopyRawImageAsSeen(RawImage ri)
    {
        var src = ri.texture;
        int w = src.width;
        int h = src.height;

        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, w, 0, h);
        GL.Clear(true, true, Color.clear);

        Rect uv = ri.uvRect;
        Graphics.DrawTexture(new Rect(0, 0, w, h), src, uv, 0, 0, 0, 0);

        var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        outTex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        outTex.Apply(false);

        GL.PopMatrix();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return outTex;
    }

    // ===== 화면 캡처 (단일 영역) =====
    private Texture2D CaptureRectTransformArea(RectTransform target)
    {
        var canvas = target.GetComponentInParent<Canvas>();
        Camera cam = (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? (canvas.worldCamera ? canvas.worldCamera : Camera.main)
            : null;

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
        w = Mathf.Clamp(w, 1, Screen.width - x);
        h = Mathf.Clamp(h, 1, Screen.height - y);

        int ix = Mathf.RoundToInt(x);
        int iy = Mathf.RoundToInt(y);
        int iw = Mathf.Max(1, Mathf.RoundToInt(w));
        int ih = Mathf.Max(1, Mathf.RoundToInt(h));

        Debug.Log($"[Print] CaptureRectTransformArea: {iw}x{ih} at ({ix},{iy})");

        var tex = new Texture2D(iw, ih, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(ix, iy, iw, ih), 0, 0);
        tex.Apply(false);
        return tex;
    }

    // ===== 화면 캡처 (자식 포함) =====
    private Texture2D CaptureRectTransformAreaIncludingChildren(RectTransform target)
    {
        var canvas = target.GetComponentInParent<Canvas>();
        Camera cam = (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? (canvas.worldCamera ? canvas.worldCamera : Camera.main)
            : null;

        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(target);
        Vector3 worldMin = target.TransformPoint(bounds.min);
        Vector3 worldMax = target.TransformPoint(bounds.max);

        Vector2 s0 = RectTransformUtility.WorldToScreenPoint(cam, worldMin);
        Vector2 s1 = RectTransformUtility.WorldToScreenPoint(cam, new Vector3(worldMin.x, worldMax.y, worldMin.z));
        Vector2 s2 = RectTransformUtility.WorldToScreenPoint(cam, worldMax);
        Vector2 s3 = RectTransformUtility.WorldToScreenPoint(cam, new Vector3(worldMax.x, worldMin.y, worldMin.z));

        float minX = Mathf.Min(s0.x, s1.x, s2.x, s3.x);
        float maxX = Mathf.Max(s0.x, s1.x, s2.x, s3.x);
        float minY = Mathf.Min(s0.y, s1.y, s2.y, s3.y);
        float maxY = Mathf.Max(s0.y, s1.y, s2.y, s3.y);

        float w = maxX - minX;
        float h = maxY - minY;

        minX = Mathf.Clamp(minX, 0, Screen.width);
        minY = Mathf.Clamp(minY, 0, Screen.height);
        w = Mathf.Clamp(w, 1, Screen.width - minX);
        h = Mathf.Clamp(h, 1, Screen.height - minY);

        int ix = Mathf.RoundToInt(minX);
        int iy = Mathf.RoundToInt(minY);
        int iw = Mathf.Max(1, Mathf.RoundToInt(w));
        int ih = Mathf.Max(1, Mathf.RoundToInt(h));

        Debug.Log($"[Print] CaptureRectTransformAreaIncludingChildren: {iw}x{ih} at ({ix},{iy})");

        var tex = new Texture2D(iw, ih, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(ix, iy, iw, ih), 0, 0);
        tex.Apply(false);
        return tex;
    }

    // ===== 화면 좌표 Rect 계산 (HiRes crop용) =====
    private Rect GetScreenRect(RectTransform target, Canvas canvas)
    {
        Camera cam = (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? (canvas.worldCamera ? canvas.worldCamera : Camera.main)
            : null;

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
        w = Mathf.Clamp(w, 1, Screen.width - x);
        h = Mathf.Clamp(h, 1, Screen.height - y);

        return new Rect(x, y, w, h);
    }

    private Rect GetScreenRectIncludingChildren(RectTransform target, Canvas canvas)
    {
        Camera cam = (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? (canvas.worldCamera ? canvas.worldCamera : Camera.main)
            : null;

        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(target);
        Vector3 worldMin = target.TransformPoint(bounds.min);
        Vector3 worldMax = target.TransformPoint(bounds.max);

        Vector2 s0 = RectTransformUtility.WorldToScreenPoint(cam, worldMin);
        Vector2 s1 = RectTransformUtility.WorldToScreenPoint(cam, new Vector3(worldMin.x, worldMax.y, worldMin.z));
        Vector2 s2 = RectTransformUtility.WorldToScreenPoint(cam, worldMax);
        Vector2 s3 = RectTransformUtility.WorldToScreenPoint(cam, new Vector3(worldMax.x, worldMin.y, worldMin.z));

        float minX = Mathf.Min(s0.x, s1.x, s2.x, s3.x);
        float maxX = Mathf.Max(s0.x, s1.x, s2.x, s3.x);
        float minY = Mathf.Min(s0.y, s1.y, s2.y, s3.y);
        float maxY = Mathf.Max(s0.y, s1.y, s2.y, s3.y);

        float w = maxX - minX;
        float h = maxY - minY;

        minX = Mathf.Clamp(minX, 0, Screen.width);
        minY = Mathf.Clamp(minY, 0, Screen.height);
        w = Mathf.Clamp(w, 1, Screen.width - minX);
        h = Mathf.Clamp(h, 1, Screen.height - minY);

        return new Rect(minX, minY, w, h);
    }

    // ===== 미러/회전 =====
    private Texture2D MirrorX(Texture2D src)
    {
        int w = src.width;
        int h = src.height;
        var dst = new Texture2D(w, h, TextureFormat.RGBA32, false);

        var s = src.GetPixels32();
        var d = new Color32[s.Length];

        for (int y = 0; y < h; y++)
        {
            int row = y * w;
            for (int x = 0; x < w; x++)
                d[row + (w - 1 - x)] = s[row + x];
        }

        dst.SetPixels32(d);
        dst.Apply(false);
        Destroy(src);
        return dst;
    }

    private Texture2D Rotate90CW(Texture2D src)
    {
        int w = src.width;
        int h = src.height;
        var dst = new Texture2D(h, w, TextureFormat.RGBA32, false);
        var s = src.GetPixels32();
        var d = new Color32[s.Length];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int srcIndex = y * w + x;
                int dstX = h - 1 - y;
                int dstY = x;
                int dstIndex = dstY * h + dstX;
                d[dstIndex] = s[srcIndex];
            }
        }

        dst.SetPixels32(d);
        dst.Apply(false);
        Destroy(src);
        return dst;
    }

    private Texture2D Rotate90CCW(Texture2D src)
    {
        int w = src.width;
        int h = src.height;
        var dst = new Texture2D(h, w, TextureFormat.RGBA32, false);
        var s = src.GetPixels32();
        var d = new Color32[s.Length];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int srcIndex = y * w + x;
                int dstX = y;
                int dstY = w - 1 - x;
                int dstIndex = dstY * h + dstX;
                d[dstIndex] = s[srcIndex];
            }
        }

        dst.SetPixels32(d);
        dst.Apply(false);
        Destroy(src);
        return dst;
    }

    // ===== Bridge 인쇄 =====
    private IEnumerator PrintViaBridgeAndNotify(string imagePath, bool isLandscapeMode)
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        string bridgeDir = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string bridgePath = Path.Combine(bridgeDir, _bridgeExeName);

        if (!File.Exists(bridgePath))
        {
            Debug.LogWarning($"[Print] Bridge exe not found: {bridgePath} -> legacy print로 폴백");
            yield return StartCoroutine(PrintAndNotifyLegacy(imagePath));
            yield break;
        }

        int unityCopies = Mathf.Max(1, _printCount);
        int bridgeCopies = Mathf.Max(1, _bridgeCopies);
        int totalCopies = unityCopies * bridgeCopies;

        float timeout = Mathf.Max(5f, _bridgeTimeoutSeconds);

        Process proc = null;
        bool started = false;

        string landscapeFlag = isLandscapeMode ? "1" : "0";

        try
        {
            var psi = new ProcessStartInfo(bridgePath)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = $"\"{imagePath}\" {totalCopies} {timeout} \"{_printerName}\" {landscapeFlag}"
            };

            Debug.Log($"[Print] Bridge start: {psi.FileName} {psi.Arguments}");
            proc = Process.Start(psi);
            started = (proc != null);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Print] Bridge 예외 발생(시작 실패): {e.Message}");
            started = false;
        }

        if (!started)
        {
            Debug.LogWarning("[Print] Bridge 프로세스 시작 실패 -> legacy print로 폴백");
            yield return StartCoroutine(PrintAndNotifyLegacy(imagePath));
            yield break;
        }

        float elapsed = 0f;
        while (!proc.HasExited && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!proc.HasExited)
        {
            try { proc.Kill(); } catch { }
            Debug.LogWarning("[Print] Bridge timeout 초과, 프로세스 강제 종료");
        }
        else
        {
            Debug.Log("[Print] Bridge 인쇄 완료 (또는 정상 종료)");
        }

        Debug.Log("출력 완료! (Bridge)");
        if (_outputSuccessCtrl) _outputSuccessCtrl.OutputSuccessObjChange();
#else
        yield return StartCoroutine(PrintAndNotifyLegacy(imagePath));
#endif
    }

    // ===== 레거시 Windows 인쇄 =====
    private IEnumerator PrintAndNotifyLegacy(string imagePath)
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        bool started = false;
        bool needAutoConfirm = false;

        if (!string.IsNullOrWhiteSpace(_printerName))
        {
            try
            {
                var psi = new ProcessStartInfo(imagePath)
                {
                    UseShellExecute = true,
                    Verb = "printto",
                    Arguments = $"\"{_printerName}\"",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Debug.Log($"[Print] printto: {psi.FileName} {psi.Arguments}");
                Process.Start(psi);
                started = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Print] printto failed: {e.Message} -> OS print fallback");
            }
        }

        if (!started)
        {
            try
            {
                var psi = new ProcessStartInfo(imagePath)
                {
                    UseShellExecute = true,
                    Verb = "print",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Debug.Log($"[Print] OS print: {psi.FileName}");
                var proc = Process.Start(psi);
                started = (proc != null);

                if (started && !_useManualPrintDialog)
                    needAutoConfirm = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Print] OS print failed: {e.Message}");
            }
        }

        if (!started)
        {
            Debug.LogWarning("[Print] No print process started");
            yield break;
        }

        if (needAutoConfirm)
        {
            yield return StartCoroutine(AutoConfirmPrintDialog(8f));
        }

        const float totalDuration = 22.0f;
        float t = 0f;
        while (t < totalDuration)
        {
            float normalized = Mathf.Clamp01(t / totalDuration);
            TickProgressUITo(normalized);
            t += Time.deltaTime;
            yield return null;
        }
        TickProgressUITo(1f);

        Debug.Log("출력 완료! (Legacy)");
        if (_outputSuccessCtrl) _outputSuccessCtrl.OutputSuccessObjChange();
#else
        Debug.Log("[Print] Non-Windows: saved only");
        yield return null;
#endif
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const byte VK_RETURN = 0x0D;

    private IEnumerator AutoConfirmPrintDialog(float timeout)
    {
        const string targetTitle = "사진 인쇄";

        float t = 0f;
        while (t < timeout)
        {
            IntPtr hwnd = FindWindow(null, targetTitle);
            if (hwnd != IntPtr.Zero)
            {
                Debug.Log("[Print] \"사진 인쇄\" 창 발견 -> Enter 전송");
                SetForegroundWindow(hwnd);

                keybd_event(VK_RETURN, 0, 0, 0);
                keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, 0);

                yield break;
            }

            t += 0.2f;
            yield return new WaitForSeconds(0.2f);
        }

        Debug.LogWarning("[Print] \"사진 인쇄\" 창을 찾지 못함");
    }
#endif

    // ===== Progress UI =====
    private void StartProgressUI()
    {
        if (_progressRoot) _progressRoot.SetActive(true);
        if (_progressFill) _progressFill.fillAmount = 0f;
    }

    private void TickProgressUITo(float targetCap)
    {
        if (!_progressFill) return;
        targetCap = Mathf.Clamp01(targetCap);
        float cur = _progressFill.fillAmount;
        float next = Mathf.Lerp(cur, targetCap, 1f - Mathf.Exp(-Time.deltaTime * 6f));
        _progressFill.fillAmount = Mathf.Min(next, targetCap);
    }

    private void StopProgressUI()
    {
        if (_progressFill) _progressFill.fillAmount = 1f;
        if (_progressRoot) _progressRoot.SetActive(false);
    }

    // ===== Util / Resample =====
    private void ToggleObjects(GameObject[] objs, bool active)
    {
        if (objs == null) return;
        foreach (var go in objs)
            if (go) go.SetActive(active);
    }

    private Texture2D MakePortraitCover(Texture2D src, int targetW, int targetH,
        float biasX, float biasY, int postCropInsetPx)
    {
        float srcW = src.width;
        float srcH = src.height;

        float scale = Mathf.Max(targetW / srcW, targetH / srcH) * Mathf.Max(1f, _coverBleed);
        float newW = srcW * scale;
        float newH = srcH * scale;

        float offsetX = (targetW - newW) * 0.5f;
        float offsetY = (targetH - newH) * 0.5f;

        float moveRangeX = Mathf.Min(0f, targetW - newW);
        float moveRangeY = Mathf.Min(0f, targetH - newH);

        offsetX += moveRangeX * (biasX * 0.5f + 0.5f) - moveRangeX * 0.5f;
        offsetY += moveRangeY * (biasY * 0.5f + 0.5f) - moveRangeY * 0.5f;

        var rt = RenderTexture.GetTemporary(targetW, targetH, 0,
            RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var prevRT = RenderTexture.active;
        RenderTexture.active = rt;

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, targetW, 0, targetH);
        GL.Clear(true, true, Color.white);

        var prevFilter = src.filterMode;
        src.filterMode = FilterMode.Bilinear;

        Graphics.DrawTexture(new Rect(offsetX, offsetY, newW, newH), src);

        int crop = Mathf.Clamp(postCropInsetPx, 0, Mathf.Min(targetW, targetH) / 3);
        int cropL = crop, cropR = crop, cropT = crop, cropB = crop;

        int outW = targetW - (cropL + cropR);
        int outH = targetH - (cropT + cropB);

        if (outW < 4 || outH < 4)
        {
            cropL = cropR = cropT = cropB = 0;
            outW = targetW;
            outH = targetH;
        }

        var outTex = new Texture2D(outW, outH, TextureFormat.RGBA32, false);
        outTex.ReadPixels(new Rect(cropL, cropB, outW, outH), 0, 0);
        outTex.Apply(false);

        src.filterMode = prevFilter;
        GL.PopMatrix();
        RenderTexture.active = prevRT;
        RenderTexture.ReleaseTemporary(rt);

        return outTex;
    }

    private Texture2D MakePortraitFit(Texture2D src, int targetW, int targetH)
    {
        float srcW = src.width;
        float srcH = src.height;

        float scale = Mathf.Min(targetW / srcW, targetH / srcH);
        float newW = srcW * scale;
        float newH = srcH * scale;

        float offsetX = (targetW - newW) * 0.5f;
        float offsetY = (targetH - newH) * 0.5f;

        var rt = RenderTexture.GetTemporary(targetW, targetH, 0,
            RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var prevRT = RenderTexture.active;
        RenderTexture.active = rt;

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, targetW, 0, targetH);
        GL.Clear(true, true, Color.white);

        var prevFilter = src.filterMode;
        src.filterMode = FilterMode.Bilinear;

        Graphics.DrawTexture(new Rect(offsetX, offsetY, newW, newH), src);

        var outTex = new Texture2D(targetW, targetH, TextureFormat.RGBA32, false);
        outTex.ReadPixels(new Rect(0, 0, targetW, targetH), 0, 0);
        outTex.Apply(false);

        src.filterMode = prevFilter;
        GL.PopMatrix();
        RenderTexture.active = prevRT;
        RenderTexture.ReleaseTemporary(rt);

        return outTex;
    }

    /// <summary>
    /// 4:6 (또는 6:4) 비율에 정확히 맞게 중앙 크롭 후 출력 크기로 리사이즈
    /// extra: 더 보이게 할 비율 (0.05 = 약 1cm 더 보임)
    /// isLandscape: true면 좌우를, false면 상하를 더 보이게
    /// </summary>
    private Texture2D MakeExactCrop(Texture2D src, int targetW, int targetH, float extra, bool isLandscape)
    {
        float srcW = src.width;
        float srcH = src.height;

        // 타겟 비율 (4:6 세로 = 0.666..., 6:4 가로 = 1.5)
        float targetRatio = (float)targetW / targetH;

        // extra 적용: 세로모드면 세로를, 가로모드면 가로를 더 보이게
        // 세로모드: 상하를 더 보이게 → 비율을 더 길게 (targetRatio 감소)
        // 가로모드: 좌우를 더 보이게 → 비율을 더 넓게 (targetRatio 증가)
        if (isLandscape)
            targetRatio *= (1f + extra);  // 가로모드: 좌우를 더 보이게
        else
            targetRatio *= (1f - extra);  // 세로모드: 상하를 더 보이게

        float srcRatio = srcW / srcH;

        int cropW, cropH;
        int cropX, cropY;

        if (srcRatio > targetRatio)
        {
            // 소스가 더 넓음 → 좌우를 크롭
            cropH = (int)srcH;
            cropW = Mathf.RoundToInt(srcH * targetRatio);
            cropX = (int)((srcW - cropW) * 0.5f);
            cropY = 0;
        }
        else
        {
            // 소스가 더 높음 → 상하를 크롭
            cropW = (int)srcW;
            cropH = Mathf.RoundToInt(srcW / targetRatio);
            cropX = 0;
            cropY = (int)((srcH - cropH) * 0.5f);
        }

        // 범위 보정
        cropX = Mathf.Clamp(cropX, 0, (int)srcW - 1);
        cropY = Mathf.Clamp(cropY, 0, (int)srcH - 1);
        cropW = Mathf.Clamp(cropW, 1, (int)srcW - cropX);
        cropH = Mathf.Clamp(cropH, 1, (int)srcH - cropY);

        Debug.Log($"[Print] ExactCrop: src={srcW}x{srcH}, crop={cropW}x{cropH} at ({cropX},{cropY}), target={targetW}x{targetH}");

        // 1) 크롭된 영역 추출
        Color32[] srcPixels = src.GetPixels32();
        Color32[] croppedPixels = new Color32[cropW * cropH];

        for (int y = 0; y < cropH; y++)
        {
            int srcRow = (cropY + y) * (int)srcW;
            int dstRow = y * cropW;
            for (int x = 0; x < cropW; x++)
            {
                croppedPixels[dstRow + x] = srcPixels[srcRow + (cropX + x)];
            }
        }

        var croppedTex = new Texture2D(cropW, cropH, TextureFormat.RGBA32, false);
        croppedTex.SetPixels32(croppedPixels);
        croppedTex.Apply(false);

        // 2) 타겟 크기로 리사이즈 (Bilinear)
        var rt = RenderTexture.GetTemporary(targetW, targetH, 0,
            RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var prevRT = RenderTexture.active;
        RenderTexture.active = rt;

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, targetW, 0, targetH);
        GL.Clear(true, true, Color.white);

        croppedTex.filterMode = FilterMode.Bilinear;
        Graphics.DrawTexture(new Rect(0, 0, targetW, targetH), croppedTex);

        var outTex = new Texture2D(targetW, targetH, TextureFormat.RGBA32, false);
        outTex.ReadPixels(new Rect(0, 0, targetW, targetH), 0, 0);
        outTex.Apply(false);

        GL.PopMatrix();
        RenderTexture.active = prevRT;
        RenderTexture.ReleaseTemporary(rt);

        Destroy(croppedTex);

        return outTex;
    }

    /// <summary>
    /// 크롭/여백 없이 강제로 타겟 크기에 맞춤 (약간 찌그러질 수 있음)
    /// extraRatio: 세로모드면 상하를, 가로모드면 좌우를 더 길게
    /// </summary>
    private Texture2D MakeStretch(Texture2D src, int targetW, int targetH, float extraRatio, bool isLandscape)
    {
        // extra 적용: 출력 크기를 살짝 조정
        int finalW = targetW;
        int finalH = targetH;

        if (isLandscape)
        {
            // 가로모드: 좌우를 더 길게 → width 증가
            finalW = Mathf.RoundToInt(targetW * (1f + extraRatio));
        }
        else
        {
            // 세로모드: 상하를 더 길게 → height 증가
            finalH = Mathf.RoundToInt(targetH * (1f + extraRatio));
        }

        float srcRatio = (float)src.width / src.height;
        float finalRatio = (float)finalW / finalH;
        float stretchPercent = Mathf.Abs(srcRatio - finalRatio) / finalRatio * 100f;

        Debug.Log($"[Print] Stretch: src={src.width}x{src.height} (ratio={srcRatio:0.###}), final={finalW}x{finalH} (ratio={finalRatio:0.###}), stretch={stretchPercent:0.#}%, extra={extraRatio:0.##}");

        var rt = RenderTexture.GetTemporary(finalW, finalH, 0,
            RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var prevRT = RenderTexture.active;
        RenderTexture.active = rt;

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, finalW, 0, finalH);
        GL.Clear(true, true, Color.white);

        src.filterMode = FilterMode.Bilinear;
        Graphics.DrawTexture(new Rect(0, 0, finalW, finalH), src);

        var outTex = new Texture2D(finalW, finalH, TextureFormat.RGBA32, false);
        outTex.ReadPixels(new Rect(0, 0, finalW, finalH), 0, 0);
        outTex.Apply(false);

        GL.PopMatrix();
        RenderTexture.active = prevRT;
        RenderTexture.ReleaseTemporary(rt);

        return outTex;
    }

    /// <summary>
    /// 이미지를 살짝 확대한 후 정확히 타겟 크기로 크롭
    /// 프린터가 센터링할 때 여백이 생기지 않도록 함
    /// scaleRatio: 세로모드면 Y축, 가로모드면 X축 확대 비율 (0.03 = 3%)
    /// </summary>
    private Texture2D MakeScaleUp(Texture2D src, int targetW, int targetH, float scaleRatio, bool isLandscape)
    {
        float srcW = src.width;
        float srcH = src.height;

        // 확대 비율 적용: 세로모드면 높이를 더 크게, 가로모드면 너비를 더 크게
        int scaledW, scaledH;
        if (isLandscape)
        {
            // 가로모드: X축 확대 → 너비를 더 크게 만들어서 좌우 여백 제거
            scaledW = Mathf.RoundToInt(targetW * (1f + scaleRatio));
            scaledH = targetH;
        }
        else
        {
            // 세로모드: Y축 확대 → 높이를 더 크게 만들어서 상하 여백 제거
            scaledW = targetW;
            scaledH = Mathf.RoundToInt(targetH * (1f + scaleRatio));
        }

        Debug.Log($"[Print] ScaleUp: src={srcW}x{srcH}, scaled={scaledW}x{scaledH}, target={targetW}x{targetH}, ratio={scaleRatio:0.##}");

        // 2) 소스를 확대된 크기로 Stretch
        var rt = RenderTexture.GetTemporary(scaledW, scaledH, 0,
            RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var prevRT = RenderTexture.active;
        RenderTexture.active = rt;

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, scaledW, 0, scaledH);
        GL.Clear(true, true, Color.white);

        src.filterMode = FilterMode.Bilinear;
        Graphics.DrawTexture(new Rect(0, 0, scaledW, scaledH), src);

        var scaledTex = new Texture2D(scaledW, scaledH, TextureFormat.RGBA32, false);
        scaledTex.ReadPixels(new Rect(0, 0, scaledW, scaledH), 0, 0);
        scaledTex.Apply(false);

        GL.PopMatrix();
        RenderTexture.active = prevRT;
        RenderTexture.ReleaseTemporary(rt);

        // 3) 중앙 크롭으로 정확히 타겟 크기 추출
        int cropX = (scaledW - targetW) / 2;
        int cropY = (scaledH - targetH) / 2;

        cropX = Mathf.Clamp(cropX, 0, scaledW - targetW);
        cropY = Mathf.Clamp(cropY, 0, scaledH - targetH);

        Color32[] scaledPixels = scaledTex.GetPixels32();
        Color32[] croppedPixels = new Color32[targetW * targetH];

        for (int y = 0; y < targetH; y++)
        {
            int srcRow = (cropY + y) * scaledW;
            int dstRow = y * targetW;
            for (int x = 0; x < targetW; x++)
            {
                croppedPixels[dstRow + x] = scaledPixels[srcRow + (cropX + x)];
            }
        }

        Destroy(scaledTex);

        var outTex = new Texture2D(targetW, targetH, TextureFormat.RGBA32, false);
        outTex.SetPixels32(croppedPixels);
        outTex.Apply(false);

        Debug.Log($"[Print] ScaleUp 완료: 최종={targetW}x{targetH} (crop offset: {cropX},{cropY})");

        return outTex;
    }

    // ===== Test Print =====
    public void PrintTestBlank(Action onDone = null)
    {
        StartCoroutine(PrintTestBlankRoutine(onDone));
    }

    private IEnumerator PrintTestBlankRoutine(Action onDone)
    {
        bool isLandscapeMode = false;
        if (GameManager.Instance != null)
            isLandscapeMode = (GameManager.Instance.CurrentMode == KioskMode.Width);

        int w = Mathf.Max(8, _outputWidth);
        int h = Mathf.Max(8, _outputHeight);

        if (isLandscapeMode)
        {
            int tmp = w;
            w = h;
            h = tmp;
        }

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var fill = new Color32(255, 255, 255, 255);
        var buf = new Color32[w * h];
        for (int i = 0; i < buf.Length; i++) buf[i] = fill;
        tex.SetPixels32(buf);
        tex.Apply(false);

        string folderPath = Application.persistentDataPath;
        Directory.CreateDirectory(folderPath);
        string filename = $"photo_testblank_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        string savePath = Path.Combine(folderPath, filename);
        File.WriteAllBytes(savePath, tex.EncodeToPNG());
        Destroy(tex);
        Debug.Log($"[Print] test blank saved: {savePath} ({w}x{h})");

        if (_enableUploadAndQr && _ncpUploader != null)
        {
            // 테스트 블랭크도 QR 테스트 해보고 싶으면 true
            StartCoroutine(UploadAndShowQrRoutine(savePath));
        }

        StartProgressUI();

        if (_usePrinterBridge)
            yield return StartCoroutine(PrintViaBridgeAndNotify(savePath, isLandscapeMode));
        else
            yield return StartCoroutine(PrintAndNotifyLegacy(savePath));

        StopProgressUI();
        onDone?.Invoke();
    }
    private Texture2D CopyTexture(Texture2D src)
    {
        var copy = new Texture2D(src.width, src.height, src.format, false);
        copy.SetPixels32(src.GetPixels32());
        copy.Apply(false);
        return copy;
    }

    private Texture2D Rotate180(Texture2D src)
    {
        int w = src.width;
        int h = src.height;

        var s = src.GetPixels32();
        var d = new Color32[s.Length];

        // 180도: (x,y) -> (w-1-x, h-1-y)
        for (int y = 0; y < h; y++)
        {
            int srcRow = y * w;
            int dstRow = (h - 1 - y) * w;
            for (int x = 0; x < w; x++)
            {
                d[dstRow + (w - 1 - x)] = s[srcRow + x];
            }
        }

        var dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
        dst.SetPixels32(d);
        dst.Apply(false);

        Destroy(src);   // 원본 해제
        return dst;
    }
}
