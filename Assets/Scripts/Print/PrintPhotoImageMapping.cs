using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class PrintPhotoImageMapping : MonoBehaviour
{
    [Header("Component")]
    [SerializeField] private PhotoFrameSelectCtrl _photoFrameSelectCtrl;

    [Header("Frame Sprites (Hight Mode)")]
    [Tooltip("세로(Hight)용 프레임 스프라이트 (0: 빨강, 1: 파랑, 2: 검정)")]
    [SerializeField] private Sprite[] _frameSprite;

    [Header("Frame Sprites (Width Mode)")]
    [Tooltip("가로(Width)용 프레임 스프라이트 (0: 빨강, 1: 파랑, 2: 검정)")]
    [SerializeField] private Sprite[] _frameSpriteWidth;

    [Header("Main Frame Images (Hight Mode)")]
    [SerializeField] private Image _mainImageChange;       // Hight 메인 프레임
    [SerializeField] private Image _fakeImageChange;       // Hight 페이크 프레임

    [Header("Main Frame Images (Width Mode)")]
    [SerializeField] private Image _mainImageChangeWidth;  // Width 메인 프레임
    [SerializeField] private Image _fakeImageChangeWidth;  // Width 페이크 프레임

    [Header("Grid Red (Hight)")]
    [SerializeField] private GameObject _redObject;
    [SerializeField] private Image[] _gridRedImagesCurrent;
    [SerializeField] private Image[] _gridRedImagesChange;

    [Header("Grid Blue (Hight)")]
    [SerializeField] private GameObject _blueObject;
    [SerializeField] private Image[] _gridBlueImagesCurrent;
    [SerializeField] private Image[] _gridBlueImagesChange;

    [Header("Grid Black (Hight)")]
    [SerializeField] private GameObject _blackObject;
    [SerializeField] private Image[] _gridBlackImagesCurrent;
    [SerializeField] private Image[] _gridBlackImagesChange;

    // ================= Width 전용 =================

    [Header("Grid Red (Width)")]
    [SerializeField] private GameObject _redObjectWidth;
    [SerializeField] private Image[] _gridRedImagesCurrentWidth;
    [SerializeField] private Image[] _gridRedImagesChangeWidth;

    [Header("Grid Blue (Width)")]
    [SerializeField] private GameObject _blueObjectWidth;
    [SerializeField] private Image[] _gridBlueImagesCurrentWidth;
    [SerializeField] private Image[] _gridBlueImagesChangeWidth;

    [Header("Grid Black (Width)")]
    [SerializeField] private GameObject _blackObjectWidth;
    [SerializeField] private Image[] _gridBlackImagesCurrentWidth;
    [SerializeField] private Image[] _gridBlackImagesChangeWidth;

    // ---------------------------------------------------
    // 현재 모드 헬퍼 (GameManager 없으면 기본 Hight)
    // ---------------------------------------------------
    private bool IsHightMode
    {
        get
        {
            if (GameManager.Instance == null) return true;
            return GameManager.Instance.CurrentMode == KioskMode.Hight;
        }
    }

    /// <summary>
    /// 현재 모드(Hight/Width)에 맞는 프레임 인덱스
    /// Hight  → _selectIndexHight
    /// Width  → _selectIndexWidth
    /// </summary>
    private int CurrentFrameIndex
    {
        get
        {
            if (_photoFrameSelectCtrl == null) return 0;

            if (IsHightMode)
                return Mathf.Clamp(_photoFrameSelectCtrl._selectIndexHight, 0, 2);
            else
                return Mathf.Clamp(_photoFrameSelectCtrl._selectIndexWidth, 0, 2);
        }
    }

    /// <summary>
    /// 외부에서 호출하는 이미지 매핑 콜백
    /// - Hight 모드: _frameSprite + Hight용 변수들 사용
    /// - Width 모드: _frameSpriteWidth + Width용 변수들 사용
    /// </summary>
    public void ImageMappingCallBack()
    {
        Debug.Log("[ImageMapping] ImageMappingCallBack 호출");

        if (_photoFrameSelectCtrl == null)
        {
            Debug.LogWarning("[ImageMapping] _photoFrameSelectCtrl 이(가) 비어있습니다.");
            return;
        }

        int index = CurrentFrameIndex;
        bool isHightMode = IsHightMode;

        // 기본적으로 전부 끄고 시작 (Hight + Width 모두)
        if (_redObject) _redObject.SetActive(false);
        if (_blueObject) _blueObject.SetActive(false);
        if (_blackObject) _blackObject.SetActive(false);

        if (_redObjectWidth) _redObjectWidth.SetActive(false);
        if (_blueObjectWidth) _blueObjectWidth.SetActive(false);
        if (_blackObjectWidth) _blackObjectWidth.SetActive(false);

        // 현재 모드에 맞는 스프라이트 배열에서 frame 선택
        Sprite frame = null;
        if (isHightMode)
        {
            if (_frameSprite == null || _frameSprite.Length <= index)
            {
                Debug.LogWarning("[ImageMapping] Hight용 _frameSprite 배열이 비어있거나 index 범위 밖입니다. index=" + index);
            }
            else
            {
                frame = _frameSprite[index];
            }
        }
        else
        {
            if (_frameSpriteWidth == null || _frameSpriteWidth.Length <= index)
            {
                Debug.LogWarning("[ImageMapping] Width용 _frameSpriteWidth 배열이 비어있거나 index 범위 밖입니다. index=" + index);
            }
            else
            {
                frame = _frameSpriteWidth[index];
            }
        }

        // 프레임 이미지 적용 (메인 + 페이크)
        if (frame != null)
        {
            if (isHightMode)
            {
                if (_mainImageChange)
                {
                    _mainImageChange.sprite = frame;
                    _mainImageChange.preserveAspect = true;
                    SetAlpha(_mainImageChange, 1f);
                }

                if (_fakeImageChange)
                {
                    _fakeImageChange.sprite = frame;
                    _fakeImageChange.preserveAspect = true;
                    SetAlpha(_fakeImageChange, 1f);
                }
            }
            else
            {
                if (_mainImageChangeWidth)
                {
                    _mainImageChangeWidth.sprite = frame;
                    _mainImageChangeWidth.preserveAspect = true;
                    SetAlpha(_mainImageChangeWidth, 1f);
                }

                if (_fakeImageChangeWidth)
                {
                    _fakeImageChangeWidth.sprite = frame;
                    _fakeImageChangeWidth.preserveAspect = true;
                    SetAlpha(_fakeImageChangeWidth, 1f);
                }
            }
        }

        // ─────────────────────────────────────────────
        // Hight / Width 모드 & 색상별로 그리드 선택
        // ─────────────────────────────────────────────
        switch (index)
        {
            case 0: // 빨강
                Debug.Log("[ImageMapping] Red Frame 적용 (" + (isHightMode ? "Hight" : "Width") + ")");

                if (isHightMode)
                {
                    if (_redObject) _redObject.SetActive(true);
                    ApplyGrid(_gridRedImagesChange, _gridRedImagesCurrent);
                }
                else
                {
                    if (_redObjectWidth) _redObjectWidth.SetActive(true);
                    ApplyGrid(_gridRedImagesChangeWidth, _gridRedImagesCurrentWidth);
                }
                break;

            case 1: // 파랑
                Debug.Log("[ImageMapping] Blue Frame 적용 (" + (isHightMode ? "Hight" : "Width") + ")");

                if (isHightMode)
                {
                    if (_blueObject) _blueObject.SetActive(true);
                    ApplyGrid(_gridBlueImagesChange, _gridBlueImagesCurrent);
                }
                else
                {
                    if (_blueObjectWidth) _blueObjectWidth.SetActive(true);
                    ApplyGrid(_gridBlueImagesChangeWidth, _gridBlueImagesCurrentWidth);
                }
                break;

            case 2: // 검정
                Debug.Log("[ImageMapping] Black Frame 적용 (" + (isHightMode ? "Hight" : "Width") + ")");

                if (isHightMode)
                {
                    if (_blackObject) _blackObject.SetActive(true);
                    ApplyGrid(_gridBlackImagesChange, _gridBlackImagesCurrent);
                }
                else
                {
                    if (_blackObjectWidth) _blackObjectWidth.SetActive(true);
                    ApplyGrid(_gridBlackImagesChangeWidth, _gridBlackImagesCurrentWidth);
                }
                break;
        }
    }

    /// <summary>
    /// src 배열의 sprite를 dst 배열로 복사
    /// </summary>
    private void ApplyGrid(Image[] src, Image[] dst)
    {
        if (src == null || dst == null)
        {
            Debug.LogWarning("[ImageMapping] ApplyGrid: src 또는 dst 가 null 입니다.");
            return;
        }

        int len = Mathf.Min(src.Length, dst.Length, 4); // 4칸까지만 사용
        for (int i = 0; i < len; i++)
        {
            if (src[i] == null || dst[i] == null) continue;

            dst[i].sprite = src[i].sprite;
            dst[i].preserveAspect = true;

            var c = dst[i].color;
            if (dst[i].sprite != null)
            {
                c.a = 1f;
                dst[i].color = c;
            }
        }
    }

    /// <summary>
    /// Image 알파 값만 변경
    /// </summary>
    private void SetAlpha(Image img, float alpha)
    {
        if (img == null) return;
        var c = img.color;
        c.a = alpha;
        img.color = c;
    }
}
