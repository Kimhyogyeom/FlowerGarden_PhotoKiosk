using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Networking;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// NCP Object Storage (S3-compatible) Uploader (AWS Signature V4)
/// - AWS SDK 없이 동작 (Amazon namespace 불필요)
/// - SHA256.HashData 미사용 (Unity/.NET 호환): SHA256.Create().ComputeHash 사용
/// - Path-style request:
///     PUT https://{host}/{bucket}/{objectKey}
/// </summary>
public class NcpObjectStorageUploader : MonoBehaviour
{
    [Header("NCP Object Storage")]
    [Tooltip("예: kr.object.ncloudstorage.com")]
    [SerializeField] private string _host = "kr.object.ncloudstorage.com";

    [Tooltip("버킷 이름")]
    [SerializeField] private string _bucketName = "hwawon-photokiosk";

    [Tooltip("region: NCP Object Storage는 보통 kr-standard")]
    [SerializeField] private string _region = "kr-standard";

    [Tooltip("S3 서비스명은 's3'")]
    [SerializeField] private string _service = "s3";

    [Header("NCP IAM Keys")]
    [Tooltip("외부 파일에서 로드하려면 비워두세요. 파일 경로: StreamingAssets/ncp_secrets.json")]
    [SerializeField] private string _accessKeyId = "";
    [SerializeField] private string _secretKey = "";

    [Tooltip("true면 StreamingAssets/ncp_secrets.json 에서 키를 로드합니다")]
    [SerializeField] private bool _loadKeysFromFile = true;

    [Header("Upload Options")]
    [Tooltip("true면 x-amz-acl: public-read 를 넣어 공개 URL로 바로 접근 가능(버킷 정책도 허용 필요)")]
    [SerializeField] private bool _makePublicRead = true;

    [Tooltip("기본 prefix (예: photos/)")]
    [SerializeField] private string _defaultPrefix = "photos/";

    [Header("Test (Optional)")]
    [SerializeField] private bool _enableAKeyTest = false;

    // =======================
    // Public API
    // =======================

    /// <summary>
    /// 로컬 파일 업로드 (png/jpg 등) - 코루틴 버전
    /// </summary>
    public IEnumerator UploadLocalFileRoutine(string localPath, string objectKey,
        Action<string> onSuccess, Action<string> onError,
        bool? makePublicReadOverride = null)
    {
        if (string.IsNullOrWhiteSpace(localPath) || !File.Exists(localPath))
        {
            onError?.Invoke($"파일이 없어요: {localPath}");
            yield break;
        }

        byte[] bytes;
        try { bytes = File.ReadAllBytes(localPath); }
        catch (Exception e)
        {
            onError?.Invoke($"파일 읽기 실패: {e.Message}");
            yield break;
        }

        string contentType = GuessContentType(localPath);
        yield return PutObjectRoutine(bytes, objectKey, contentType, onSuccess, onError, makePublicReadOverride);
    }

    /// <summary>
    /// PNG 바이트 업로드 (권장) - 코루틴 버전
    /// </summary>
    public IEnumerator UploadPngBytesRoutine(byte[] pngBytes, string objectKey,
        Action<string> onSuccess, Action<string> onError,
        bool? makePublicReadOverride = null)
    {
        yield return PutObjectRoutine(pngBytes, objectKey, "image/png", onSuccess, onError, makePublicReadOverride);
    }

    /// <summary>
    /// (별칭) 테스트 스크립트에서 많이 쓰는 이름: UploadPng (코루틴)
    /// </summary>
    public IEnumerator UploadPng(byte[] pngBytes, string objectKey,
        Action<string> onSuccess, Action<string> onFail,
        bool? makePublicReadOverride = null)
    {
        yield return PutObjectRoutine(pngBytes, objectKey, "image/png", onSuccess, onFail, makePublicReadOverride);
    }

    /// <summary>
    /// 업로드된 객체의 공개 URL (public-read일 때 사용)
    /// </summary>
    public string BuildPublicUrl(string objectKey)
    {
        objectKey = NormalizeObjectKey(objectKey);
        return $"https://{_host}/{_bucketName}/{EscapePath(objectKey)}";
    }

    // =======================
    // Lifecycle
    // =======================

    private void Awake()
    {
        if (_loadKeysFromFile)
        {
            LoadKeysFromFile();
        }
    }

    private void LoadKeysFromFile()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "ncp_secrets.json");

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"[NCP] 키 파일이 없습니다: {filePath}\n" +
                "ncp_secrets.json 파일을 StreamingAssets 폴더에 생성하세요.\n" +
                "형식: {\"accessKeyId\":\"YOUR_KEY\",\"secretKey\":\"YOUR_SECRET\"}");
            return;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            var secrets = JsonUtility.FromJson<NcpSecrets>(json);

            if (!string.IsNullOrEmpty(secrets.accessKeyId))
                _accessKeyId = secrets.accessKeyId;
            if (!string.IsNullOrEmpty(secrets.secretKey))
                _secretKey = secrets.secretKey;

            // Debug.Log("[NCP] 키 파일에서 인증 정보 로드 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"[NCP] 키 파일 로드 실패: {e.Message}");
        }
    }

    [Serializable]
    private class NcpSecrets
    {
        public string accessKeyId;
        public string secretKey;
    }

    // =======================
    // Test: A 키로 더미 업로드
    // =======================

    private void Update()
    {
        if (!_enableAKeyTest) return;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.aKey.wasPressedThisFrame)
        {
            // Debug.Log("[NCP TEST] AKey pressed -> uploading test png...");
            StartCoroutine(TestUploadDummyPng());
        }
#endif
    }

    private IEnumerator TestUploadDummyPng()
    {
        // 256x256 흰색 PNG 생성
        const int w = 256;
        const int h = 256;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var fill = new Color32(255, 255, 255, 255);
        var buf = new Color32[w * h];
        for (int i = 0; i < buf.Length; i++) buf[i] = fill;
        tex.SetPixels32(buf);
        tex.Apply(false);

        byte[] png = tex.EncodeToPNG();
        Destroy(tex);

        string key = $"{_defaultPrefix}test/ncp_test_{DateTime.Now:yyyyMMdd_HHmmss}.png";

        string resultUrl = null;
        string err = null;

        yield return PutObjectRoutine(
            png,
            key,
            "image/png",
            url => resultUrl = url,
            e => err = e
        );

        if (!string.IsNullOrEmpty(err))
            Debug.LogError("[NCP TEST] 업로드 실패:\n" + err);
        // else
        //     Debug.Log("[NCP TEST] 업로드 성공! URL: " + resultUrl);
    }

    // =======================
    // Internals
    // =======================

    private IEnumerator PutObjectRoutine(byte[] bodyBytes, string objectKey, string contentType,
        Action<string> onSuccess, Action<string> onError,
        bool? makePublicReadOverride = null)
    {
        // ---- validation
        if (bodyBytes == null || bodyBytes.Length == 0)
        {
            onError?.Invoke("업로드 바이트가 비어있습니다.");
            yield break;
        }
        if (string.IsNullOrWhiteSpace(_accessKeyId) || string.IsNullOrWhiteSpace(_secretKey))
        {
            onError?.Invoke("AccessKey/SecretKey가 비어있습니다.");
            yield break;
        }
        if (string.IsNullOrWhiteSpace(_bucketName))
        {
            onError?.Invoke("bucketName이 비어있습니다.");
            yield break;
        }

        objectKey = NormalizeObjectKey(objectKey);
        bool makePublic = makePublicReadOverride ?? _makePublicRead;

        // ---- time (UTC)
        // ⚠️ PC 시간이 크게 틀리면 SignatureDoesNotMatch 뜰 수 있음 (윈도우 시간 자동동기화 필수)
        DateTime utcNow = DateTime.UtcNow;
        string amzDate = utcNow.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        string dateStamp = utcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        // ---- endpoint (Path-style: /{bucket}/{key})
        string canonicalUri = "/" + _bucketName + "/" + EscapePath(objectKey);
        string url = $"https://{_host}{canonicalUri}";

        // ---- payload hash
        string payloadHash = ToHex(Sha256Bytes(bodyBytes));

        // ---- headers (must match signed headers)
        string hostHeader = _host;
        string aclHeader = makePublic ? "public-read" : null;

        // canonical headers (sorted)
        var canonicalHeadersSb = new StringBuilder();
        canonicalHeadersSb.Append("content-type:").Append(contentType).Append("\n");
        canonicalHeadersSb.Append("host:").Append(hostHeader).Append("\n");
        if (!string.IsNullOrEmpty(aclHeader))
            canonicalHeadersSb.Append("x-amz-acl:").Append(aclHeader).Append("\n");
        canonicalHeadersSb.Append("x-amz-content-sha256:").Append(payloadHash).Append("\n");
        canonicalHeadersSb.Append("x-amz-date:").Append(amzDate).Append("\n");

        string signedHeaders = !string.IsNullOrEmpty(aclHeader)
            ? "content-type;host;x-amz-acl;x-amz-content-sha256;x-amz-date"
            : "content-type;host;x-amz-content-sha256;x-amz-date";

        // ---- canonical request
        string canonicalQueryString = "";
        string canonicalRequest =
            "PUT\n" +
            canonicalUri + "\n" +
            canonicalQueryString + "\n" +
            canonicalHeadersSb + "\n" +
            signedHeaders + "\n" +
            payloadHash;

        string canonicalRequestHash = ToHex(Sha256Bytes(Encoding.UTF8.GetBytes(canonicalRequest)));

        // ---- string to sign
        string credentialScope = $"{dateStamp}/{_region}/{_service}/aws4_request";
        string stringToSign =
            "AWS4-HMAC-SHA256\n" +
            amzDate + "\n" +
            credentialScope + "\n" +
            canonicalRequestHash;

        // ---- signature
        byte[] signingKey = GetSignatureKey(_secretKey, dateStamp, _region, _service);
        string signature = ToHex(HmacSha256Bytes(signingKey, Encoding.UTF8.GetBytes(stringToSign)));

        string authorization =
            $"AWS4-HMAC-SHA256 Credential={_accessKeyId}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";

        // ---- request
        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyBytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout = 30; // 30초 타임아웃 (무한 대기 방지)

            // headers (MUST match canonical headers)
            req.SetRequestHeader("Content-Type", contentType);
            req.SetRequestHeader("Host", hostHeader);
            req.SetRequestHeader("x-amz-content-sha256", payloadHash);
            req.SetRequestHeader("x-amz-date", amzDate);
            if (!string.IsNullOrEmpty(aclHeader))
                req.SetRequestHeader("x-amz-acl", aclHeader);
            req.SetRequestHeader("Authorization", authorization);

            yield return req.SendWebRequest();

            long code = req.responseCode;
            bool ok = (code == 200 || code == 204);

            if (!ok || req.result == UnityWebRequest.Result.ProtocolError || req.result == UnityWebRequest.Result.ConnectionError)
            {
                string body = req.downloadHandler != null ? req.downloadHandler.text : "";
                string err =
                    $"HTTP {code}\n" +
                    (string.IsNullOrEmpty(req.error) ? "" : (req.error + "\n")) +
                    body;

                onError?.Invoke(err);
                yield break;
            }

            string publicUrl = BuildPublicUrl(objectKey);
            onSuccess?.Invoke(publicUrl);
        }
    }

    // =======================
    // Helpers
    // =======================

    private string NormalizeObjectKey(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            objectKey = $"{_defaultPrefix}photo_{DateTime.Now:yyyyMMdd_HHmmss}.png";

        objectKey = objectKey.Replace("\\", "/").Trim();
        while (objectKey.StartsWith("/")) objectKey = objectKey.Substring(1);
        return objectKey;
    }

    /// <summary>
    /// AWS SigV4 규칙에 맞게 path segment 단위 Escape (슬래시는 유지)
    /// </summary>
    private static string EscapePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return "";
        var parts = path.Split('/');
        for (int i = 0; i < parts.Length; i++)
            parts[i] = Uri.EscapeDataString(parts[i]);
        return string.Join("/", parts);
    }

    private static string GuessContentType(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLowerInvariant();
        switch (ext)
        {
            case ".png": return "image/png";
            case ".jpg":
            case ".jpeg": return "image/jpeg";
            case ".webp": return "image/webp";
            default: return "application/octet-stream";
        }
    }

    private static byte[] Sha256Bytes(byte[] data)
    {
        using (var sha = SHA256.Create())
            return sha.ComputeHash(data);
    }

    private static byte[] HmacSha256Bytes(byte[] key, byte[] data)
    {
        using (var hmac = new HMACSHA256(key))
            return hmac.ComputeHash(data);
    }

    private static byte[] HmacSha256Bytes(byte[] key, string data)
    {
        return HmacSha256Bytes(key, Encoding.UTF8.GetBytes(data));
    }

    private static byte[] GetSignatureKey(string secretKey, string dateStamp, string regionName, string serviceName)
    {
        byte[] kSecret = Encoding.UTF8.GetBytes("AWS4" + secretKey);
        byte[] kDate = HmacSha256Bytes(kSecret, dateStamp);
        byte[] kRegion = HmacSha256Bytes(kDate, regionName);
        byte[] kService = HmacSha256Bytes(kRegion, serviceName);
        byte[] kSigning = HmacSha256Bytes(kService, "aws4_request");
        return kSigning;
    }

    private static string ToHex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        for (int i = 0; i < bytes.Length; i++)
            sb.Append(bytes[i].ToString("x2"));
        return sb.ToString();
    }
}
