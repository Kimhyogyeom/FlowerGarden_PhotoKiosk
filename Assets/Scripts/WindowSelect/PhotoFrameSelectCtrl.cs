using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 프레임 변경 버튼 클릭시 실행되는 스크립트
/// 스케일 0 <-> 1 인/아웃 시각화
/// </summary>
public class PhotoFrameSelectCtrl : MonoBehaviour
{
    [Header("hight")]
    [SerializeField] private Sprite[] _photoFrameTextureHight;
    // 각 포토 프레임에 대응되는 텍스처 배열
    // 인덱스 0,1,2 순서대로 사용
    [SerializeField] private GameObject[] _photoFrameSelectImagesHight;
    // 포토 프레임 선택 버튼(또는 하이라이트용) 오브젝트들
    // 0: 첫 번째 프레임, 1: 두 번째 프레임, 2: 세 번째 프레임
    [SerializeField] private Image _mainRawImageHight;
    [SerializeField] private Image _fakeImageHight;

    [Space(5)]

    [Header("Width")]
    [SerializeField] private Sprite[] _photoFrameTextureWidth;
    // 각 포토 프레임에 대응되는 텍스처 배열
    // 인덱스 0,1,2 순서대로 사용
    [SerializeField] private GameObject[] _photoFrameSelectImagesWidth;
    // 포토 프레임 선택 버튼(또는 하이라이트용) 오브젝트들
    // 0: 첫 번째 프레임, 1: 두 번째 프레임, 2: 세 번째 프레임
    [SerializeField] private Image _mainRawImageWidth;
    [SerializeField] private Image _fakeImageWidth;
    // 선택된 프레임을 실제로 보여주는 메인 RawImage

    [Header("Add Frame RawImage")]
    // [SerializeField] private Image _addFrameRawImage;
    // [SerializeField] private Image _photoMainImage;
    // 인쇄용 등, 최종 합성에 사용될 추가 프레임 RawImage

    // 현재 어떤 프레임이 선택되어 있는지 표시하는 플래그 (필요 시 외부에서 참조)
    public bool _selectFlag0Hight = true;
    public bool _selectFlag1Hight = false;
    public bool _selectFlag2Hight = false;

    public int _selectIndexHight = -1;

    public bool _selectFlag0Width = true;
    public bool _selectFlag1Width = false;
    public bool _selectFlag2Width = false;

    public int _selectIndexWidth = -1;

    /// <summary>
    /// 첫 번째 사진(프레임) 선택
    /// - 사운드 재생
    /// - 첫 번째 프레임 하이라이트
    /// - 메인/추가 RawImage 텍스처를 첫 번째 프레임으로 설정
    /// </summary>
    public void OnPhotoFrameSelect0()
    {
        // 프레임 선택 버튼 사운드
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._buttonClickSound);
        // 선택 표시 갱신 (첫 번째만 활성화)
        _photoFrameSelectImagesHight[0].SetActive(true);
        _photoFrameSelectImagesHight[1].SetActive(false);
        _photoFrameSelectImagesHight[2].SetActive(false);

        // 메인 RawImage에 첫 번째 프레임 적용
        _mainRawImageHight.sprite = _photoFrameTextureHight[0];
        _fakeImageHight.sprite = _photoFrameTextureHight[0];
        // 선택 상태 플래그 갱신
        _selectFlag0Hight = true;
        _selectFlag1Hight = false;
        _selectFlag2Hight = false;

        _selectIndexHight = 0;
        // 최종 출력용 추가 프레임에도 동일한 텍스처 적용
        // _addFrameRawImage.sprite = _photoFrameTexture[0];
    }

    /// <summary>
    /// 두 번째 사진(프레임) 선택
    /// - 사운드 재생
    /// - 두 번째 프레임 하이라이트
    /// - 메인/추가 RawImage 텍스처를 두 번째 프레임으로 설정
    /// </summary>
    public void OnPhotoFrameSelect1()
    {
        // Sound
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._buttonClickSound);
        _photoFrameSelectImagesHight[0].SetActive(false);
        _photoFrameSelectImagesHight[1].SetActive(true);
        _photoFrameSelectImagesHight[2].SetActive(false);

        _mainRawImageHight.sprite = _photoFrameTextureHight[1];
        _fakeImageHight.sprite = _photoFrameTextureHight[1];

        _selectFlag0Hight = false;
        _selectFlag1Hight = true;
        _selectFlag2Hight = false;

        _selectIndexHight = 1;
        // _addFrameRawImage.sprite = _photoFrameTexture[1];
    }

    /// <summary>
    /// 세 번째 사진(프레임) 선택
    /// - 사운드 재생
    /// - 세 번째 프레임 하이라이트
    /// - 메인/추가 RawImage 텍스처를 세 번째 프레임으로 설정
    /// </summary>
    public void OnPhotoFrameSelect2()
    {
        // Sound
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._buttonClickSound);
        _photoFrameSelectImagesHight[0].SetActive(false);
        _photoFrameSelectImagesHight[1].SetActive(false);
        _photoFrameSelectImagesHight[2].SetActive(true);

        _mainRawImageHight.sprite = _photoFrameTextureHight[2];
        _fakeImageHight.sprite = _photoFrameTextureHight[2];

        _selectFlag0Hight = false;
        _selectFlag1Hight = false;
        _selectFlag2Hight = true;

        _selectIndexHight = 2;
        // _addFrameRawImage.sprite = _photoFrameTexture[2];
    }
    // ────────────────────────────────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Width 0번 프레임
    /// </summary>
    public void OnPhotoFrameSelect0W()
    {
        // Sound
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._buttonClickSound);
        _photoFrameSelectImagesWidth[0].SetActive(false);
        _photoFrameSelectImagesWidth[1].SetActive(false);
        _photoFrameSelectImagesWidth[2].SetActive(true);

        _mainRawImageWidth.sprite = _photoFrameTextureWidth[2];
        _fakeImageWidth.sprite = _photoFrameTextureWidth[2];

        _selectFlag0Width = false;
        _selectFlag1Width = false;
        _selectFlag2Width = true;

        _selectIndexWidth = 2;
        // _addFrameRawImage.sprite = _photoFrameTexture[2];
    }
    /// <summary>
    /// Width 1번 프레임
    /// </summary>
    public void OnPhotoFrameSelect1W()
    {
        // Sound
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._buttonClickSound);
        _photoFrameSelectImagesWidth[0].SetActive(false);
        _photoFrameSelectImagesWidth[1].SetActive(false);
        _photoFrameSelectImagesWidth[2].SetActive(true);

        _mainRawImageWidth.sprite = _photoFrameTextureWidth[2];
        _fakeImageWidth.sprite = _photoFrameTextureWidth[2];

        _selectFlag0Width = false;
        _selectFlag1Width = false;
        _selectFlag2Width = true;

        _selectIndexWidth = 2;
        // _addFrameRawImage.sprite = _photoFrameTexture[2];
    }
    /// <summary>
    /// Width 2번 프레임
    /// </summary>
    public void OnPhotoFrameSelect2W()
    {
        // Sound
        SoundManager.Instance.PlaySFX(SoundManager.Instance._soundDatabase._buttonClickSound);
        _photoFrameSelectImagesWidth[0].SetActive(false);
        _photoFrameSelectImagesWidth[1].SetActive(false);
        _photoFrameSelectImagesWidth[2].SetActive(true);

        _mainRawImageWidth.sprite = _photoFrameTextureWidth[2];
        _fakeImageWidth.sprite = _photoFrameTextureWidth[2];

        _selectFlag0Width = false;
        _selectFlag1Width = false;
        _selectFlag2Width = true;

        _selectIndexWidth = 2;
        // _addFrameRawImage.sprite = _photoFrameTexture[2];
    }
    /// <summary>
    /// 리셋 로직
    /// - 항상 첫 번째 프레임이 선택된 상태로 초기화
    /// </summary>
    public void AllReset()
    {
        // 첫 번째 프레임 선택 로직 재사용
        OnPhotoFrameSelect0();

        // 아래 코드는 OnPhotoFrameSelect0()과 동일한 동작이라 주석 처리해둔 상태
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
