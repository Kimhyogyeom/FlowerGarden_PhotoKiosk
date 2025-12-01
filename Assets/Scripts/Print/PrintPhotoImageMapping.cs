using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class ImageMapping : MonoBehaviour
{
    [Header("Component")]
    [SerializeField] private PhotoFrameSelectCtrl _photoFrameSelectCtrl;

    [SerializeField] private Sprite[] _frameSprite;
    [SerializeField] private Image _mainImageChange;
    [SerializeField] private Image _fakeImageChange;

    [Header("Grid Red")]
    [SerializeField] private GameObject _redObject; //
    [SerializeField] private Image[] _gridRedImagesCurrent;
    [SerializeField] private Image[] _gridRedImagesChange;

    [Header("Grid Blue")]
    [SerializeField] private GameObject _blueObject;
    [SerializeField] private Image[] _gridBlueImagesCurrent;
    [SerializeField] private Image[] _gridBlueImagesChange;

    [Header("Grid Black")]
    [SerializeField] private GameObject _blackObject;
    [SerializeField] private Image[] _gridBlackImagesCurrent;
    [SerializeField] private Image[] _gridBlackImagesChange;

    /// <summary>
    /// 외부 호출용 이미지 교체용 콜백 함수
    /// </summary>
    public void ImageMappingCallBack()
    {
        Debug.Log("[ImageMapping] ImageMappingCallBack 호출");

        if (_photoFrameSelectCtrl == null)
        {
            Debug.LogWarning("[ImageMapping] _photoFrameSelectCtrl 이(가) 비어있습니다.");
            return;
        }

        int index = Mathf.Clamp(_photoFrameSelectCtrl._selectIndexHight, 0, 2);

        // 기본적으로 전부 끄고 시작
        if (_redObject) _redObject.SetActive(false);
        if (_blueObject) _blueObject.SetActive(false);
        if (_blackObject) _blackObject.SetActive(false);

        // 프레임 메인 이미지(테두리) 세팅
        if (_frameSprite != null && _frameSprite.Length > index)
        {
            if (_mainImageChange) _mainImageChange.sprite = _frameSprite[index];
            if (_fakeImageChange) _fakeImageChange.sprite = _frameSprite[index];
        }

        switch (index)
        {
            case 0: // 빨강
                Debug.Log("[ImageMapping] Red Frame 적용");
                if (_redObject) _redObject.SetActive(true);

                ApplyGrid(_gridRedImagesChange, _gridRedImagesCurrent);
                break;

            case 1: // 파랑
                Debug.Log("[ImageMapping] Blue Frame 적용");
                if (_blueObject) _blueObject.SetActive(true);

                ApplyGrid(_gridBlueImagesChange, _gridBlueImagesCurrent);
                break;

            case 2: // 검정
                Debug.Log("[ImageMapping] Black Frame 적용");
                if (_blackObject) _blackObject.SetActive(true);

                ApplyGrid(_gridBlackImagesChange, _gridBlackImagesCurrent);
                break;
        }
    }

    /// <summary>
    /// change 배열의 sprite를 current 배열로 복사
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
}
