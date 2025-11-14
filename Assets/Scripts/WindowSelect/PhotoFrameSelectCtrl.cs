using UnityEngine;
using UnityEngine.UI;

public class PhotoFrameSelectCtrl : MonoBehaviour
{
    [Header("Photo Frame Select Images")]
    [SerializeField] private GameObject[] _photoFrameSelectImages;

    [SerializeField] private Texture[] _photoFrameTexture;
    [SerializeField] private RawImage _mainRawImage;

    [Header("Add Frame RawImage")]
    [SerializeField] private RawImage _addFrameRawImage;

    public bool _selectFlag0 = true;
    public bool _selectFlag1 = false;
    public bool _selectFlag2 = false;

    /// <summary>
    /// 첫번째 사진 선택
    /// </summary>
    public void OnPhotoFrameSelect0()
    {
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._frameSelectButton);
        _photoFrameSelectImages[0].SetActive(true);
        _photoFrameSelectImages[1].SetActive(false);
        _photoFrameSelectImages[2].SetActive(false);

        _mainRawImage.texture = _photoFrameTexture[0];

        _selectFlag0 = true;
        _selectFlag1 = false;
        _selectFlag2 = false;

        _addFrameRawImage.texture = _photoFrameTexture[0];
    }

    /// <summary>
    /// 두번째 사진 선택
    /// </summary>
    public void OnPhotoFrameSelect1()
    {
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._frameSelectButton);
        _photoFrameSelectImages[0].SetActive(false);
        _photoFrameSelectImages[1].SetActive(true);
        _photoFrameSelectImages[2].SetActive(false);

        _mainRawImage.texture = _photoFrameTexture[1];

        _selectFlag0 = false;
        _selectFlag1 = true;
        _selectFlag2 = false;

        _addFrameRawImage.texture = _photoFrameTexture[1];
    }

    /// <summary>
    /// 세번째 사진 선택
    /// </summary>
    public void OnPhotoFrameSelect2()
    {
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._frameSelectButton);
        _photoFrameSelectImages[0].SetActive(false);
        _photoFrameSelectImages[1].SetActive(false);
        _photoFrameSelectImages[2].SetActive(true);

        _mainRawImage.texture = _photoFrameTexture[2];

        _selectFlag0 = false;
        _selectFlag1 = false;
        _selectFlag2 = true;

        _addFrameRawImage.texture = _photoFrameTexture[2];
    }

    /// <summary>
    /// 리셋 로직
    /// </summary>
    public void AllReset()
    {
        OnPhotoFrameSelect0();
        //_photoFrameSelectImages[0].SetActive(true);
        //_photoFrameSelectImages[1].SetActive(false);
        //_photoFrameSelectImages[2].SetActive(false);

        //_mainRawImage.texture = _photoFrameTexture[0];

        //_selectFlag0 = true;
        //_selectFlag1 = false;
        //_selectFlag2 = false;

        //_addFrameRawImage.texture = _photoFrameTexture[0];
    }
}
