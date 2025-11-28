// PrintController.cs (Image + RawImage + Bridge 대응, 고화질 PNG 버전)
// - RawImage: texture 복사(uvRect 반영, 페이드 영향 없음)
// - Image(+자식): 화면에서 RectTransform 영역 캡처(자식 포함 가능)
// - 캡처 순간에 toHideTemporarily 에 들어있는 오브젝트만 잠깐 비활성화
// - PNG(무손실) 저장 후
//   1) 가능하면 PhotoPrinterBridge.exe 호출(자동 인쇄, 대화창 없음)
//   2) 실패 시 Windows printto / print 폴백

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class PrintController : MonoBehaviour
{
    [Header("Bridge Settings")]
    [SerializeField] private bool _usePrinterBridge = true;
    [SerializeField] private string _bridgeExeName = "PhotoPrinterBridge.exe";
    [SerializeField, Min(1)] private int _bridgeCopies = 1;      // Bridge에 넘길 기본 장수
    [SerializeField] private float _bridgeTimeoutSeconds = 60f;  // Bridge 프로세스 최대 대기 시간

    [Header("Timing")]
    [SerializeField] private float _captureStartDelay = 1f;   // 기본 1초 딜레이

    [Header("Compoment")]
    [SerializeField] private OutputSuccessCtrl _outputSuccessCtrl;
    [SerializeField] private GameObject _background1;
    [SerializeField] private GameObject _background2;
    [SerializeField] private GameObject _background3;

    [Header("Print Settings")]
    [Tooltip("같은 이미지를 몇 번 출력할지 (기본 2장)")]
    [SerializeField, Min(1)] public int _printCount = 2;

    [Header("Capture Source")]
    [Tooltip("target에 RawImage가 있을 때 texture를 직접 복사할지 여부")]
    [SerializeField] private bool _captureFromSourceTexture = true;
    // true : target에 RawImage가 있으면 raw.texture 복사
    // false: 항상 화면 ReadPixels 방식 사용

    [Tooltip("자식 UI까지 포함해 캡처")]
    [SerializeField] private bool _includeChildrenInCapture = true;

    public enum RotationMode { None, ForceCW, ForceCCW }

    [Header("Transform")]
    [Tooltip("저장 전에 회전 (세로/가로 변환 등)")]
    [SerializeField] private RotationMode _rotation = RotationMode.None;

    [Tooltip("저장 전에 좌우(거울) 뒤집기")]
    [SerializeField] private bool _mirrorHorizontally = false;

    [Header("Portrait Cover (Optional)")]
    [Tooltip("세로 비율로 여백 없이 'Cover' 리샘플")]
    [SerializeField] private bool _forcePortraitCover = false;

    [SerializeField] private int _outputWidth = 1240;
    [SerializeField] private int _outputHeight = 1844;

    [Header("Windows Print Target (레거시 폴백용)")]
    [Tooltip("printto에 사용할 프린터 이름 (비우면 OS 기본 print 사용)")]
    [SerializeField] private string _printerName = "DS-RX1";

    [Header("Progress UI (Optional)")]
    [SerializeField] private GameObject _progressRoot;
    [SerializeField] private Image _progressFill;

    [Header("Cover 옵션")]
    [SerializeField, Range(1f, 1.1f)] private float _coverBleed = 1.02f;
    [SerializeField, Range(-1f, 1f)] private float _coverBiasX = 0f;
    [SerializeField, Range(-1f, 1f)] private float _coverBiasY = 0.08f;
    [SerializeField, Min(0)] private int _postCropInsetPx = 0;

    [Header("Init - 초기화 : 백업 필드")]
    private bool _initCaptureFromSourceTexture;
    private bool _initIncludeChildrenInCapture;
    private RotationMode _initRotation;
    private bool _initMirrorHorizontally;
    private bool _initForcePortraitCover;
    private int _initOutputWidth;
    private int _initOutputHeight;
    private string _initPrinterName;
    private float _initCoverBleed;
    private float _initCoverBiasX;
    private float _initCoverBiasY;
    private int _initPostCropInsetPx;
    private int _initPrintCount;

    private void Awake()
    {
        _initCaptureFromSourceTexture = _captureFromSourceTexture;
        _initIncludeChildrenInCapture = _includeChildrenInCapture;
        _initRotation = _rotation;
        _initMirrorHorizontally = _mirrorHorizontally;
        _initForcePortraitCover = _forcePortraitCover;
        _initOutputWidth = _outputWidth;
        _initOutputHeight = _outputHeight;
        _initPrinterName = _printerName;
        _initCoverBleed = _coverBleed;
        _initCoverBiasX = _coverBiasX;
        _initCoverBiasY = _coverBiasY;
        _initPostCropInsetPx = _postCropInsetPx;
        _initPrintCount = _printCount;
    }

    public void ResetPrintState(bool deleteSavedPhotos = true)
    {
        StopAllCoroutines();
        StopProgressUI();

        _captureFromSourceTexture = _initCaptureFromSourceTexture;
        _includeChildrenInCapture = _initIncludeChildrenInCapture;
        _rotation = _initRotation;
        _mirrorHorizontally = _initMirrorHorizontally;
        _forcePortraitCover = _initForcePortraitCover;
        _outputWidth = _initOutputWidth;
        _outputHeight = _initOutputHeight;
        _printerName = _initPrinterName;
        _coverBleed = _initCoverBleed;
        _coverBiasX = _initCoverBiasX;
        _coverBiasY = _initCoverBiasY;
        _postCropInsetPx = _initPostCropInsetPx;
        _printCount = _initPrintCount;

        if (deleteSavedPhotos)
        {
            try
            {
                string dir = Application.persistentDataPath;
                if (Directory.Exists(dir))
                {
                    var pngFiles = Directory.GetFiles(dir, "photo_raw_*.png");
                    var jpgFiles = Directory.GetFiles(dir, "photo_raw_*.jpg");

                    foreach (var f in pngFiles)
                    {
                        try { File.Delete(f); }
                        catch (Exception e)
                        {
                            UnityEngine.Debug.LogWarning($"[Print] Reset: delete failed for {f}: {e.Message}");
                        }
                    }
                    foreach (var f in jpgFiles)
                    {
                        try { File.Delete(f); }
                        catch (Exception e)
                        {
                            UnityEngine.Debug.LogWarning($"[Print] Reset: delete failed for {f}: {e.Message}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[Print] Reset: delete photos error: {e.Message}");
            }
        }

        UnityEngine.Debug.Log("[Print] ResetPrintState: 초기화 완료");
    }

    // ===== 외부 API =====

    /// <summary>
    /// Image 기준 인쇄 요청 (TargetImage + 자식들 포함)
    /// </summary>
    public void PrintRawImage(Image image, Action onDone, params GameObject[] toHideTemporarily)
    {
        if (!image)
        {
            UnityEngine.Debug.LogError("[Print] Image is null");
            onDone?.Invoke();
            return;
        }

        PrintUIArea(image.rectTransform, onDone, toHideTemporarily);
    }

    /// <summary>
    /// RawImage 버전 (예전 코드 호환용)
    /// </summary>
    public void PrintRawImage(RawImage rawImage, Action onDone, params GameObject[] toHideTemporarily)
    {
        if (!rawImage)
        {
            UnityEngine.Debug.LogError("[Print] RawImage is null");
            onDone?.Invoke();
            return;
        }

        PrintUIArea(rawImage.rectTransform, onDone, toHideTemporarily);
    }

    IEnumerator TestCorutine()
    {
        yield return new WaitForSeconds(2f);
        UnityEngine.Debug.Log("출력 완료!");
        _outputSuccessCtrl.OutputSuccessObjChange();
    }

    // ───────────────────────────────────────

    public void PrintUIArea(RectTransform target, Action onDone, params GameObject[] toHideTemporarily)
    {
        if (!target)
        {
            UnityEngine.Debug.LogError("[Print] target RectTransform is null");
            onDone?.Invoke();
            return;
        }
        StartCoroutine(CaptureAndPrintRoutine(target, onDone, toHideTemporarily));
    }

    // ===== Main =====

    private IEnumerator CaptureAndPrintRoutine(RectTransform target, Action onDone, GameObject[] toHide)
    {
        if (!target)
        {
            UnityEngine.Debug.LogError("[Print] CaptureAndPrintRoutine: target is null");
            onDone?.Invoke();
            yield break;
        }

        // Timer
        if (_captureStartDelay > 0f)
            yield return new WaitForSeconds(_captureStartDelay);

        // 0. 찍히면 안 되는 애들(검은 페이드 패널 등)은 미리 꺼두기
        ToggleObjects(toHide, false);

        // ───────────────── 원래 RectTransform 상태 백업 ─────────────────
        RectTransform rt = target;

        Vector2 oldAnchorMin = rt.anchorMin;
        Vector2 oldAnchorMax = rt.anchorMax;
        Vector2 oldAnchoredPos = rt.anchoredPosition;
        Vector2 oldSizeDelta = rt.sizeDelta;
        Vector2 oldPivot = rt.pivot;
        Vector3 oldScale = rt.localScale;
        int oldSiblingIdx = rt.GetSiblingIndex();

        // 캔버스 찾기 (없으면 그냥 Screen 기준으로 간다고 가정)
        Canvas canvas = rt.GetComponentInParent<Canvas>();

        // ──────────────── 캡처용으로 잠깐 화면 중앙으로 이동 ────────────────
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        // 필요하면 가장 위로 올려서 다른 UI에 가리지 않게
        if (rt.parent != null)
        {
            rt.SetSiblingIndex(rt.parent.childCount - 1);
        }

        // 레이아웃/캔버스 갱신
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();

        // ───────────────── 1) 텍스처 생성 (RawImage 우선 → 화면 캡처 폴백) ─────────────────
        Texture2D tex = null;

        // 1-1) target 밑에 RawImage가 있고, 그 텍스처를 그대로 쓰고 싶다면
        if (_captureFromSourceTexture && !_includeChildrenInCapture)
        {
            var raw = rt.GetComponent<RawImage>();
            if (raw && raw.texture)
            {
                tex = CopyRawImageAsSeen(raw);
                UnityEngine.Debug.Log("[Print] CopyRawImageAsSeen 사용 (RawImage.texture 기반)");
            }
        }

        // 1-2) 그게 아니면 화면에서 RectTransform 영역 캡처 (자식 포함 여부 옵션)
        if (tex == null)
        {
            tex = _includeChildrenInCapture
                ? CaptureRectTransformAreaIncludingChildren(rt)
                : CaptureRectTransformArea(rt);

            if (tex != null)
                UnityEngine.Debug.Log($"[Print] ReadPixels 기반 캡처 완료: {tex.width}x{tex.height}");
        }

        // ──────────────── 캡처 끝났으니, RectTransform 원래대로 복구 ────────────────
        rt.anchorMin = oldAnchorMin;
        rt.anchorMax = oldAnchorMax;
        rt.anchoredPosition = oldAnchoredPos;
        rt.sizeDelta = oldSizeDelta;
        rt.pivot = oldPivot;
        rt.localScale = oldScale;
        rt.SetSiblingIndex(oldSiblingIdx);

        // 찍히면 안 되었던 애들 다시 켜기
        ToggleObjects(toHide, true);

        // 텍스처 실패 시 종료
        if (tex == null)
        {
            UnityEngine.Debug.LogError("[Print] Capture failed (null texture)");
            onDone?.Invoke();
            yield break;
        }

        // ───────────────── 2) 미러 / 회전 / 커버 처리 ─────────────────
        if (_mirrorHorizontally) tex = MirrorX(tex);

        switch (_rotation)
        {
            case RotationMode.ForceCW: tex = Rotate90CW(tex); break;
            case RotationMode.ForceCCW: tex = Rotate90CCW(tex); break;
        }

        if (_forcePortraitCover)
        {
            int w = Mathf.Max(8, _outputWidth);
            int h = Mathf.Max(8, _outputHeight);
            var portrait = MakePortraitCover(tex, w, h, _coverBiasX, _coverBiasY, _postCropInsetPx);
            UnityEngine.Object.Destroy(tex);
            tex = portrait;
        }

        // ───────────────── 3) 파일 저장 (PNG, 무손실) ─────────────────
        string folderPath = Application.persistentDataPath;
        Directory.CreateDirectory(folderPath);
        string filename = $"photo_raw_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        string savePath = Path.Combine(folderPath, filename);

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(savePath, bytes);
        UnityEngine.Debug.Log($"[Print] 저장 완료: {savePath} ({tex.width}x{tex.height})");
        UnityEngine.Object.Destroy(tex);

        // ───────────────── 4) 인쇄 + 진행 UI ─────────────────
        StartProgressUI();

        if (_usePrinterBridge)
        {
            yield return StartCoroutine(PrintViaBridgeAndNotify(savePath));
        }
        else
        {
            int safeCount = Mathf.Max(1, _printCount);
            for (int i = 0; i < safeCount; i++)
            {
                UnityEngine.Debug.Log($"[Print] (Legacy) {_printCount}장 중 {i + 1}번째 출력 시작");
                yield return StartCoroutine(PrintAndNotifyLegacy(savePath));
            }
        }

        StopProgressUI();

        onDone?.Invoke();
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

    // ===== 화면 캡처(단일 영역) =====

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

        UnityEngine.Debug.Log($"[Print] CaptureRectTransformArea: {iw}x{ih} at ({ix},{iy})");

        var tex = new Texture2D(iw, ih, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(ix, iy, iw, ih), 0, 0);
        tex.Apply(false);
        return tex;
    }

    // ===== 화면 캡처(자식 포함) =====

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

        UnityEngine.Debug.Log($"[Print] CaptureRectTransformAreaIncludingChildren: {iw}x{ih} at ({ix},{iy})");

        var tex = new Texture2D(iw, ih, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(ix, iy, iw, ih), 0, 0);
        tex.Apply(false);
        return tex;
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
        UnityEngine.Object.Destroy(src);
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
            int row = y * w;
            for (int x = 0; x < w; x++)
            {
                int si = row + x;
                int dx = h - 1 - y;
                int dy = x;
                d[dy * h + dx] = s[si];
            }
        }

        dst.SetPixels32(d);
        dst.Apply(false);
        UnityEngine.Object.Destroy(src);
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
            int row = y * w;
            for (int x = 0; x < w; x++)
            {
                int si = row + x;
                int dx = y;
                int dy = (w - 1) - x;
                d[dy * h + dx] = s[si];
            }
        }

        dst.SetPixels32(d);
        dst.Apply(false);
        UnityEngine.Object.Destroy(src);
        return dst;
    }

    // ===== Bridge 인쇄 =====
    private IEnumerator PrintViaBridgeAndNotify(string imagePath)
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

        // Unity 빌드 기준: .exe 옆에 Bridge exe 두기
        string bridgeDir = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string bridgePath = Path.Combine(bridgeDir, _bridgeExeName);

        if (!File.Exists(bridgePath))
        {
            UnityEngine.Debug.LogWarning($"[Print] Bridge exe not found: {bridgePath} → legacy print로 폴백");
            yield return StartCoroutine(PrintAndNotifyLegacy(imagePath));
            yield break;
        }

        int unityCopies = Mathf.Max(1, _printCount);
        int bridgeCopies = Mathf.Max(1, _bridgeCopies);
        int totalCopies = unityCopies * bridgeCopies;

        float timeout = Mathf.Max(5f, _bridgeTimeoutSeconds);

        Process proc = null;
        bool started = false;

        // try 안에서는 yield 사용 안 함
        try
        {
            var psi = new ProcessStartInfo(bridgePath)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                // Program.cs에서 args: <imagePath> <copies> <timeoutSeconds> <printerName?> 라고 가정
                Arguments = $"\"{imagePath}\" {totalCopies} {timeout} \"{_printerName}\""
            };

            UnityEngine.Debug.Log($"[Print] Bridge start: {psi.FileName} {psi.Arguments}");
            proc = Process.Start(psi);
            started = (proc != null);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"[Print] Bridge 예외 발생(시작 실패): {e.Message}");
            started = false;
        }

        if (!started)
        {
            UnityEngine.Debug.LogWarning("[Print] Bridge 프로세스 시작 실패 → legacy print로 폴백");
            yield return StartCoroutine(PrintAndNotifyLegacy(imagePath));
            yield break;
        }

        // 여기부터는 try 바깥이니까 yield 사용 가능
        float elapsed = 0f;
        while (!proc.HasExited && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!proc.HasExited)
        {
            try { proc.Kill(); } catch { }
            UnityEngine.Debug.LogWarning("[Print] Bridge timeout 초과, 프로세스 강제 종료");
        }
        else
        {
            UnityEngine.Debug.Log("[Print] Bridge 인쇄 완료 (또는 정상 종료)");
        }

        UnityEngine.Debug.Log("출력 완료! (Bridge)");
        _outputSuccessCtrl.OutputSuccessObjChange();

#else
        // 윈도우가 아니면 그냥 레거시 or 저장만
        yield return StartCoroutine(PrintAndNotifyLegacy(imagePath));
#endif
    }

    // ===== 레거시 Windows 인쇄 (사진 인쇄 창 + AutoConfirm 포함) =====

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
                UnityEngine.Debug.Log($"[Print] printto: {psi.FileName} {psi.Arguments}");
                Process.Start(psi);
                started = true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[Print] printto failed: {e.Message} → OS print fallback");
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
                UnityEngine.Debug.Log($"[Print] OS print: {psi.FileName}");
                var proc = Process.Start(psi);
                started = (proc != null);
                if (started) needAutoConfirm = true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[Print] OS print failed: {e.Message}");
            }
        }

        if (!started)
        {
            UnityEngine.Debug.LogWarning("[Print] No print process started (check printer / associations)");
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

        UnityEngine.Debug.Log("출력 완료! (Legacy)");
        _outputSuccessCtrl.OutputSuccessObjChange();

#else
        UnityEngine.Debug.Log("[Print] Non-Windows: saved only (no auto print)");
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
                UnityEngine.Debug.Log("[Print] AutoConfirmPrintDialog: \"사진 인쇄\" 창 발견 → Enter 전송");
                SetForegroundWindow(hwnd);

                keybd_event(VK_RETURN, 0, 0, 0);
                keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, 0);

                yield break;
            }

            t += 0.2f;
            yield return new WaitForSeconds(0.2f);
        }

        UnityEngine.Debug.LogWarning("[Print] AutoConfirmPrintDialog: \"사진 인쇄\" 창을 찾지 못함 (timeout)");
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

    // ────────────────────────────────── Test Print ──────────────────────────────────

    public void PrintTestBlank(Action onDone = null)
    {
        StartCoroutine(PrintTestBlankRoutine(onDone));
    }

    private IEnumerator PrintTestBlankRoutine(Action onDone)
    {
        int w = Mathf.Max(8, _outputWidth);
        int h = Mathf.Max(8, _outputHeight);

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
        UnityEngine.Object.Destroy(tex);
        UnityEngine.Debug.Log($"[Print] test blank saved: {savePath} ({w}x{h})");

        StartProgressUI();

        if (_usePrinterBridge)
        {
            yield return StartCoroutine(PrintViaBridgeAndNotify(savePath));
        }
        else
        {
            int safeCount = Mathf.Max(1, _printCount);
            for (int i = 0; i < safeCount; i++)
            {
                UnityEngine.Debug.Log($"[Print] 테스트 블랭크 {_printCount}장 중 {i + 1}번째 출력 시작 (Legacy)");
                yield return StartCoroutine(PrintAndNotifyLegacy(savePath));
            }
        }

        StopProgressUI();

        onDone?.Invoke();
    }
}
