using System.Drawing;
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
    [SerializeField] private GameObject _redObject;
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
        print("여기 호출됨?");
        int index = _photoFrameSelectCtrl._selectIndex;

        if (index == 0)
        {
            _redObject.SetActive(true);
            _gridRedImagesCurrent[0].sprite = _gridRedImagesChange[0].sprite;
            _mainImageChange.sprite = _frameSprite[0];
            _fakeImageChange.sprite = _frameSprite[0];
        }
        else if (index == 1)
        {
            _blueObject.SetActive(true);
            _gridRedImagesCurrent[1].sprite = _gridRedImagesChange[1].sprite;
            _mainImageChange.sprite = _frameSprite[1];
            _fakeImageChange.sprite = _frameSprite[1];
        }
        else if (index == 2)
        {
            _blackObject.SetActive(true);
            _gridRedImagesCurrent[2].sprite = _gridRedImagesChange[2].sprite;
            _mainImageChange.sprite = _frameSprite[2];
            _fakeImageChange.sprite = _frameSprite[2];
        }


    }
}
