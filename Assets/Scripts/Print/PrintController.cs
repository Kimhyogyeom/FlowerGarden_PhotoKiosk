// PrintController.cs (lean version)
// - RawImage 소스 텍스처(uvRect 포함) 우선 복사 → 실패 시 화면 캡처(자식 포함 가능)
// - (옵션) 회전/미러/(옵션) 세로 Cover 리샘플
// - JPG 저장: photo_raw_yyyyMMdd_HHmmss.jpg (persistentDataPath)
// - Windows: 기본 앱 printto → 실패 시 OS print 폴백 (+ "사진 인쇄" 창 자동 Enter)
// - 진행 UI(fill) 애니메이션 + 완료 콜백

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class PrintController : MonoBehaviour
{
    [Header("Compoment")]
    [SerializeField] private OutputSuccessCtrl _outputSuccessCtrl;

    [Header("Capture Source")]
    [Tooltip("RawImage.texture 원본(uvRect 포함)으로 복사 (자식 UI가 필요 없으면 권장)")]
    [SerializeField] private bool _captureFromSourceTexture = true;

    [Tooltip("자식 UI까지 포함해 화면 캡처")]
    [SerializeField] private bool _includeChildrenInCapture = false;

    public enum RotationMode { None, ForceCW, ForceCCW }

    [Header("Transform")]
    [Tooltip("저장 전에 회전 (세로 강제 등 필요 시)")]
    [SerializeField] private RotationMode _rotation = RotationMode.None;

    [Tooltip("저장 전에 좌우(거울) 뒤집기")]
    [SerializeField] private bool _mirrorHorizontally = false;

    [Header("Portrait Cover (Optional)")]
    [Tooltip("세로 비율로 여백 없이 'Cover' 리샘플")]
    [SerializeField] private bool _forcePortraitCover = false;
    [SerializeField] private int _outputWidth = 1240;
    [SerializeField] private int _outputHeight = 1844;

    [Header("Windows Print Target")]
    [Tooltip("printto에 사용할 프린터 이름 (비우면 기본 프린터로 OS print 사용)")]
    [SerializeField] private string _printerName = "DS-RX1";

    [Header("Progress UI (Optional)")]
    [SerializeField] private GameObject _progressRoot;
    [SerializeField] private Image _progressFill;

    //[Header("Progress Timing")]
    //[Tooltip("스풀 전송 중 채울 최대치 (0~1 사이, 출력 전반부 느낌)")]
    ////[SerializeField, Range(0.1f, 0.99f)] private float _preSpoolMaxFill = 0.4f;

    //[Tooltip("인쇄 명령(사진 인쇄 창 확인 포함) 후, 실제 용지 나올 때까지 예상 시간(초)")]
    //[SerializeField] private float _postSpoolSeconds = 10.0f;

    [Header("Cover 옵션")]
    [SerializeField, Range(1f, 1.1f)] private float _coverBleed = 1.02f;
    [SerializeField, Range(-1f, 1f)] private float _coverBiasX = 0f;
    [SerializeField, Range(-1f, 1f)] private float _coverBiasY = 0.08f;
    [SerializeField, Min(0)] private int _postCropInsetPx = 0;


    [Header("Init - 초기화 : 백업 필드 추가")]
    // ===== Init State Backup =====
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
    }

    /// <summary>
    /// 출력 관련 상태/옵션/임시 파일을 초기화.
    /// 기본 화면 전환(패널 토글, 씬 전환 등)은 이 함수 호출 후 밖에서 처리.
    /// </summary>
    public void ResetPrintState(bool deleteSavedPhotos = true)
    {
        // 1) 코루틴 / 진행 UI 정리
        StopAllCoroutines();
        StopProgressUI();

        // 2) 인스펙터에서 바뀌었을 수 있는 런타임 옵션을 초기값으로 복구
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

        // 3) 찍어둔 사진 파일 삭제 (선택)
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
    /// PrintButtonHandler 에서 호출함
    /// </summary>
    /// <param name="rawImage"></param>
    /// <param name="onDone"></param>
    /// <param name="toHideTemporarily"></param>
    public void PrintRawImage(RawImage rawImage, Action onDone, params GameObject[] toHideTemporarily)
    {
        // 카메라가 없어서 임시로 만든 로직
        StartCoroutine(TestCorutine());

        // 실제 환경에선 이걸 써야함
        //if (!rawImage)
        //{
        //    UnityEngine.Debug.LogError("[Print] RawImage is null");
        //    onDone?.Invoke();
        //    return;
        //}
        //PrintUIArea(rawImage.rectTransform, onDone, toHideTemporarily);
    }

    /// <summary>
    /// 카메라가 없어서 임시로 만든 코루틴임
    /// </summary>
    /// <returns></returns>
    IEnumerator TestCorutine()
    {
        yield return new WaitForSeconds(2f); // 대기 시간
        UnityEngine.Debug.Log("출력 완료!");
        _outputSuccessCtrl.OutputSuccessObjChange();        
    }

    // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

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
        ToggleObjects(toHide, false);
        yield return new WaitForEndOfFrame();

        // 1) 소스 텍스처 우선
        Texture2D tex = null;
        if (_captureFromSourceTexture && !_includeChildrenInCapture)
        {
            var raw = target.GetComponent<RawImage>();
            if (raw && raw.texture)
                tex = CopyRawImageAsSeen(raw); // uvRect 반영
        }

        // 2) 폴백: 화면 캡처
        if (tex == null)
        {
            tex = _includeChildrenInCapture
                ? CaptureRectTransformAreaIncludingChildren(target)
                : CaptureRectTransformArea(target);
        }

        if (tex == null)
        {
            UnityEngine.Debug.LogError("[Print] Capture failed (null texture)");
            ToggleObjects(toHide, true);
            onDone?.Invoke();
            yield break;
        }

        // 3) 미러/회전/커버
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

        // 4) 저장
        string folderPath = Application.persistentDataPath;
        Directory.CreateDirectory(folderPath);
        string filename = $"photo_raw_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
        string savePath = Path.Combine(folderPath, filename);

        File.WriteAllBytes(savePath, tex.EncodeToJPG(95)); // 필요하면 PNG로 바꿔도 됨
        UnityEngine.Debug.Log($"[Print] 저장 완료: {savePath} ({tex.width}x{tex.height})");
        UnityEngine.Object.Destroy(tex);

        ToggleObjects(toHide, true);

        // 5) 인쇄 + 진행 UI
        StartProgressUI();
        yield return StartCoroutine(PrintAndNotify(savePath));
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
        w = Mathf.Clamp(w, 0, Screen.width - x);
        h = Mathf.Clamp(h, 0, Screen.height - y);

        int ix = Mathf.RoundToInt(x);
        int iy = Mathf.RoundToInt(y);
        int iw = Mathf.Max(1, Mathf.RoundToInt(w));
        int ih = Mathf.Max(1, Mathf.RoundToInt(h));

        var tex = new Texture2D(iw, ih, TextureFormat.RGB24, false);
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

        int ix = Mathf.RoundToInt(Mathf.Clamp(minX, 0, Screen.width));
        int iy = Mathf.RoundToInt(Mathf.Clamp(minY, 0, Screen.height));
        int iw = Mathf.Max(1, Mathf.RoundToInt(Mathf.Clamp(maxX - minX, 0, Screen.width - ix)));
        int ih = Mathf.Max(1, Mathf.RoundToInt(Mathf.Clamp(maxY - minY, 0, Screen.height - iy)));

        var tex = new Texture2D(iw, ih, TextureFormat.RGB24, false);
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

    // ===== 인쇄 =====
    private IEnumerator PrintAndNotify(string imagePath)
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        bool started = false;
        bool needAutoConfirm = false;

        // 1) printto 우선 시도 (에러는 허용, 실패하면 OS print로)
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

        // 2) 실패 시 OS print (사진 인쇄 창 뜨는 루트)
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

                // 여기서는 그냥 "자동 확인 해보자" 플래그만 세팅
                if (started)
                {
                    needAutoConfirm = true;
                }
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

        // 3) 인쇄창 자동 Enter (※ try/catch 밖, yield 허용 영역)
        if (needAutoConfirm)
        {
            // 실패해도 그냥 넘어감 (로그만 찍히고 인쇄는 계속)
            yield return StartCoroutine(AutoConfirmPrintDialog(8f));
        }

        // 4) ProgressBar
        const float totalDuration = 22.0f;
        float t = 0f;

        // StartProgressUI()에서 이미 0으로 초기화됐다고 가정
        while (t < totalDuration)
        {
            float normalized = Mathf.Clamp01(t / totalDuration); // 0 ~ 1
            TickProgressUITo(normalized);
            t += Time.deltaTime;
            yield return null;
        }

        // 혹시 덜 찼으면 확실히 1로
        TickProgressUITo(1f);

        UnityEngine.Debug.Log("출력 완료!");
        _outputSuccessCtrl.OutputSuccessObjChange();

#else
    UnityEngine.Debug.Log("[Print] Non-Windows: saved only (no auto print)");
    yield return null;
#endif
    }



    // ===== 인쇄창 자동 확인 (편법) =====
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

        var outTex = new Texture2D(outW, outH, TextureFormat.RGB24, false);
        outTex.ReadPixels(new Rect(cropL, cropB, outW, outH), 0, 0);
        outTex.Apply(false);

        src.filterMode = prevFilter;
        GL.PopMatrix();
        RenderTexture.active = prevRT;
        RenderTexture.ReleaseTemporary(rt);

        return outTex;
    }

    // ────────────────────────────────── Test Print (관리자 모드로 테스트 진행중 ──────────────────────────────────

    public void PrintTestBlank(Action onDone = null)
    {
        StartCoroutine(PrintTestBlankRoutine(onDone));
    }

    private IEnumerator PrintTestBlankRoutine(Action onDone)
    {
        // 출력 해상도 결정
        int w = Mathf.Max(8, _outputWidth);
        int h = Mathf.Max(8, _outputHeight);

        // 흰색 이미지 생성
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        var fill = new Color32(255, 255, 255, 255);
        var buf = new Color32[w * h];
        for (int i = 0; i < buf.Length; i++) buf[i] = fill;
        tex.SetPixels32(buf);
        tex.Apply(false);

        // 저장
        string folderPath = Application.persistentDataPath;
        Directory.CreateDirectory(folderPath);
        string filename = $"photo_testblank_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
        string savePath = Path.Combine(folderPath, filename);
        File.WriteAllBytes(savePath, tex.EncodeToJPG(95));
        UnityEngine.Object.Destroy(tex);
        UnityEngine.Debug.Log($"[Print] test blank saved: {savePath} ({w}x{h})");

        // 진행 UI 시작
        StartProgressUI();

        // 인쇄
        yield return StartCoroutine(PrintAndNotify(savePath));

        // 진행 UI 종료
        StopProgressUI();

        onDone?.Invoke();
    }
}
